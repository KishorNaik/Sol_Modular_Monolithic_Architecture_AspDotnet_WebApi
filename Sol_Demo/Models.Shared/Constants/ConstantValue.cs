namespace Models.Shared.Constant;

public static class ConstantValue
{
    public const double CacheTime = 365 * 7000;

    public const string DbName = "MMA";

    public const string SqlCacheDbName = "SQLCache";

    public const string HangFireDbName = "HangFireDB";

    public const string SeriLogDbName = "SeriLogs";

    public const string StatusCode = "StatusCode";

    public const int IvLength = 16;

    public static string? AesSecretKey = "AesSecretKey";

    public const string UserOnlyPolicy = "UserOnlyPolicy";

    public const string AdminOnlyPolicy = "AdminOnlyPolicy";

    public const string UserAndAdminPolicy = "UserAndAdminPolicy";
}