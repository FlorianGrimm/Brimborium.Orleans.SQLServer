IF (OBJECT_ID('[dbo].[CleanupDefunctSiloEntriesKey]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[CleanupDefunctSiloEntriesKey] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[CleanupDefunctSiloEntriesKey] 
(
@DeploymentId nvarchar (4000),
@IAmAliveTime datetime2
)
AS BEGIN 

    DELETE FROM OrleansMembershipTable
    WHERE DeploymentId = @DeploymentId
        AND @DeploymentId IS NOT NULL
        AND IAmAliveTime < @IAmAliveTime
        AND Status != 3;

END;
GO

IF (OBJECT_ID('[dbo].[DeleteMembershipTableEntriesKey]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[DeleteMembershipTableEntriesKey] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[DeleteMembershipTableEntriesKey] (
    @DeploymentId nvarchar (4000)
)
AS BEGIN 

	DELETE FROM OrleansMembershipTable
		WHERE DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL;
	DELETE FROM OrleansMembershipVersionTable
		WHERE DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL;

END;
GO

IF (OBJECT_ID('[dbo].[GatewaysQueryKey]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[GatewaysQueryKey] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[GatewaysQueryKey] (
    @DeploymentId nvarchar (4000)
)
AS BEGIN 

	SELECT
		Address,
		ProxyPort,
		Generation
	FROM
		OrleansMembershipTable
	WHERE
		DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
		AND Status = @Status AND @Status IS NOT NULL
		AND ProxyPort > 0;

END;
GO

IF (OBJECT_ID('[dbo].[InsertMembershipKey]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[InsertMembershipKey] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[InsertMembershipKey] (
	@DeploymentId nvarchar (4000),
	@Address varchar (8000),
	@Port int,
	@Generation int,
	@SiloName nvarchar(150),
	@HostName nvarchar(150),
	@ProxyPort int,
	@SuspectTimes varchar (8000),
	@StartTime datetime2,
	@IAmAliveTime datetime2
)
AS BEGIN 

	SET XACT_ABORT, NOCOUNT ON;
	DECLARE @ROWCOUNT AS INT;
	BEGIN TRANSACTION;
	INSERT INTO OrleansMembershipTable
	(
		DeploymentId,
		Address,
		Port,
		Generation,
		SiloName,
		HostName,
		Status,
		ProxyPort,
		StartTime,
		IAmAliveTime
	)
	SELECT
		@DeploymentId,
		@Address,
		@Port,
		@Generation,
		@SiloName,
		@HostName,
		@Status,
		@ProxyPort,
		@StartTime,
		@IAmAliveTime
	WHERE NOT EXISTS
	(
		SELECT 1
		FROM
			OrleansMembershipTable WITH(HOLDLOCK, XLOCK, ROWLOCK)
		WHERE
			DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
			AND Address = @Address AND @Address IS NOT NULL
			AND Port = @Port AND @Port IS NOT NULL
			AND Generation = @Generation AND @Generation IS NOT NULL
	);

	UPDATE OrleansMembershipVersionTable
	SET
		Timestamp = GETUTCDATE(),
		Version = Version + 1
	WHERE
		DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
		AND Version = @Version AND @Version IS NOT NULL
		AND @@ROWCOUNT > 0;

	SET @ROWCOUNT = @@ROWCOUNT;

	IF @ROWCOUNT = 0
		ROLLBACK TRANSACTION
	ELSE
		COMMIT TRANSACTION
	SELECT @ROWCOUNT;

END;
GO

IF (OBJECT_ID('[dbo].[InsertMembershipVersionKey]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[InsertMembershipVersionKey] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[InsertMembershipVersionKey] (
    @DeploymentId nvarchar (4000)
)
AS BEGIN 

	SET NOCOUNT ON;
	INSERT INTO OrleansMembershipVersionTable
	(
		DeploymentId
	)
	SELECT @DeploymentId
	WHERE NOT EXISTS
	(
		SELECT 1
		FROM
			OrleansMembershipVersionTable WITH(HOLDLOCK, XLOCK, ROWLOCK)
		WHERE
			DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
	);

	SELECT @@ROWCOUNT;

END;
GO

IF (OBJECT_ID('[dbo].[MembershipReadAllKey]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[MembershipReadAllKey] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[MembershipReadAllKey] (
    @DeploymentId nvarchar (4000)
)
AS BEGIN 

	SELECT
		v.DeploymentId,
		m.Address,
		m.Port,
		m.Generation,
		m.SiloName,
		m.HostName,
		m.Status,
		m.ProxyPort,
		m.SuspectTimes,
		m.StartTime,
		m.IAmAliveTime,
		v.Version
	FROM
		OrleansMembershipVersionTable v LEFT OUTER JOIN OrleansMembershipTable m
		ON v.DeploymentId = m.DeploymentId
	WHERE
		v.DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL;

END;
GO

IF (OBJECT_ID('[dbo].[MembershipReadRowKey]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[MembershipReadRowKey] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[MembershipReadRowKey] (
    @DeploymentId nvarchar (4000),
    @Address varchar (8000)
)
AS BEGIN 

	SELECT
		v.DeploymentId,
		m.Address,
		m.Port,
		m.Generation,
		m.SiloName,
		m.HostName,
		m.Status,
		m.ProxyPort,
		m.SuspectTimes,
		m.StartTime,
		m.IAmAliveTime,
		v.Version
	FROM
		OrleansMembershipVersionTable v
		-- This ensures the version table will returned even if there is no matching membership row.
		LEFT OUTER JOIN OrleansMembershipTable m ON v.DeploymentId = m.DeploymentId
		AND Address = @Address AND @Address IS NOT NULL
		AND Port = @Port AND @Port IS NOT NULL
		AND Generation = @Generation AND @Generation IS NOT NULL
	WHERE
		v.DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL;

END;
GO

IF (OBJECT_ID('[dbo].[ReadRangeRows1Key]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[ReadRangeRows1Key] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[ReadRangeRows1Key] (
    @ServiceId nvarchar (4000),
    @BeginHash int,
    @EndHash int
)
AS BEGIN 

	SELECT
		GrainId,
		ReminderName,
		StartTime,
		Period,
		Version
	FROM OrleansRemindersTable
	WHERE
		ServiceId = @ServiceId AND @ServiceId IS NOT NULL
		AND GrainHash > @BeginHash AND @BeginHash IS NOT NULL
		AND GrainHash <= @EndHash AND @EndHash IS NOT NULL;

END;
GO

IF (OBJECT_ID('[dbo].[ReadRangeRows2Key]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[ReadRangeRows2Key] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[ReadRangeRows2Key] (
    @ServiceId nvarchar (4000),
    @BeginHash int,
    @EndHash int
)
AS BEGIN 

	SELECT
		GrainId,
		ReminderName,
		StartTime,
		Period,
		Version
	FROM OrleansRemindersTable
	WHERE
		ServiceId = @ServiceId AND @ServiceId IS NOT NULL
		AND ((GrainHash > @BeginHash AND @BeginHash IS NOT NULL)
		OR (GrainHash <= @EndHash AND @EndHash IS NOT NULL));

END;
GO

IF (OBJECT_ID('[dbo].[UpdateIAmAlivetimeKey]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[UpdateIAmAlivetimeKey] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[UpdateIAmAlivetimeKey] (
-- @Address varchar (8000),
-- @DeploymentId nvarchar (4000),
-- @Generation int,
-- @GrainHash int,
-- @GrainId varchar (8000),
-- @GrainIdExtensionString nvarchar (4000),
-- @GrainIdHash int,
-- @GrainIdN0 bigint,
-- @GrainIdN1 bigint,
-- @GrainTypeHash int,
-- @GrainTypeString nvarchar (4000),
-- @IAmAliveTime datetime2,
-- @PayloadBinary varbinary (4000),
-- @Period bigint,
-- @Port int,
-- @ReminderName nvarchar (4000),
-- @ServiceId nvarchar (4000),
-- @StartTime datetime2,
-- @Status int,
-- @SuspectTimes varchar (8000),
-- @Version int,
-- @GrainStateVersion int
)
AS BEGIN 

	-- This is expected to never fail by Orleans, so return value
	-- is not needed nor is it checked.
	SET NOCOUNT ON;
	UPDATE OrleansMembershipTable
	SET
		IAmAliveTime = @IAmAliveTime
	WHERE
		DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
		AND Address = @Address AND @Address IS NOT NULL
		AND Port = @Port AND @Port IS NOT NULL
		AND Generation = @Generation AND @Generation IS NOT NULL;

END;
GO

IF (OBJECT_ID('[dbo].[UpdateMembershipKey]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[UpdateMembershipKey] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[UpdateMembershipKey] (
-- @Address varchar (8000),
-- @DeploymentId nvarchar (4000),
-- @Generation int,
-- @GrainHash int,
-- @GrainId varchar (8000),
-- @GrainIdExtensionString nvarchar (4000),
-- @GrainIdHash int,
-- @GrainIdN0 bigint,
-- @GrainIdN1 bigint,
-- @GrainTypeHash int,
-- @GrainTypeString nvarchar (4000),
-- @IAmAliveTime datetime2,
-- @PayloadBinary varbinary (4000),
-- @Period bigint,
-- @Port int,
-- @ReminderName nvarchar (4000),
-- @ServiceId nvarchar (4000),
-- @StartTime datetime2,
-- @Status int,
-- @SuspectTimes varchar (8000),
-- @Version int,
-- @GrainStateVersion int
)
AS BEGIN 

	SET XACT_ABORT, NOCOUNT ON;
	BEGIN TRANSACTION;

	UPDATE OrleansMembershipVersionTable
	SET
		Timestamp = GETUTCDATE(),
		Version = Version + 1
	WHERE
		DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
		AND Version = @Version AND @Version IS NOT NULL;

	UPDATE OrleansMembershipTable
	SET
		Status = @Status,
		SuspectTimes = @SuspectTimes,
		IAmAliveTime = @IAmAliveTime
	WHERE
		DeploymentId = @DeploymentId AND @DeploymentId IS NOT NULL
		AND Address = @Address AND @Address IS NOT NULL
		AND Port = @Port AND @Port IS NOT NULL
		AND Generation = @Generation AND @Generation IS NOT NULL
		AND @@ROWCOUNT > 0;

	SELECT @@ROWCOUNT;
	COMMIT TRANSACTION;

END;
GO

