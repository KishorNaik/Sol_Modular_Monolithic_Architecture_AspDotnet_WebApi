using FluentResults;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Utility.Shared.Exceptions;

namespace Utility.Shared.Config;

public interface IConfigHelper
{
    Result<string> GetValue(string key);

    Result<T> GetSection<T>(string sectionName) where T : new();
}

public class ConfigHelper : IConfigHelper
{
    private readonly IConfiguration _configuration;

    public ConfigHelper(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Result<string> GetValue(string key)
    {
        if (key is null)
            return ResultExceptionFactory.Error<string>("key cannot be null", System.Net.HttpStatusCode.BadRequest);

        string value = _configuration[key]!;

        return Result.Ok<string>(value);
    }

    public Result<T> GetSection<T>(string sectionName) where T : new()
    {
        if (string.IsNullOrEmpty(sectionName))
            return ResultExceptionFactory.Error<T>("sectionName cannot be null or empty", System.Net.HttpStatusCode.BadRequest);

        var section = _configuration.GetSection(sectionName);
        if (!section.Exists())
            return ResultExceptionFactory.Error<T>($"Section '{sectionName}' does not exist", System.Net.HttpStatusCode.NotFound);

        T configObject = new T();
        section.Bind(configObject);

        return Result.Ok(configObject);
    }
}

public static class ConfigServiceRegister
{
    public static void AddConfigService(this IServiceCollection services)
    {
        services.AddSingleton<IConfigHelper, ConfigHelper>();
    }
}