USE [Orleans]
GO
/****** Object:  Table [dbo].[OrleansRemindersTable]    Script Date: 1/27/2024 5:11:18 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OrleansRemindersTable](
	[ServiceId] [nvarchar](150) NOT NULL,
	[GrainId] [varchar](150) NOT NULL,
	[ReminderName] [nvarchar](150) NOT NULL,
	[StartTime] [datetime2](3) NOT NULL,
	[Period] [bigint] NOT NULL,
	[GrainHash] [int] NOT NULL,
	[Version] [int] NOT NULL,
 CONSTRAINT [PK_RemindersTable_ServiceId_GrainId_ReminderName] PRIMARY KEY CLUSTERED 
(
	[ServiceId] ASC,
	[GrainId] ASC,
	[ReminderName] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
