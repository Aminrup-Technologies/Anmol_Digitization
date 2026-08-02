/*
    Object:      [dbo].[TVP_ActivityUpdate]
    Type:        TABLE TYPE
    Extracted:   2026-08-02 16:02:17 (UTC), from AnmolDristiDev
    Source:      Live SQL Server instance (production-baseline extraction)
    Note:        Extracted verbatim from the deployed database. No logic,
                 formatting, or object names have been altered.
*/

CREATE TYPE [dbo].[TVP_ActivityUpdate] AS TABLE (
    [ActivityId] int NOT NULL,
    [TotalDistanceKM] decimal(12,2) NULL,
    [CalculatedClaimAmount] decimal(18,2) NULL,
    PRIMARY KEY CLUSTERED ([ActivityId] ASC)
);
