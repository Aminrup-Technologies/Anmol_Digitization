/*
    Object:      [dbo].[SalesDailyActivity]
    Type:        TABLE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TABLE [dbo].[SalesDailyActivity] (
    [ActivityId] int IDENTITY(1,1) NOT NULL,
    [ImportedFromStagingId] int NULL,
    [ImportBatchId] uniqueidentifier NULL,
    [User_Id] nvarchar(100) NOT NULL,
    [User_Name] nvarchar(255) NULL,
    [Login_Id] nvarchar(100) NULL,
    [Zone] nvarchar(max) NULL,
    [Reporting_Manager1] nvarchar(255) NULL,
    [Reporting_Manager1_Code] nvarchar(50) NULL,
    [Reporting_Manager2] nvarchar(255) NULL,
    [Reporting_Manager2_Code] nvarchar(50) NULL,
    [Reporting_Manager3] nvarchar(255) NULL,
    [Reporting_Manager3_Code] nvarchar(50) NULL,
    [ActivityDate] date NOT NULL,
    [Day] nvarchar(20) NULL,
    [Month] nvarchar(50) NULL,
    [Attendance_Status] nvarchar(100) NULL,
    [TotalDistanceKM] decimal(10,2) NULL,
    [PrimaryOrderValue] decimal(18,2) NULL,
    [SecondaryOrderValue] decimal(18,2) NULL,
    [CalculatedClaimAmount] decimal(18,2) NULL,
    [ClaimStatus] nvarchar(50) NULL,
    [ClaimProcessedBy] nvarchar(100) NULL,
    [ClaimProcessedDate] datetime NULL,
    [IsFlagged] bit NOT NULL CONSTRAINT [DF__SalesDail__IsFla__202DAF9E] DEFAULT ((0)),
    [Flags] nvarchar(1000) NULL,
    [CreatedByUserId] nvarchar(100) NULL,
    [CreatedDateTime] datetime NOT NULL CONSTRAINT [DF__SalesDail__Creat__2121D3D7] DEFAULT (dateadd(minute,(330),sysutcdatetime())),
    [ModifiedByUserId] nvarchar(100) NULL,
    [ModifiedDateTime] datetime NULL,
    [IsDeleted] bit NOT NULL CONSTRAINT [DF__SalesDail__IsDel__2215F810] DEFAULT ((0)),
    [Remarks] nvarchar(max) NULL,
    [SubmittedBy] nvarchar(100) NULL,
    [SubmittedOn] datetime2(7) NULL,
    [ApprovalRequestId] int NULL,
    [RowVersion] timestamp NOT NULL,
    [Planned_Beat] nvarchar(max) NULL,
    [Planned_Beat_Id] nvarchar(max) NULL,
    [Order_Distributor_ERPID] nvarchar(max) NULL,
    [Order_Distributor_Name] nvarchar(max) NULL,
    [ClaimedDA] decimal(18,2) NULL CONSTRAINT [DF__SalesDail__Claim__18E19391] DEFAULT ((0)),
    [ApprovedDA] decimal(18,2) NULL CONSTRAINT [DF__SalesDail__Appro__19D5B7CA] DEFAULT ((0)),
    [RejectedDA] decimal(18,2) NULL CONSTRAINT [DF__SalesDail__Rejec__1AC9DC03] DEFAULT ((0)),
    [ClaimedHotel] decimal(18,2) NULL CONSTRAINT [DF__SalesDail__Claim__1BBE003C] DEFAULT ((0)),
    [ApprovedHotel] decimal(18,2) NULL CONSTRAINT [DF__SalesDail__Appro__1CB22475] DEFAULT ((0)),
    [RejectedHotel] decimal(18,2) NULL CONSTRAINT [DF__SalesDail__Rejec__1DA648AE] DEFAULT ((0)),
    [ClaimedTravel] decimal(18,2) NULL CONSTRAINT [DF__SalesDail__Claim__1E9A6CE7] DEFAULT ((0)),
    [ApprovedTravel] decimal(18,2) NULL CONSTRAINT [DF__SalesDail__Appro__1F8E9120] DEFAULT ((0)),
    [RejectedTravel] decimal(18,2) NULL CONSTRAINT [DF__SalesDail__Rejec__2082B559] DEFAULT ((0)),
    [RouteOriginName] nvarchar(255) NULL,
    [RouteDestinationName] nvarchar(255) NULL,
    [RouteWaypointsJSON] nvarchar(max) NULL,
    [TravelMode] nvarchar(50) NULL,
    [ApprovedDistanceKM] decimal(10,2) NULL CONSTRAINT [DF__SalesDail__Appro__6C98FCFF] DEFAULT ((0)),
    [ApprovedCalculatedClaimAmount] decimal(18,2) NULL CONSTRAINT [DF__SalesDail__Appro__6D8D2138] DEFAULT ((0))
);
GO
ALTER TABLE [dbo].[SalesDailyActivity] ADD CONSTRAINT [PK__SalesDai__45F4A791F5994F8B] PRIMARY KEY CLUSTERED ([ActivityId] ASC);
ALTER TABLE [dbo].[SalesDailyActivity] ADD CONSTRAINT [UQ_SalesDailyActivity_UserDate] UNIQUE ([User_Id] ASC, [ActivityDate] ASC);
ALTER TABLE [dbo].[SalesDailyActivity] ADD CONSTRAINT [CK_SalesDailyActivity_ClaimStatus] CHECK ([ClaimStatus]='Rejected' OR [ClaimStatus]='Approved' OR [ClaimStatus]='Returned' OR [ClaimStatus]='Submitted' OR [ClaimStatus]='Draft');
