﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace Users.Infrastructures.Entities;

public partial class TusersOrganization
{
    public decimal Id { get; set; }

    public Guid UserId { get; set; }

    public Guid OrgId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public byte[] Version { get; set; }

    public virtual Tuser User { get; set; }
}