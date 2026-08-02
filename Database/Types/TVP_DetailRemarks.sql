/*
    Object:      [dbo].[TVP_DetailRemarks]
    Type:        TABLE TYPE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TYPE [dbo].[TVP_DetailRemarks] AS TABLE (
    [ApprovalRequestDetailId] int NOT NULL,
    [Remark] nvarchar(1000) NULL
);
