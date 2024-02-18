CREATE TABLE [dbo].[OrleansMembershipVersionTable](
	[DeploymentId] [nvarchar](150) NOT NULL,
	[Timestamp] [datetime2](3) NOT NULL,
	[Version] [int] NOT NULL,
 CONSTRAINT [PK_OrleansMembershipVersionTable_DeploymentId] PRIMARY KEY CLUSTERED 
(
	[DeploymentId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[OrleansMembershipVersionTable] ADD  DEFAULT (getutcdate()) FOR [Timestamp]
GO
ALTER TABLE [dbo].[OrleansMembershipVersionTable] ADD  DEFAULT ((0)) FOR [Version]
GO
