/*
 NEXUS HITO 4 - OPERACION MEMORIA
 Script 01: creacion de base de datos y esquema relacional.
 Motor: Microsoft SQL Server
 Ejecutar en SQL Server Management Studio con permisos para crear bases de datos.
*/
IF DB_ID(N'NEXUS_DB') IS NULL CREATE DATABASE NEXUS_DB;
GO
USE NEXUS_DB;
GO

IF OBJECT_ID(N'dbo.ExplorerProfile', N'U') IS NULL
CREATE TABLE dbo.ExplorerProfile (
 ExplorerId INT IDENTITY(1,1) CONSTRAINT PK_ExplorerProfile PRIMARY KEY,
 Name NVARCHAR(80) NOT NULL,
 Age TINYINT NOT NULL,
 Gender NVARCHAR(40) NULL,
 CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ExplorerProfile_CreatedAt DEFAULT SYSUTCDATETIME(),
 CONSTRAINT CK_ExplorerProfile_Age CHECK (Age BETWEEN 1 AND 100)
);
GO

IF OBJECT_ID(N'dbo.Reality', N'U') IS NULL
CREATE TABLE dbo.Reality (
 RealityId INT IDENTITY(1,1) CONSTRAINT PK_Reality PRIMARY KEY,
 Code VARCHAR(20) NOT NULL CONSTRAINT UQ_Reality_Code UNIQUE,
 Name NVARCHAR(80) NOT NULL,
 Description NVARCHAR(250) NULL,
 MapDefinition NVARCHAR(MAX) NULL,
 CONSTRAINT CK_Reality_MapJson CHECK (MapDefinition IS NULL OR ISJSON(MapDefinition) = 1)
);
GO

IF OBJECT_ID(N'dbo.Expedition', N'U') IS NULL
CREATE TABLE dbo.Expedition (
 ExpeditionId INT IDENTITY(1,1) CONSTRAINT PK_Expedition PRIMARY KEY,
 ExplorerId INT NOT NULL,
 RealityId INT NOT NULL,
 CharacterName NVARCHAR(80) NOT NULL,
 Archetype NVARCHAR(30) NOT NULL,
 Status VARCHAR(15) NOT NULL CONSTRAINT DF_Expedition_Status DEFAULT 'Active',
 Energy TINYINT NOT NULL,
 Connection TINYINT NOT NULL CONSTRAINT DF_Expedition_Connection DEFAULT 100,
 Danger TINYINT NOT NULL,
 Health TINYINT NOT NULL,
 Shield TINYINT NOT NULL,
 InvestigationsIRIS INT NOT NULL CONSTRAINT DF_Expedition_IRIS DEFAULT 0,
 Resources INT NOT NULL CONSTRAINT DF_Expedition_Resources DEFAULT 0,
 PositionRow INT NULL,
 PositionColumn INT NULL,
 StartedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Expedition_StartedAt DEFAULT SYSUTCDATETIME(),
 UpdatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_Expedition_UpdatedAt DEFAULT SYSUTCDATETIME(),
 RowVersion ROWVERSION NOT NULL,
 CONSTRAINT FK_Expedition_Explorer FOREIGN KEY (ExplorerId) REFERENCES dbo.ExplorerProfile(ExplorerId),
 CONSTRAINT FK_Expedition_Reality FOREIGN KEY (RealityId) REFERENCES dbo.Reality(RealityId),
 CONSTRAINT CK_Expedition_Status CHECK (Status IN ('Active','Completed','Abandoned')),
 CONSTRAINT CK_Expedition_Archetype CHECK (Archetype IN ('Humano','Robot','Enemigo','EnemigoRobot')),
 CONSTRAINT CK_Expedition_Energy CHECK (Energy BETWEEN 0 AND 100),
 CONSTRAINT CK_Expedition_Connection CHECK (Connection BETWEEN 0 AND 100),
 CONSTRAINT CK_Expedition_Danger CHECK (Danger BETWEEN 0 AND 100),
 CONSTRAINT CK_Expedition_Health CHECK (Health BETWEEN 0 AND 100),
 CONSTRAINT CK_Expedition_Shield CHECK (Shield BETWEEN 0 AND 100),
 CONSTRAINT CK_Expedition_Counters CHECK (InvestigationsIRIS >= 0 AND Resources >= 0)
);
GO

IF OBJECT_ID(N'dbo.MissionDefinition', N'U') IS NULL
CREATE TABLE dbo.MissionDefinition (
 MissionId INT IDENTITY(1,1) CONSTRAINT PK_MissionDefinition PRIMARY KEY,
 Code VARCHAR(30) NOT NULL CONSTRAINT UQ_MissionDefinition_Code UNIQUE,
 Name NVARCHAR(120) NOT NULL,
 Description NVARCHAR(500) NULL
);
GO

IF OBJECT_ID(N'dbo.MissionObjective', N'U') IS NULL
CREATE TABLE dbo.MissionObjective (
 ObjectiveId INT IDENTITY(1,1) CONSTRAINT PK_MissionObjective PRIMARY KEY,
 MissionId INT NOT NULL,
 Code VARCHAR(30) NOT NULL,
 SortOrder SMALLINT NOT NULL,
 Description NVARCHAR(250) NOT NULL,
 CONSTRAINT FK_MissionObjective_Mission FOREIGN KEY (MissionId) REFERENCES dbo.MissionDefinition(MissionId),
 CONSTRAINT UQ_MissionObjective_Code UNIQUE (MissionId, Code),
 CONSTRAINT UQ_MissionObjective_Order UNIQUE (MissionId, SortOrder)
);
GO

IF OBJECT_ID(N'dbo.ExpeditionMission', N'U') IS NULL
CREATE TABLE dbo.ExpeditionMission (
 ExpeditionMissionId INT IDENTITY(1,1) CONSTRAINT PK_ExpeditionMission PRIMARY KEY,
 ExpeditionId INT NOT NULL,
 MissionId INT NOT NULL,
 Status VARCHAR(15) NOT NULL CONSTRAINT DF_ExpeditionMission_Status DEFAULT 'Active',
 Experience INT NOT NULL CONSTRAINT DF_ExpeditionMission_Experience DEFAULT 0,
 StartedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ExpeditionMission_StartedAt DEFAULT SYSUTCDATETIME(),
 CompletedAt DATETIME2(0) NULL,
 CONSTRAINT FK_ExpeditionMission_Expedition FOREIGN KEY (ExpeditionId) REFERENCES dbo.Expedition(ExpeditionId),
 CONSTRAINT FK_ExpeditionMission_Mission FOREIGN KEY (MissionId) REFERENCES dbo.MissionDefinition(MissionId),
 CONSTRAINT UQ_ExpeditionMission UNIQUE (ExpeditionId, MissionId),
 CONSTRAINT CK_ExpeditionMission_Status CHECK (Status IN ('Active','Completed','Failed')),
 CONSTRAINT CK_ExpeditionMission_Experience CHECK (Experience >= 0)
);
GO

IF OBJECT_ID(N'dbo.ObjectiveProgress', N'U') IS NULL
CREATE TABLE dbo.ObjectiveProgress (
 ExpeditionMissionId INT NOT NULL,
 ObjectiveId INT NOT NULL,
 ProgressValue INT NOT NULL CONSTRAINT DF_ObjectiveProgress_Value DEFAULT 0,
 CompletedAt DATETIME2(0) NULL,
 CONSTRAINT PK_ObjectiveProgress PRIMARY KEY (ExpeditionMissionId, ObjectiveId),
 CONSTRAINT FK_ObjectiveProgress_ExpeditionMission FOREIGN KEY (ExpeditionMissionId) REFERENCES dbo.ExpeditionMission(ExpeditionMissionId),
 CONSTRAINT FK_ObjectiveProgress_Objective FOREIGN KEY (ObjectiveId) REFERENCES dbo.MissionObjective(ObjectiveId),
 CONSTRAINT CK_ObjectiveProgress_Value CHECK (ProgressValue >= 0)
);
GO

IF OBJECT_ID(N'dbo.ItemDefinition', N'U') IS NULL
CREATE TABLE dbo.ItemDefinition (
 ItemId INT IDENTITY(1,1) CONSTRAINT PK_ItemDefinition PRIMARY KEY,
 Code VARCHAR(30) NOT NULL CONSTRAINT UQ_ItemDefinition_Code UNIQUE,
 Name NVARCHAR(100) NOT NULL,
 Type NVARCHAR(40) NOT NULL,
 Stackable BIT NOT NULL CONSTRAINT DF_ItemDefinition_Stackable DEFAULT 1,
 EffectData NVARCHAR(300) NULL
);
GO

IF OBJECT_ID(N'dbo.InventoryItem', N'U') IS NULL
CREATE TABLE dbo.InventoryItem (
 ExpeditionId INT NOT NULL,
 ItemId INT NOT NULL,
 Quantity INT NOT NULL,
 CONSTRAINT PK_InventoryItem PRIMARY KEY (ExpeditionId, ItemId),
 CONSTRAINT FK_InventoryItem_Expedition FOREIGN KEY (ExpeditionId) REFERENCES dbo.Expedition(ExpeditionId),
 CONSTRAINT FK_InventoryItem_Item FOREIGN KEY (ItemId) REFERENCES dbo.ItemDefinition(ItemId),
 CONSTRAINT CK_InventoryItem_Quantity CHECK (Quantity > 0)
);
GO

IF OBJECT_ID(N'dbo.GameEvent', N'U') IS NULL
CREATE TABLE dbo.GameEvent (
 EventId BIGINT IDENTITY(1,1) CONSTRAINT PK_GameEvent PRIMARY KEY,
 ExpeditionId INT NOT NULL,
 Type NVARCHAR(40) NOT NULL,
 Description NVARCHAR(500) NOT NULL,
 OccurredAt DATETIME2(0) NOT NULL CONSTRAINT DF_GameEvent_OccurredAt DEFAULT SYSUTCDATETIME(),
 CONSTRAINT FK_GameEvent_Expedition FOREIGN KEY (ExpeditionId) REFERENCES dbo.Expedition(ExpeditionId)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_Expedition_Explorer_Status_Updated' AND object_id=OBJECT_ID(N'dbo.Expedition'))
 CREATE INDEX IX_Expedition_Explorer_Status_Updated ON dbo.Expedition(ExplorerId, Status, UpdatedAt DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'IX_GameEvent_Expedition_Occurred' AND object_id=OBJECT_ID(N'dbo.GameEvent'))
 CREATE INDEX IX_GameEvent_Expedition_Occurred ON dbo.GameEvent(ExpeditionId, OccurredAt);
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Reality WHERE Code='TIERRA-01')
 INSERT dbo.Reality(Code,Name,Description) VALUES('TIERRA-01',N'Mundo Natural',N'Realidad de origen.');
IF NOT EXISTS (SELECT 1 FROM dbo.Reality WHERE Code='TIERRA-02')
 INSERT dbo.Reality(Code,Name,Description) VALUES('TIERRA-02',N'Zona Zombi',N'Realidad de alto peligro.');
IF NOT EXISTS (SELECT 1 FROM dbo.Reality WHERE Code='TIERRA-07')
 INSERT dbo.Reality(Code,Name,Description) VALUES('TIERRA-07',N'Mundo Criogenico',N'Realidad de bajas temperaturas.');
IF NOT EXISTS (SELECT 1 FROM dbo.Reality WHERE Code='NEXUS-09')
 INSERT dbo.Reality(Code,Name,Description) VALUES('NEXUS-09',N'Mundo Digital',N'Realidad tecnologica.');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.MissionDefinition WHERE Code='ANOMALIA-PERDIDA')
 INSERT dbo.MissionDefinition(Code,Name,Description) VALUES('ANOMALIA-PERDIDA',N'LA ANOMALIA PERDIDA',N'Mision principal de NEXUS v1.0.');
GO
IF NOT EXISTS (SELECT 1 FROM dbo.ItemDefinition WHERE Code='CELULA-ENERGIA')
 INSERT dbo.ItemDefinition(Code,Name,Type,Stackable,EffectData) VALUES('CELULA-ENERGIA',N'Celula de Energia',N'Energia',1,N'Recuperacion de energia');
IF NOT EXISTS (SELECT 1 FROM dbo.ItemDefinition WHERE Code='ANALIZADOR')
 INSERT dbo.ItemDefinition(Code,Name,Type,Stackable,EffectData) VALUES('ANALIZADOR',N'Analizador',N'Analizador',0,N'Permite analizar la realidad');
GO
PRINT N'NEXUS_DB: esquema y catalogos iniciales preparados.';
GO