/* Esquema relacional reproducible de NEXUS. Ejecutar en la base NEXUS. */
USE [NEXUS];

IF OBJECT_ID(N'dbo.ExplorerProfile', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExplorerProfile (
        ExplorerId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExplorerProfile PRIMARY KEY,
        Name nvarchar(80) NOT NULL CONSTRAINT UQ_ExplorerProfile_Name UNIQUE,
        Gender nvarchar(40) NOT NULL,
        Age int NOT NULL CONSTRAINT CK_ExplorerProfile_Age CHECK (Age BETWEEN 1 AND 100),
        Fitness int NOT NULL CONSTRAINT CK_ExplorerProfile_Fitness CHECK (Fitness BETWEEN 0 AND 100),
        CharacterId int NOT NULL CONSTRAINT CK_ExplorerProfile_Character CHECK (CharacterId BETWEEN 1 AND 4),
        CreatedAt datetime2(0) NOT NULL CONSTRAINT DF_ExplorerProfile_Created DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID(N'dbo.Reality', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reality (
        RealityId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reality PRIMARY KEY,
        Code nvarchar(32) NOT NULL CONSTRAINT UQ_Reality_Code UNIQUE,
        Name nvarchar(80) NOT NULL
    );
END;

IF OBJECT_ID(N'dbo.MissionDefinition', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MissionDefinition (
        MissionId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MissionDefinition PRIMARY KEY,
        Name nvarchar(120) NOT NULL CONSTRAINT UQ_MissionDefinition_Name UNIQUE,
        Description nvarchar(500) NOT NULL CONSTRAINT DF_Mission_Description DEFAULT N'Misión principal NEXUS'
    );
END;

IF OBJECT_ID(N'dbo.MissionObjective', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MissionObjective (
        ObjectiveId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MissionObjective PRIMARY KEY,
        MissionId int NOT NULL,
        ObjectiveOrder tinyint NOT NULL CONSTRAINT CK_MissionObjective_Order CHECK (ObjectiveOrder BETWEEN 1 AND 4),
        Description nvarchar(200) NOT NULL,
        CONSTRAINT UQ_MissionObjective_Mission_Order UNIQUE(MissionId, ObjectiveOrder),
        CONSTRAINT FK_MissionObjective_Mission FOREIGN KEY(MissionId) REFERENCES dbo.MissionDefinition(MissionId)
    );
END;

IF OBJECT_ID(N'dbo.Expedition', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Expedition (
        ExpeditionId uniqueidentifier NOT NULL CONSTRAINT PK_Expedition PRIMARY KEY,
        ExplorerId int NOT NULL,
        SaveName nvarchar(80) NOT NULL,
        RealityId int NOT NULL,
        MissionId int NOT NULL,
        Status nvarchar(16) NOT NULL CONSTRAINT CK_Expedition_Status CHECK (Status IN (N'Active',N'Paused',N'Completed')),
        Energy int NOT NULL CONSTRAINT CK_Expedition_Energy CHECK (Energy BETWEEN 0 AND 1000),
        ConnectionValue int NOT NULL CONSTRAINT CK_Expedition_Connection CHECK (ConnectionValue BETWEEN 0 AND 100),
        Danger int NOT NULL CONSTRAINT CK_Expedition_Danger CHECK (Danger BETWEEN 0 AND 100),
        PositionRow int NOT NULL,
        PositionColumn int NOT NULL,
        SnapshotJson nvarchar(max) NOT NULL,
        StartedAt datetime2(0) NOT NULL CONSTRAINT DF_Expedition_Started DEFAULT SYSUTCDATETIME(),
        UpdatedAt datetime2(0) NOT NULL CONSTRAINT DF_Expedition_Updated DEFAULT SYSUTCDATETIME(),
        RowVersion rowversion NOT NULL,
        CONSTRAINT FK_Expedition_Explorer FOREIGN KEY (ExplorerId) REFERENCES dbo.ExplorerProfile(ExplorerId),
        CONSTRAINT FK_Expedition_Reality FOREIGN KEY (RealityId) REFERENCES dbo.Reality(RealityId),
        CONSTRAINT FK_Expedition_Mission FOREIGN KEY (MissionId) REFERENCES dbo.MissionDefinition(MissionId),
        CONSTRAINT UQ_Expedition_Explorer_Save UNIQUE (ExplorerId, SaveName)
    );
    CREATE INDEX IX_Expedition_Explorer_Status_Updated ON dbo.Expedition(ExplorerId, Status, UpdatedAt DESC);
END;

IF OBJECT_ID(N'dbo.ItemDefinition', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItemDefinition (
        ItemId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ItemDefinition PRIMARY KEY,
        Name nvarchar(100) NOT NULL,
        TypeName nvarchar(40) NOT NULL,
        CONSTRAINT UQ_ItemDefinition_Name_Type UNIQUE(Name, TypeName)
    );
END;

IF OBJECT_ID(N'dbo.InventoryItem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryItem (
        ExpeditionId uniqueidentifier NOT NULL,
        ItemId int NOT NULL,
        Quantity int NOT NULL CONSTRAINT CK_InventoryItem_Quantity CHECK (Quantity > 0),
        CONSTRAINT PK_InventoryItem PRIMARY KEY (ExpeditionId, ItemId),
        CONSTRAINT FK_InventoryItem_Expedition FOREIGN KEY (ExpeditionId) REFERENCES dbo.Expedition(ExpeditionId) ON DELETE CASCADE,
        CONSTRAINT FK_InventoryItem_Item FOREIGN KEY (ItemId) REFERENCES dbo.ItemDefinition(ItemId)
    );
END;

IF OBJECT_ID(N'dbo.ExpeditionMission', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExpeditionMission (
        ExpeditionMissionId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExpeditionMission PRIMARY KEY,
        ExpeditionId uniqueidentifier NOT NULL,
        MissionId int NOT NULL,
        Status nvarchar(16) NOT NULL CONSTRAINT CK_ExpeditionMission_Status CHECK (Status IN(N'Active',N'Completed')),
        Experience int NOT NULL CONSTRAINT CK_ExpeditionMission_Experience CHECK (Experience >= 0),
        CONSTRAINT UQ_ExpeditionMission_Expedition_Mission UNIQUE(ExpeditionId,MissionId),
        CONSTRAINT FK_ExpeditionMission_Expedition FOREIGN KEY(ExpeditionId) REFERENCES dbo.Expedition(ExpeditionId) ON DELETE CASCADE,
        CONSTRAINT FK_ExpeditionMission_Mission FOREIGN KEY(MissionId) REFERENCES dbo.MissionDefinition(MissionId)
    );
END;

IF OBJECT_ID(N'dbo.ObjectiveProgress', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ObjectiveProgress (
        ExpeditionMissionId int NOT NULL,
        ObjectiveId int NOT NULL,
        Completed bit NOT NULL,
        CompletedAt datetime2(0) NULL,
        CONSTRAINT PK_ObjectiveProgress PRIMARY KEY(ExpeditionMissionId,ObjectiveId),
        CONSTRAINT FK_ObjectiveProgress_ExpeditionMission FOREIGN KEY(ExpeditionMissionId) REFERENCES dbo.ExpeditionMission(ExpeditionMissionId) ON DELETE CASCADE,
        CONSTRAINT FK_ObjectiveProgress_Objective FOREIGN KEY(ObjectiveId) REFERENCES dbo.MissionObjective(ObjectiveId)
    );
END;

IF OBJECT_ID(N'dbo.GameEvent', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.GameEvent (
        EventId bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_GameEvent PRIMARY KEY,
        ExpeditionId uniqueidentifier NOT NULL,
        EventType nvarchar(60) NOT NULL,
        Description nvarchar(1000) NOT NULL,
        OccurredAt datetime2(0) NOT NULL,
        CONSTRAINT FK_GameEvent_Expedition FOREIGN KEY(ExpeditionId) REFERENCES dbo.Expedition(ExpeditionId) ON DELETE CASCADE
    );
    CREATE INDEX IX_GameEvent_Expedition_Occurred ON dbo.GameEvent(ExpeditionId, OccurredAt);
END;

IF NOT EXISTS(SELECT 1 FROM dbo.Reality WHERE Code=N'TIERRA-01') INSERT dbo.Reality(Code,Name) VALUES(N'TIERRA-01',N'Tierra 01');
IF NOT EXISTS(SELECT 1 FROM dbo.Reality WHERE Code=N'TIERRA-02') INSERT dbo.Reality(Code,Name) VALUES(N'TIERRA-02',N'Tierra 02');
IF NOT EXISTS(SELECT 1 FROM dbo.Reality WHERE Code=N'TIERRA-07') INSERT dbo.Reality(Code,Name) VALUES(N'TIERRA-07',N'Tierra 07');
IF NOT EXISTS(SELECT 1 FROM dbo.Reality WHERE Code=N'NEXUS-09') INSERT dbo.Reality(Code,Name) VALUES(N'NEXUS-09',N'Nexus 09');
IF NOT EXISTS(SELECT 1 FROM dbo.MissionDefinition WHERE Name=N'LA ANOMALÍA PERDIDA') INSERT dbo.MissionDefinition(Name) VALUES(N'LA ANOMALÍA PERDIDA');
DECLARE @mainMissionId int = (SELECT MissionId FROM dbo.MissionDefinition WHERE Name=N'LA ANOMALÍA PERDIDA');
IF NOT EXISTS(SELECT 1 FROM dbo.MissionObjective WHERE MissionId=@mainMissionId AND ObjectiveOrder=1) INSERT dbo.MissionObjective(MissionId,ObjectiveOrder,Description) VALUES(@mainMissionId,1,N'Localizar la anomalía');
IF NOT EXISTS(SELECT 1 FROM dbo.MissionObjective WHERE MissionId=@mainMissionId AND ObjectiveOrder=2) INSERT dbo.MissionObjective(MissionId,ObjectiveOrder,Description) VALUES(@mainMissionId,2,N'Recuperar el núcleo');
IF NOT EXISTS(SELECT 1 FROM dbo.MissionObjective WHERE MissionId=@mainMissionId AND ObjectiveOrder=3) INSERT dbo.MissionObjective(MissionId,ObjectiveOrder,Description) VALUES(@mainMissionId,3,N'Superar la entidad');
IF NOT EXISTS(SELECT 1 FROM dbo.MissionObjective WHERE MissionId=@mainMissionId AND ObjectiveOrder=4) INSERT dbo.MissionObjective(MissionId,ObjectiveOrder,Description) VALUES(@mainMissionId,4,N'Completar la extracción');
