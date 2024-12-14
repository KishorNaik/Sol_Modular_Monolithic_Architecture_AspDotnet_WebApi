CREATE TABLE [UserSchema].[TUsers]
(
	[Id] NUMERIC identity(1,1) NOT NULL PRIMARY KEY, 
    [Identifier] UNIQUEIDENTIFIER NOT NULL UNIQUE, 
    [FirstName] VARCHAR(50) NOT NULL, 
    [LastName] VARCHAR(50) NOT NULL, 
    [UserType] INT NOT NULL, 
    [Status] BIT NOT NULL,
    [CreatedDate] DATETIME NULL DEFAULT GETDATE(),
    [ModifiedDate] DATETIME NULL DEFAULT GETDATE(),
    [Version] ROWVERSION NULL,
)
