USE [AdventureWorksLT2019]
GO

/****** Object:  Table [dbo].[EntityLog]    Script Date: 1/28/2024 9:26:02 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[EntityLog](
	[EntityLogKey] [char](30) NOT NULL,
	[EntityId] [int] NOT NULL,
	[SimTime] [real] NOT NULL,
	[Action] [nvarchar](50) NOT NULL,
	[Information] [nvarchar](max) NULL,
 CONSTRAINT [PK_EntityLog] PRIMARY KEY CLUSTERED 
(
	[EntityLogKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

