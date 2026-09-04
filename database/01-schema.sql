/* =============================================================================================
   Transaction Management - database schema
   Target      : Microsoft SQL Server 2019 or later (also runs on LocalDB)
   Contains    : database, tables, primary keys, foreign keys, unique constraints,
                 check constraints and indexes.
   Deliberately contains NO stored procedures - all data access goes through EF Core.

   This script is an alternative to `dotnet ef database update`; use one or the other,
   not both, against the same database.
   ============================================================================================= */

IF DB_ID(N'TransactionManagementDb') IS NULL
BEGIN
    CREATE DATABASE [TransactionManagementDb];
END
GO

USE [TransactionManagementDb];
GO

SET NOCOUNT ON;
GO

/* ---------------------------------------------------------------------------------------------
   Drop in dependency order so the script can be re-run during development.
   --------------------------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.TransactionDetails', N'U') IS NOT NULL DROP TABLE dbo.TransactionDetails;
IF OBJECT_ID(N'dbo.Transactions', N'U') IS NOT NULL DROP TABLE dbo.Transactions;
IF OBJECT_ID(N'dbo.Products', N'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID(N'dbo.BusinessPartners', N'U') IS NOT NULL DROP TABLE dbo.BusinessPartners;
GO

/* ---------------------------------------------------------------------------------------------
   BusinessPartners
   A customer or a supplier. PartnerType: 1 = Customer, 2 = Supplier.
   --------------------------------------------------------------------------------------------- */
CREATE TABLE dbo.BusinessPartners
(
    Id            INT             IDENTITY(1, 1) NOT NULL,
    Code          NVARCHAR(20)    NOT NULL,
    Name          NVARCHAR(150)   NOT NULL,
    PartnerType   TINYINT         NOT NULL,
    Email         NVARCHAR(150)   NULL,
    PhoneNumber   NVARCHAR(30)    NULL,
    IsActive      BIT             NOT NULL CONSTRAINT DF_BusinessPartners_IsActive DEFAULT (1),
    CreatedAtUtc  DATETIME2(7)    NOT NULL,
    ModifiedAtUtc DATETIME2(7)    NULL,

    CONSTRAINT PK_BusinessPartners PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT CK_BusinessPartners_PartnerType CHECK (PartnerType IN (1, 2))
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_BusinessPartners_Code ON dbo.BusinessPartners (Code);
CREATE NONCLUSTERED INDEX IX_BusinessPartners_Name ON dbo.BusinessPartners (Name);
GO

/* ---------------------------------------------------------------------------------------------
   Products
   --------------------------------------------------------------------------------------------- */
CREATE TABLE dbo.Products
(
    Id               INT            IDENTITY(1, 1) NOT NULL,
    Code             NVARCHAR(30)   NOT NULL,
    Name             NVARCHAR(150)  NOT NULL,
    UnitOfMeasure    NVARCHAR(20)   NOT NULL,
    DefaultUnitPrice DECIMAL(18, 2) NOT NULL,
    IsActive         BIT            NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
    CreatedAtUtc     DATETIME2(7)   NOT NULL,
    ModifiedAtUtc    DATETIME2(7)   NULL,

    CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT CK_Products_DefaultUnitPrice CHECK (DefaultUnitPrice >= 0)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_Products_Code ON dbo.Products (Code);
CREATE NONCLUSTERED INDEX IX_Products_Name ON dbo.Products (Name);
GO

/* ---------------------------------------------------------------------------------------------
   Transactions (master)
   RowVersion provides optimistic concurrency for the whole aggregate.
   TotalQuantity / TotalAmount are maintained by the domain model over the ACTIVE detail lines.
   --------------------------------------------------------------------------------------------- */
CREATE TABLE dbo.Transactions
(
    Id                INT            IDENTITY(1, 1) NOT NULL,
    TransactionNumber NVARCHAR(30)   NOT NULL,
    TransactionDate   DATE           NOT NULL,
    BusinessPartnerId INT            NOT NULL,
    Reference         NVARCHAR(50)   NULL,
    Remarks           NVARCHAR(500)  NULL,
    TotalQuantity     DECIMAL(18, 3) NOT NULL CONSTRAINT DF_Transactions_TotalQuantity DEFAULT (0),
    TotalAmount       DECIMAL(18, 2) NOT NULL CONSTRAINT DF_Transactions_TotalAmount DEFAULT (0),
    RowVersion        ROWVERSION     NOT NULL,
    CreatedAtUtc      DATETIME2(7)   NOT NULL,
    ModifiedAtUtc     DATETIME2(7)   NULL,

    CONSTRAINT PK_Transactions PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Transactions_BusinessPartners_BusinessPartnerId
        FOREIGN KEY (BusinessPartnerId) REFERENCES dbo.BusinessPartners (Id)
        ON DELETE NO ACTION,
    CONSTRAINT CK_Transactions_TotalAmount CHECK (TotalAmount >= 0),
    CONSTRAINT CK_Transactions_TotalQuantity CHECK (TotalQuantity >= 0)
);
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_Transactions_TransactionNumber
    ON dbo.Transactions (TransactionNumber);

CREATE NONCLUSTERED INDEX IX_Transactions_TransactionDate
    ON dbo.Transactions (TransactionDate)
    INCLUDE (TransactionNumber, BusinessPartnerId, TotalAmount);

CREATE NONCLUSTERED INDEX IX_Transactions_BusinessPartnerId
    ON dbo.Transactions (BusinessPartnerId);
GO

/* ---------------------------------------------------------------------------------------------
   TransactionDetails (detail)
   Cascade delete from the master: a detail line has no life of its own outside its transaction.
   The foreign key to Products is NO ACTION so a product in use cannot be deleted by accident.
   --------------------------------------------------------------------------------------------- */
CREATE TABLE dbo.TransactionDetails
(
    Id            INT            IDENTITY(1, 1) NOT NULL,
    TransactionId INT            NOT NULL,
    ProductId     INT            NOT NULL,
    DetailDate    DATE           NOT NULL,
    Description   NVARCHAR(250)  NULL,
    Quantity      DECIMAL(18, 3) NOT NULL,
    Amount        DECIMAL(18, 2) NOT NULL,
    IsActive      BIT            NOT NULL CONSTRAINT DF_TransactionDetails_IsActive DEFAULT (1),
    CreatedAtUtc  DATETIME2(7)   NOT NULL,
    ModifiedAtUtc DATETIME2(7)   NULL,

    CONSTRAINT PK_TransactionDetails PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_TransactionDetails_Transactions_TransactionId
        FOREIGN KEY (TransactionId) REFERENCES dbo.Transactions (Id)
        ON DELETE CASCADE,
    CONSTRAINT FK_TransactionDetails_Products_ProductId
        FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id)
        ON DELETE NO ACTION,
    CONSTRAINT CK_TransactionDetails_Quantity CHECK (Quantity > 0),
    CONSTRAINT CK_TransactionDetails_Amount CHECK (Amount >= 0)
);
GO

CREATE NONCLUSTERED INDEX IX_TransactionDetails_TransactionId
    ON dbo.TransactionDetails (TransactionId)
    INCLUDE (ProductId, Quantity, Amount, IsActive);

CREATE NONCLUSTERED INDEX IX_TransactionDetails_ProductId
    ON dbo.TransactionDetails (ProductId);
GO

PRINT 'Schema created.';
GO
