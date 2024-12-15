namespace User.Contracts.Features.AddUsers;

#region Request DTO
public class AddUserRequestDto
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? Mobile { get; set; }

    public Guid? OrgId { get; set; }
}
#endregion
