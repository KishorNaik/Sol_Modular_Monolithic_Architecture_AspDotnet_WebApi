CREATE TABLE [UserSchema].[TUsersOrganizations]
(
	[Id] NUMERIC IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[UserId] UNIQUEIDENTIFIER NOT NULL UNIQUE,
	[OrgId] UNIQUEIDENTIFIER NOT NULL,
	[CreatedDate] DATETIME NULL DEFAULT(GETDATE()),
    [ModifiedDate] DATETIME NULL DEFAULT(GETDATE()),
    [Version] ROWVERSION NULL
	CONSTRAINT [FK_TUsersOrganizations_TUsers] FOREIGN KEY (UserId) REFERENCES UserSchema.TUsers(Identifier)
	ON UPDATE CASCADE	
	ON DELETE CASCADE
)
