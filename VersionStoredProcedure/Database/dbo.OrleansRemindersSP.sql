IF (OBJECT_ID('[dbo].[DeleteReminderRowKey]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[DeleteReminderRowKey] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[DeleteReminderRowKey] (
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

	DELETE FROM OrleansRemindersTable
	WHERE
		ServiceId = @ServiceId AND @ServiceId IS NOT NULL
		AND GrainId = @GrainId AND @GrainId IS NOT NULL
		AND ReminderName = @ReminderName AND @ReminderName IS NOT NULL
		AND Version = @Version AND @Version IS NOT NULL;
	SELECT @@ROWCOUNT;

END;
GO

IF (OBJECT_ID('[dbo].[DeleteReminderRowsKey]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[DeleteReminderRowsKey] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[DeleteReminderRowsKey] (
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

	DELETE FROM OrleansRemindersTable
	WHERE
		ServiceId = @ServiceId AND @ServiceId IS NOT NULL;

END;
GO

IF (OBJECT_ID('[dbo].[UpsertReminderRowKey]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[UpsertReminderRowKey] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[UpsertReminderRowKey] (
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

	DECLARE @Version AS INT = 0;
	SET XACT_ABORT, NOCOUNT ON;
	BEGIN TRANSACTION;
	UPDATE OrleansRemindersTable WITH(UPDLOCK, ROWLOCK, HOLDLOCK)
	SET
		StartTime = @StartTime,
		Period = @Period,
		GrainHash = @GrainHash,
		@Version = Version = Version + 1
	WHERE
		ServiceId = @ServiceId AND @ServiceId IS NOT NULL
		AND GrainId = @GrainId AND @GrainId IS NOT NULL
		AND ReminderName = @ReminderName AND @ReminderName IS NOT NULL;

	INSERT INTO OrleansRemindersTable
	(
		ServiceId,
		GrainId,
		ReminderName,
		StartTime,
		Period,
		GrainHash,
		Version
	)
	SELECT
		@ServiceId,
		@GrainId,
		@ReminderName,
		@StartTime,
		@Period,
		@GrainHash,
		0
	WHERE
		@@ROWCOUNT=0;
	SELECT @Version AS Version;
	COMMIT TRANSACTION;

END;
GO

