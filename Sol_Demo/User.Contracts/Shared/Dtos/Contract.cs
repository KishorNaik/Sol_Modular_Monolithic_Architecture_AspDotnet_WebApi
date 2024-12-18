﻿using Models.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace User.Contracts.Shared.Dtos;

public class UserDto
{

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public UserTypeEnum UserType { get; set; }

    public StatusEnum Status { get; set; }
}

public class UserCommunicationDto
{
    public string? EmailId { get; set; }

    public string? MobileNumber { get; set; }
}

public class UserOrganizationDto
{
    public Guid? OrgId { get; set; }
}