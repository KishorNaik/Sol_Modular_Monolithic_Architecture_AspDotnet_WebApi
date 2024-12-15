using FluentResults;
using Microsoft.EntityFrameworkCore;
using sorovi.DependencyInjection.AutoRegister;
using System.Net;
using Users.Infrastructures.Contexts;
using Utility.Shared.Exceptions;
using Utility.Shared.ServiceHandler;

namespace Users.Infrastructures.Services.GetVersionByIdentifer;

#region Db Service
public class GetUserVersionByIdentifierDbServiceSqlParameters
{
    public Guid? Identifier { get; }

    public CancellationToken CancellationToken { get; }

    public GetUserVersionByIdentifierDbServiceSqlParameters(Guid? identifier, CancellationToken cancellationToken)
    {
        Identifier = identifier;
        CancellationToken = cancellationToken;
    }
}

public interface IGetUserVersionByIdentifierDbService : IServiceHandlerAsync<GetUserVersionByIdentifierDbServiceSqlParameters, byte[]>
{

}

[ScopedService(typeof(IGetUserVersionByIdentifierDbService))]
public sealed class GetUserVersionByIdentifierDbService : IGetUserVersionByIdentifierDbService
{
    private readonly UsersDbContext _usersDbContext;

    public GetUserVersionByIdentifierDbService(UsersDbContext usersDbContext)
    {
        _usersDbContext = usersDbContext;
    }

    async Task<Result<byte[]>> IServiceHandlerAsync<GetUserVersionByIdentifierDbServiceSqlParameters, byte[]>.HandleAsync(GetUserVersionByIdentifierDbServiceSqlParameters @params)
    {
        try
        {
            if(@params is null)
                return ResultExceptionFactory.Error($"{nameof(GetUserVersionByIdentifierDbServiceSqlParameters)} cannot be null", HttpStatusCode.BadRequest);

            if(@params.Identifier is null)
                return ResultExceptionFactory.Error($"{nameof(@params.Identifier)} cannot be null", HttpStatusCode.BadRequest);

            var result = (await _usersDbContext
                .Tusers
                .AsNoTracking()
                .AsParallel()
                .AsQueryable()
                .FirstOrDefaultAsync(x => x.Identifier == @params.Identifier, @params.CancellationToken)
                )?.Version;

            if(result is null)
                return ResultExceptionFactory.Error($"{nameof(@params.Identifier)} not found", HttpStatusCode.NotFound);

            if(result?.Length == 0)
                return ResultExceptionFactory.Error($"{nameof(@params.Identifier)} not found", HttpStatusCode.NotFound);

            return Result.Ok(result)!;
        }
        catch(Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}
#endregion 
