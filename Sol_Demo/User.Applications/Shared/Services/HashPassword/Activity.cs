
namespace User.Applications.Shared.Services.HashPassword;

public class GenerateHashPasswordServiceResult
{
    public string? Salt { get; }

    public string? Hash { get; }

    public GenerateHashPasswordServiceResult(string? salt, string? hash)
    {
        Salt = salt;
        Hash = hash;
    }
}

public interface IGenerateHashPasswordService : IServiceHandlerAsync<string, GenerateHashPasswordServiceResult>
{

}

[ScopedService(typeof(IGenerateHashPasswordService))]
public sealed class GenerateHashPasswordService : IGenerateHashPasswordService
{
    async Task<Result<GenerateHashPasswordServiceResult>> IServiceHandlerAsync<string, GenerateHashPasswordServiceResult>.HandleAsync(string @params)
    {
       try
        {
            if(@params is null)
                return ResultExceptionFactory.Error("password is empty or null", HttpStatusCode.BadRequest);

            var saltData = await Salt.CreateAsync(ByteRange.byte256);
            var hashData = await Hash.CreateAsync(@params, saltData, ByteRange.byte256);

            var passwordServiceResult=new GenerateHashPasswordServiceResult(saltData, hashData);

            return Result.Ok(passwordServiceResult);
        }
        catch(Exception ex)
        {
            return ResultExceptionFactory.Error(ex.Message, HttpStatusCode.InternalServerError);
        }
    }
}
