namespace Organization.Applications.Shared.Cache;

public class OrganizationSharedCacheService : INotification
{
    public Guid? Identifier { get; }

    public OrganizationSharedCacheService(Guid? identifier)
    {
        Identifier = identifier;
    }
}