/* =============================================================================================
   Transaction Management - master and sample data
   Run after 01-schema.sql. Idempotent: it only inserts into empty tables.
   Produces enough volume to demonstrate pagination, searching, sorting and RDLC reporting.
   ============================================================================================= */

USE [TransactionManagementDb];
GO

SET NOCOUNT ON;
GO

/* ---------------------------------------------------------------------------------------------
   Business partners: 4 customers and 4 suppliers.
   --------------------------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.BusinessPartners)
BEGIN
    INSERT dbo.BusinessPartners (Code, Name, PartnerType, Email, PhoneNumber, IsActive, CreatedAtUtc)
    VALUES
        (N'CUS-001', N'Meridian Retail Group',      1, N'orders@meridianretail.example',    N'+880 2 55011001', 1, SYSUTCDATETIME()),
        (N'CUS-002', N'Northbridge Trading Co.',    1, N'purchasing@northbridge.example',   N'+880 2 55011002', 1, SYSUTCDATETIME()),
        (N'CUS-003', N'Delta Hospitality Ltd.',     1, N'accounts@deltahospitality.example',N'+880 2 55011003', 1, SYSUTCDATETIME()),
        (N'CUS-004', N'Calder & Vane Stores',       1, N'supply@caldervane.example',        N'+880 2 55011004', 1, SYSUTCDATETIME()),
        (N'SUP-001', N'Orion Industrial Supplies',  2, N'sales@orionsupplies.example',      N'+880 2 55022001', 1, SYSUTCDATETIME()),
        (N'SUP-002', N'Bluepeak Manufacturing',     2, N'contact@bluepeakmfg.example',      N'+880 2 55022002', 1, SYSUTCDATETIME()),
        (N'SUP-003', N'Harborline Logistics',       2, N'ops@harborline.example',           N'+880 2 55022003', 1, SYSUTCDATETIME()),
        (N'SUP-004', N'Vertex Components BD',       2, N'info@vertexcomponents.example',    N'+880 2 55022004', 1, SYSUTCDATETIME());
END
GO

/* ---------------------------------------------------------------------------------------------
   Products.
   --------------------------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    INSERT dbo.Products (Code, Name, UnitOfMeasure, DefaultUnitPrice, IsActive, CreatedAtUtc)
    VALUES
        (N'PRD-001', N'A4 Copier Paper 80gsm',        N'Ream',  4.75,   1, SYSUTCDATETIME()),
        (N'PRD-002', N'Ballpoint Pen (Box of 50)',    N'Box',   12.50,  1, SYSUTCDATETIME()),
        (N'PRD-003', N'Laser Toner Cartridge',        N'Piece', 89.00,  1, SYSUTCDATETIME()),
        (N'PRD-004', N'Steel Filing Cabinet',         N'Unit',  245.00, 1, SYSUTCDATETIME()),
        (N'PRD-005', N'Ergonomic Office Chair',       N'Unit',  189.90, 1, SYSUTCDATETIME()),
        (N'PRD-006', N'LED Desk Lamp',                N'Piece', 32.40,  1, SYSUTCDATETIME()),
        (N'PRD-007', N'Corrugated Carton (Large)',    N'Piece', 1.85,   1, SYSUTCDATETIME()),
        (N'PRD-008', N'Packing Tape 48mm',            N'Roll',  2.30,   1, SYSUTCDATETIME()),
        (N'PRD-009', N'Industrial Safety Gloves',     N'Pair',  6.95,   1, SYSUTCDATETIME()),
        (N'PRD-010', N'Stainless Steel Bolt M10',     N'Kg',    3.60,   1, SYSUTCDATETIME()),
        (N'PRD-011', N'Thermal Receipt Roll',         N'Roll',  1.20,   1, SYSUTCDATETIME()),
        (N'PRD-012', N'Warehouse Pallet (Wooden)',    N'Unit',  18.75,  1, SYSUTCDATETIME());
END
GO

/* ---------------------------------------------------------------------------------------------
   45 transactions, each with 1 to 4 detail lines.

   Generated set-based from a tally derived from sys.all_objects: no cursor, no loop and no
   stored procedure. Transaction numbers stay unique because the sequence is global while the
   month prefix comes from the generated date.
   --------------------------------------------------------------------------------------------- */
IF NOT EXISTS (SELECT 1 FROM dbo.Transactions)
BEGIN
    DECLARE @TransactionCount INT = 45;

    ;WITH Tally AS
    (
        SELECT TOP (@TransactionCount)
               ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS Sequence
        FROM sys.all_objects
    ),
    Generated AS
    (
        SELECT
            t.Sequence,
            CAST(DATEADD(DAY, -((t.Sequence * 7) % 120), CAST(SYSUTCDATETIME() AS DATE)) AS DATE) AS TransactionDate,
            ((t.Sequence - 1) % 8) + 1 AS BusinessPartnerId
        FROM Tally AS t
    )
    INSERT dbo.Transactions
        (TransactionNumber, TransactionDate, BusinessPartnerId, Reference, Remarks,
         TotalQuantity, TotalAmount, CreatedAtUtc)
    SELECT
        N'TRX-' + FORMAT(g.TransactionDate, 'yyyyMM') + N'-'
            + RIGHT(N'00000' + CAST(g.Sequence AS NVARCHAR(5)), 5),
        g.TransactionDate,
        g.BusinessPartnerId,
        N'PO-' + CAST(10000 + (g.Sequence * 37) AS NVARCHAR(10)),
        CASE WHEN g.Sequence % 5 = 0 THEN N'Seeded sample transaction.' ELSE NULL END,
        0,
        0,
        SYSUTCDATETIME()
    FROM Generated AS g;

    /* Detail lines: 1 to 4 per transaction, product rotated through the catalogue. */
    ;WITH LineTally AS
    (
        SELECT 1 AS LineNumber UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4
    )
    INSERT dbo.TransactionDetails
        (TransactionId, ProductId, DetailDate, Description, Quantity, Amount, IsActive, CreatedAtUtc)
    SELECT
        t.Id,
        (((t.Id + l.LineNumber) % 12) + 1)                                        AS ProductId,
        DATEADD(DAY, -(l.LineNumber - 1), t.TransactionDate)                      AS DetailDate,
        CASE l.LineNumber
            WHEN 1 THEN N'Primary line'
            ELSE N'Additional line ' + CAST(l.LineNumber AS NVARCHAR(2))
        END                                                                       AS Description,
        CAST(((t.Id + l.LineNumber) % 20) + 1 AS DECIMAL(18, 3))                  AS Quantity,
        CAST(p.DefaultUnitPrice * (((t.Id + l.LineNumber) % 20) + 1) AS DECIMAL(18, 2)) AS Amount,
        /* One line in roughly every ten transactions is inactive, to exercise the Active flag. */
        CASE WHEN l.LineNumber > 1 AND (t.Id % 10) = 0 THEN 0 ELSE 1 END          AS IsActive,
        SYSUTCDATETIME()
    FROM dbo.Transactions AS t
    CROSS JOIN LineTally AS l
    INNER JOIN dbo.Products AS p
        ON p.Id = (((t.Id + l.LineNumber) % 12) + 1)
    WHERE l.LineNumber <= ((t.Id % 4) + 1);

    /* Totals mirror the domain rule: active lines only. */
    UPDATE t
    SET t.TotalQuantity = ISNULL(d.TotalQuantity, 0),
        t.TotalAmount   = ISNULL(d.TotalAmount, 0)
    FROM dbo.Transactions AS t
    OUTER APPLY
    (
        SELECT SUM(td.Quantity) AS TotalQuantity,
               SUM(td.Amount)   AS TotalAmount
        FROM dbo.TransactionDetails AS td
        WHERE td.TransactionId = t.Id
          AND td.IsActive = 1
    ) AS d;
END
GO

SELECT
    (SELECT COUNT(*) FROM dbo.BusinessPartners)   AS BusinessPartners,
    (SELECT COUNT(*) FROM dbo.Products)           AS Products,
    (SELECT COUNT(*) FROM dbo.Transactions)       AS Transactions,
    (SELECT COUNT(*) FROM dbo.TransactionDetails) AS TransactionDetails;
GO
