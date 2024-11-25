CREATE TABLE [UserSchema].[TUsersOrganizations]
(
	[id] NUMERIC IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[UserId] UNIQUEIDENTIFIER NOT NULL,
	[OrgId] UNIQUEIDENTIFIER NOT NULL,
	[CreatedDate] DATETIME NULL DEFAULT(GETDATE()),
    [ModifiedDate] DATETIME NULL DEFAULT(GETDATE()),
    [Version] ROWVERSION NULL
)
