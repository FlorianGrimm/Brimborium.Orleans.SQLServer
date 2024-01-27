USE [Orleans]
GO
/****** Object:  Table [dbo].[OrleansStorage]    Script Date: 1/27/2024 5:11:18 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OrleansStorage](
	[GrainIdHash] [int] NOT NULL,
	[GrainIdN0] [bigint] NOT NULL,
	[GrainIdN1] [bigint] NOT NULL,
	[GrainTypeHash] [int] NOT NULL,
	[GrainTypeString] [nvarchar](512) NOT NULL,
	[GrainIdExtensionString] [nvarchar](512) NULL,
	[ServiceId] [nvarchar](150) NOT NULL,
	[PayloadBinary] [varbinary](max) NULL,
	[ModifiedOn] [datetime2](3) NOT NULL,
	[Version] [int] NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[OrleansStorage] SET (LOCK_ESCALATION = DISABLE)
GO
