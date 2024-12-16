﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Users.Infrastructures.Entities;

namespace Users.Infrastructures.Contexts;

public partial class UsersDbContext : DbContext
{
    public UsersDbContext(DbContextOptions<UsersDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Tuser> Tusers { get; set; }

    public virtual DbSet<TuserCommunication> TuserCommunications { get; set; }

    public virtual DbSet<TuserCredential> TuserCredentials { get; set; }

    public virtual DbSet<TuserSetting> TuserSettings { get; set; }

    public virtual DbSet<TuserToken> TuserTokens { get; set; }

    public virtual DbSet<TusersOrganization> TusersOrganizations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tuser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TUsers__3214EC07C6311A37");

            entity.ToTable("TUsers", "UserSchema");

            entity.HasIndex(e => e.Identifier, "UQ__TUsers__821FB019896B86F6").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnType("numeric(18, 0)");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();
        });

        modelBuilder.Entity<TuserCommunication>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TUserCom__3214EC072EE6084E");

            entity.ToTable("TUserCommunications", "UserSchema");

            entity.HasIndex(e => e.UserId, "UQ__TUserCom__1788CC4D3E58DA0C").IsUnique();

            entity.HasIndex(e => e.MobileNumber, "UQ__TUserCom__250375B1F901B64A").IsUnique();

            entity.HasIndex(e => e.EmailId, "UQ__TUserCom__7ED91ACEEED71308").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnType("numeric(18, 0)");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.EmailId)
                .IsRequired()
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.MobileNumber)
                .IsRequired()
                .HasMaxLength(15)
                .IsUnicode(false);
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.User).WithOne(p => p.TuserCommunication)
                .HasPrincipalKey<Tuser>(p => p.Identifier)
                .HasForeignKey<TuserCommunication>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TUserCommunications_TUsers");
        });

        modelBuilder.Entity<TuserCredential>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TUserCre__3214EC076D99ACBF");

            entity.ToTable("TUserCredentials", "UserSchema");

            entity.HasIndex(e => e.UserId, "UQ__TUserCre__1788CC4D9A05139F").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnType("numeric(18, 0)");
            entity.Property(e => e.AesSecretKey)
                .IsRequired()
                .IsUnicode(false);
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Hash)
                .IsRequired()
                .IsUnicode(false);
            entity.Property(e => e.HmacSecretKey)
                .IsRequired()
                .IsUnicode(false);
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Salt)
                .IsRequired()
                .IsUnicode(false);
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.User).WithOne(p => p.TuserCredential)
                .HasPrincipalKey<Tuser>(p => p.Identifier)
                .HasForeignKey<TuserCredential>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TUserCredentials_TUsers");
        });

        modelBuilder.Entity<TuserSetting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TUserSet__3213E83FBAD742CA");

            entity.ToTable("TUserSettings", "UserSchema");

            entity.HasIndex(e => e.UserId, "UQ__TUserSet__1788CC4D463B9EBA").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnType("numeric(18, 0)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.User).WithOne(p => p.TuserSetting)
                .HasPrincipalKey<Tuser>(p => p.Identifier)
                .HasForeignKey<TuserSetting>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TUserSettings_TUsers");
        });

        modelBuilder.Entity<TuserToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TUserTok__3214EC07FD628A19");

            entity.ToTable("TUserTokens", "UserSchema");

            entity.HasIndex(e => e.UserId, "UQ__TUserTok__1788CC4DD387EE3A").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnType("numeric(18, 0)");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.RefreshToken).IsUnicode(false);
            entity.Property(e => e.RefreshTokenExpirayTime).HasColumnType("datetime");
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.User).WithOne(p => p.TuserToken)
                .HasPrincipalKey<Tuser>(p => p.Identifier)
                .HasForeignKey<TuserToken>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TUserTokens_TUsers");
        });

        modelBuilder.Entity<TusersOrganization>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TUsersOr__3213E83FC16735C1");

            entity.ToTable("TUsersOrganizations", "UserSchema");

            entity.HasIndex(e => e.UserId, "UQ__TUsersOr__1788CC4D167DFB8B").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnType("numeric(18, 0)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.ModifiedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Version)
                .IsRowVersion()
                .IsConcurrencyToken();

            entity.HasOne(d => d.User).WithOne(p => p.TusersOrganization)
                .HasPrincipalKey<Tuser>(p => p.Identifier)
                .HasForeignKey<TusersOrganization>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TUsersOrganizations_TUsers");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}