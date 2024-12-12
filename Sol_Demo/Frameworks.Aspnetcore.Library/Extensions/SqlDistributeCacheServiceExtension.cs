using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Frameworks.Aspnetcore.Library.Extensions;

public static class SqlDistributeCacheServiceExtension
{
	public static void AddCustomSqlDistributedCache(this IServiceCollection services, IConfiguration configuration, string? dbSectionName, string? schemaName, string? tableName)
	{
		services.AddDistributedSqlServerCache(options =>
		{
			options.ConnectionString = configuration.GetSecretConnectionString(dbSection: dbSectionName);
			options.SchemaName = schemaName ?? "dbo";
			options.TableName = tableName;
		});
	}
}

/*
 * 
 * Manually create DB and Table

CREATE DATABASE SQLCache

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[DbCache](
	[Id] [nvarchar](449) NOT NULL,
	[Value] [varbinary](max) NOT NULL,
	[ExpiresAtTime] [datetimeoffset](7) NOT NULL,
	[SlidingExpirationInSeconds] [bigint] NULL,
	[AbsoluteExpiration] [datetimeoffset](7) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, 
	IGNORE_DUP_KEY = OFF, 
	ALLOW_ROW_LOCKS = ON, 
	ALLOW_PAGE_LOCKS = ON, 
	OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
 * */