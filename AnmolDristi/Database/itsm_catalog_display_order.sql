-- ========================================================================
-- When: 2026-11-26
-- What: Add itsm.MST_Category.SortOrder, itsm.MST_SubCategory.SortOrder,
--       and itsm.MST_Item.SortOrder. Each column is INT NOT NULL,
--       default 0. These tables have no CompanyId column.
-- Why:  Category configuration stores display order, and Create Ticket
--       reads the same columns. The catalog is global. On first add, rows
--       that are still 0 are numbered from the existing name order so the
--       lists do not jump. Equal values later tie-break by Id.
--       This script is not executed by the application build.
-- ========================================================================

SET NOCOUNT ON;
GO

IF COL_LENGTH(N'itsm.MST_Category', N'SortOrder') IS NULL
BEGIN
    ALTER TABLE itsm.MST_Category
        ADD SortOrder INT NOT NULL
            CONSTRAINT DF_itsm_MST_Category_SortOrder DEFAULT (0);

    ;WITH ordered AS (
        SELECT Id, ROW_NUMBER() OVER (ORDER BY Name, Id) AS DisplayOrder
        FROM itsm.MST_Category
    )
    UPDATE target
        SET SortOrder = ordered.DisplayOrder
    FROM itsm.MST_Category AS target
    INNER JOIN ordered ON ordered.Id = target.Id;
END
GO

IF COL_LENGTH(N'itsm.MST_SubCategory', N'SortOrder') IS NULL
BEGIN
    ALTER TABLE itsm.MST_SubCategory
        ADD SortOrder INT NOT NULL
            CONSTRAINT DF_itsm_MST_SubCategory_SortOrder DEFAULT (0);

    ;WITH ordered AS (
        SELECT Id, ROW_NUMBER() OVER (PARTITION BY CategoryId ORDER BY Name, Id) AS DisplayOrder
        FROM itsm.MST_SubCategory
    )
    UPDATE target
        SET SortOrder = ordered.DisplayOrder
    FROM itsm.MST_SubCategory AS target
    INNER JOIN ordered ON ordered.Id = target.Id;
END
GO

IF COL_LENGTH(N'itsm.MST_Item', N'SortOrder') IS NULL
BEGIN
    ALTER TABLE itsm.MST_Item
        ADD SortOrder INT NOT NULL
            CONSTRAINT DF_itsm_MST_Item_SortOrder DEFAULT (0);

    ;WITH ordered AS (
        SELECT Id, ROW_NUMBER() OVER (PARTITION BY SubCategoryId ORDER BY Name, Id) AS DisplayOrder
        FROM itsm.MST_Item
    )
    UPDATE target
        SET SortOrder = ordered.DisplayOrder
    FROM itsm.MST_Item AS target
    INNER JOIN ordered ON ordered.Id = target.Id;
END
GO
