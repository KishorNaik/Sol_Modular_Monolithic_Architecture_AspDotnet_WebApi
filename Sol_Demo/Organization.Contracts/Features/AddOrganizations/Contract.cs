namespace Organization.Contracts.Features.AddOrganizations;

#region Request DTO

public class AddOrganizationRequestDto
{
    public string? Name { get; set; }
}

#endregion Request DTO

#region Response DTO

public class AddOrganizationResponseDto
{
    public Guid? Identifier { get; set; }
}

#endregion Response DTO