BEGIN TRANSACTION

CREATE TABLE [Presence] (
[UserID] varchar(255) NOT NULL, 
[RegionID] uniqueidentifier NOT NULL, 
[SessionID] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
[SecureSessionID] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
[Online] char(5) NOT NULL DEFAULT 'false',
[Login] char(16) NOT NULL DEFAULT '0',
[Logout] char(16) NOT NULL DEFAULT '0',
[Position] char(64) NOT NULL DEFAULT '<0,0,0>',
[LookAt] char(64) NOT NULL DEFAULT '<0,0,0>',
[HomeRegionID] uniqueidentifier NOT NULL,
[HomePosition] CHAR(64) NOT NULL DEFAULT '<0,0,0>',
[HomeLookAt] CHAR(64) NOT NULL DEFAULT '<0,0,0>',
)
 ON [PRIMARY]

COMMIT