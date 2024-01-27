USE [Orleans]
GO
/****** Object:  Table [dbo].[OrleansMembershipTable]    Script Date: 1/27/2024 5:11:18 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[OrleansMembershipTable](
	[DeploymentId] [nvarchar](150) NOT NULL,
	[Address] [varchar](45) NOT NULL,
	[Port] [int] NOT NULL,
	[Generation] [int] NOT NULL,
	[SiloName] [nvarchar](150) NOT NULL,
	[HostName] [nvarchar](150) NOT NULL,
	[Status] [int] NOT NULL,
	[ProxyPort] [int] NULL,
	[SuspectTimes] [varchar](8000) NULL,
	[StartTime] [datetime2](3) NOT NULL,
	[IAmAliveTime] [datetime2](3) NOT NULL,
 CONSTRAINT [PK_MembershipTable_DeploymentId] PRIMARY KEY CLUSTERED 
(
	[DeploymentId] ASC,
	[Address] ASC,
	[Port] ASC,
	[Generation] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[OrleansMembershipTable]  WITH CHECK ADD  CONSTRAINT [FK_MembershipTable_MembershipVersionTable_DeploymentId] FOREIGN KEY([DeploymentId])
REFERENCES [dbo].[OrleansMembershipVersionTable] ([DeploymentId])
GO
ALTER TABLE [dbo].[OrleansMembershipTable] CHECK CONSTRAINT [FK_MembershipTable_MembershipVersionTable_DeploymentId]
GO
