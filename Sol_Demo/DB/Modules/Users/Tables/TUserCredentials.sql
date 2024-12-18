﻿CREATE TABLE [UserSchema].[TUserCredentials]
(
	[Id] NUMERIC identity(1,1) NOT NULL PRIMARY KEY,
	[UserId] UNIQUEIDENTIFIER NOT NULL UNIQUE,
	Salt VARCHAR(MAX) NOT NULL,
	Hash VARCHAR(MAX) NOT NULL,
	ClientId UNIQUEIDENTIFIER NOT NULL,
	AesSecretKey VARCHAR(MAX) NOT NULL,
	HmacSecretKey VARCHAR(MAX) NOT NULL,
	[CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
	[ModifiedDate] DATETIME NOT NULL DEFAULT GETDATE(),
	[Version] ROWVERSION NULL
	CONSTRAINT FK_TUserCredentials_TUsers FOREIGN KEY (UserId) REFERENCES UserSchema.TUsers(Identifier)
	ON UPDATE CASCADE
	ON DELETE CASCADE
)
