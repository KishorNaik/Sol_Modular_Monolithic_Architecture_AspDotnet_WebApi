
using FluentResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using Models.Shared.Enums;
using sorovi.DependencyInjection.AutoRegister;
using System.Net;
using Users.Infrastructures.Contexts;
using Users.Infrastructures.Entities;
using Utility.Shared.Exceptions;
using Utility.Shared.ServiceHandler;

namespace Users.Infrastructures.Services.GetUsersByIdentifier;

#region Db Service
public class GetUserByIdentiferDbServiceSqlParameters
{
    public Guid? Identifer { get; }

    public StatusEnum? Status { get; }

    public CancellationToken CancellationToken { get; }

    public GetUserByIdentiferDbServiceSqlParameters(Guid? identifer, StatusEnum? status, CancellationToken cancellationToken)
    {
        Identifer = identifer;
        Status = status;
        CancellationToken = cancellationToken;
    }
}



public interface IGetUserByIdentiferDbService : IServiceHandlerAsync<GetUserByIdentiferDbServiceSqlParameters, Tuser>
{
   
}

[ScopedService(typeof(IGetUserByIdentiferDbService))]
public sealed class GetUserByIdentiferDbService : IGetUserByIdentiferDbService
{
    private readonly UsersDbContext _usersDbContext;

    public GetUserByIdentiferDbService(UsersDbContext usersDbContext)
    {
      _usersDbContext = usersDbContext;
    }

    async Task<Result<Tuser>> IServiceHandlerAsync<GetUserByIdentiferDbServiceSqlParameters, Tuser>
        .HandleAsync(GetUserByIdentiferDbServiceSqlParameters @params)
    {
        try
        { 
            if(@params is null)
                return ResultExceptionFactory.Error<Tuser>($"{nameof(GetUserByIdentiferDbServiceSqlParameters)} is null", httpStatusCode: HttpStatusCode.BadRequest);

            if(@params.Identifer is null)
                return ResultExceptionFactory.Error<Tuser>($"{nameof(@params.Identifer)} is null", httpStatusCode: HttpStatusCode.BadRequest);

            var result = await _usersDbContext
                .Tusers
                .Include(x=>x.TuserToken)
                .Include(x=>x.TuserCommunication)
                .Include(x=>x.TuserCredential)
                .Include(x=>x.TuserSetting)
                .Include(x=>x.TusersOrganization)
                .AsNoTracking()
                .AsQueryable()     
                .FirstOrDefaultAsync(x => x.Identifier == @params.Identifer && x.Status == Convert.ToBoolean((int)@params.Status!), @params.CancellationToken);

            if(result is null)
                return ResultExceptionFactory.Error<Tuser>($"{nameof(@params.Identifer)} is not found", httpStatusCode: HttpStatusCode.NotFound);

            return Result.Ok(result);

        }
        catch(Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, httpStatusCode: HttpStatusCode.InternalServerError);
        }
    }
}

#endregion 
