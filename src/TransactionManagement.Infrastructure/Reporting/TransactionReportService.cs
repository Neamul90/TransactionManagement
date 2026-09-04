using Microsoft.EntityFrameworkCore;
using TransactionManagement.Application.Abstractions.Reporting;
using TransactionManagement.Application.Dtos.Transactions;
using TransactionManagement.Infrastructure.Persistence;

namespace TransactionManagement.Infrastructure.Reporting;

/// <summary>
/// Builds the flattened, print-ready data set for the RDLC report in a single read-only round trip.
/// It knows nothing about RDLC itself — rendering is a presentation concern and lives in the Web layer.
/// </summary>
public sealed class TransactionReportService : ITransactionReportService
{
    private readonly ApplicationDbContext _context;

    public TransactionReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TransactionReportDto?> GetTransactionReportAsync(
        int transactionId,
        CancellationToken cancellationToken = default)
    {
        var report = await _context.Transactions
            .AsNoTracking()
            .Where(transaction => transaction.Id == transactionId)
            .Select(transaction => new TransactionReportDto
            {
                TransactionId = transaction.Id,
                TransactionNumber = transaction.TransactionNumber,
                TransactionDate = transaction.TransactionDate,
                BusinessPartnerCode = transaction.BusinessPartner!.Code,
                BusinessPartnerName = transaction.BusinessPartner.Name,
                BusinessPartnerType = transaction.BusinessPartner.PartnerType.ToString(),
                Reference = transaction.Reference ?? string.Empty,
                Remarks = transaction.Remarks ?? string.Empty,
                TotalQuantity = transaction.TotalQuantity,
                TotalAmount = transaction.TotalAmount,
                Lines = transaction.Details
                    .OrderBy(detail => detail.Id)
                    .Select(detail => new TransactionReportLineDto
                    {
                        ProductCode = detail.Product!.Code,
                        ProductName = detail.Product.Name,
                        UnitOfMeasure = detail.Product.UnitOfMeasure,
                        DetailDate = detail.DetailDate,
                        Description = detail.Description ?? string.Empty,
                        Quantity = detail.Quantity,
                        Amount = detail.Amount,
                        IsActive = detail.IsActive
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (report is null)
        {
            return null;
        }

        // Line numbering is presentation data and is applied after materialisation so that
        // the database is never asked to produce a row number it does not need.
        var numberedLines = report.Lines
            .Select((line, index) => new TransactionReportLineDto
            {
                LineNumber = index + 1,
                ProductCode = line.ProductCode,
                ProductName = line.ProductName,
                UnitOfMeasure = line.UnitOfMeasure,
                DetailDate = line.DetailDate,
                Description = line.Description,
                Quantity = line.Quantity,
                Amount = line.Amount,
                IsActive = line.IsActive
            })
            .ToList();

        return new TransactionReportDto
        {
            TransactionId = report.TransactionId,
            TransactionNumber = report.TransactionNumber,
            TransactionDate = report.TransactionDate,
            BusinessPartnerCode = report.BusinessPartnerCode,
            BusinessPartnerName = report.BusinessPartnerName,
            BusinessPartnerType = report.BusinessPartnerType,
            Reference = report.Reference,
            Remarks = report.Remarks,
            TotalQuantity = report.TotalQuantity,
            TotalAmount = report.TotalAmount,
            Lines = numberedLines
        };
    }
}
