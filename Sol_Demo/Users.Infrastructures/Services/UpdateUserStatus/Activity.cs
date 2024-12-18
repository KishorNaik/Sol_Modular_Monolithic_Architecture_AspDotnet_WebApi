using FluentResults;
using Models.Shared.Enums;
using sorovi.DependencyInjection.AutoRegister;
using System.Net;
using Users.Infrastructures.Contexts;
using Users.Infrastructures.Entities;
using Utility.Shared.Exceptions;
using Utility.Shared.ServiceHandler;

namespace Users.Infrastructures.Services.UpdateUserStatus;

#region Db Service
public class UpdateUserStatusDbServiceSqlParameters
{
    public Tuser User { get; }

    public CancellationToken CancellationToken { get; }


    public UpdateUserStatusDbServiceSqlParameters(Tuser user, CancellationToken cancellationToken)
    {
        User = user;
        CancellationToken = cancellationToken;
    }
}

public interface IUpdateUserStatusDbService : IServiceHandlerVoidAsync<UpdateUserStatusDbServiceSqlParameters>
{

}

[ScopedService(typeof(IUpdateUserStatusDbService))]
public class UpdateUserStatusDbService : IUpdateUserStatusDbService
{
    private readonly UsersDbContext _usersDbContext;

    public UpdateUserStatusDbService(UsersDbContext usersDbContext)
    {
        _usersDbContext = usersDbContext;
    }

    async Task<Result> IServiceHandlerVoidAsync<UpdateUserStatusDbServiceSqlParameters>.HandleAsync(UpdateUserStatusDbServiceSqlParameters @params)
    {
        try
        {
            if(@params is null)
                return ResultExceptionFactory.Error($"{nameof(UpdateUserStatusDbServiceSqlParameters)} is null", HttpStatusCode.BadRequest);

            if(@params.User is null)
                return ResultExceptionFactory.Error($"{nameof(UpdateUserStatusDbServiceSqlParameters.User)} is null", HttpStatusCode.BadRequest);

            Tuser tuser = @params.User;

            // Update Status
            tuser.Status = Convert.ToBoolean(Convert.ToInt32(StatusEnum.Active));

            // Empty Token
            tuser.TuserToken.EmailToken = Guid.Empty;

            // Update Is Email Verification Flag
            tuser.TuserSetting.IsEmailVerified = Convert.ToBoolean(Convert.ToInt32(VerifiedEnum.Yes));

            _usersDbContext.Tusers.Update(tuser);
            await _usersDbContext.SaveChangesAsync(@params.CancellationToken);

            return Result.Ok();
        }
        catch(Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}
#endregion 
