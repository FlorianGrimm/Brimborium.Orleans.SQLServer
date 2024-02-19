IF (OBJECT_ID('[dbo].[DeleteReminderRowKey]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[DeleteReminderRowKey] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[DeleteReminderRowKey] (
    @ServiceId nvarchar (150),
    @GrainId varchar (150),
    @ReminderName nvarchar (150),
    @Version int
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
   @ServiceId nvarchar (150)
)
AS BEGIN 

    DELETE FROM OrleansRemindersTable
    WHERE
        ServiceId = @ServiceId AND @ServiceId IS NOT NULL;

END;
GO

IF (OBJECT_ID('[dbo].[ReadReminderRowKey]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[ReadReminderRowKey] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[ReadReminderRowKey] (
   @ServiceId nvarchar (150),
   @GrainId varchar (150),
   @ReminderName nvarchar (150)
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
        AND GrainId = @GrainId AND @GrainId IS NOT NULL
        AND ReminderName = @ReminderName AND @ReminderName IS NOT NULL;

END;
GO

IF (OBJECT_ID('[dbo].[ReadReminderRowsKey]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[ReadReminderRowsKey] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[ReadReminderRowsKey] (
   @ServiceId nvarchar (150),
   @GrainId varchar (150)
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
        AND GrainId = @GrainId AND @GrainId IS NOT NULL;

END;
GO

IF (OBJECT_ID('[dbo].[UpsertReminderRowKey]') IS NULL) BEGIN
  EXECUTE sys.sp_executesql N'CREATE PROCEDURE [dbo].[UpsertReminderRowKey] AS BEGIN SET NOCOUNT ON; END;';
END;
GO
ALTER PROCEDURE [dbo].[UpsertReminderRowKey] (
   @ServiceId nvarchar (150),
   @GrainId varchar (150),
   @ReminderName nvarchar (150),
   @StartTime datetime2,
   @Period bigint,
   @GrainHash int
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

