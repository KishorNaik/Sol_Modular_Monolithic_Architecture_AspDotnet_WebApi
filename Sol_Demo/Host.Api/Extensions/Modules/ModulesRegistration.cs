﻿using Organization.Applications;

namespace Host.Api.Extensions.Modules;

public static class ModulesRegistration
{
    public static void AddModules(this IHostApplicationBuilder hostApplicationBuilder)
    {
        Console.WriteLine($"Env: {hostApplicationBuilder.Environment.EnvironmentName}");

        hostApplicationBuilder
            .Services
            .AddOrganizationModule(hostApplicationBuilder);
    }
}