CREATE DATABASE ActionsTiming;
GO

CREATE LOGIN Actions_Login WITH PASSWORD = 'VerbsRFun';

USE ActionsTiming;
	
CREATE USER Actions_User FOR LOGIN Actions_Login;

EXEC sp_addRoleMember
	@roleName = 'db_datareader'
	, @memberName = 'Actions_User';

EXEC sp_addRoleMember
	@roleName = 'db_datawriter'
	, @memberName = 'Actions_User';

