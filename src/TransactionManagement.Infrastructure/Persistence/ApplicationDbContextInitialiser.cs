using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TransactionManagement.Domain.Entities;
using TransactionManagement.Domain.Enums;

namespace TransactionManagement.Infrastructure.Persistence;

/// <summary>
/// Applies pending migrations and seeds master and sample data.
/// Seeding is idempotent: it only runs against an empty table, so restarting the application
/// never duplicates data.
/// </summary>
public sealed class ApplicationDbContextInitialiser
{
    private const int SampleTransactionCount = 45;
    private const int RandomSeed = 20260904;

    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApplicationDbContextInitialiser> _logger;

    public ApplicationDbContextInitialiser(
        ApplicationDbContext context,
        ILogger<ApplicationDbContextInitialiser> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.MigrateAsync(cancellationToken);

        _logger.LogInformation("Database schema is up to date");
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedBusinessPartnersAsync(cancellationToken);
        await SeedProductsAsync(cancellationToken);
        await SeedTransactionsAsync(cancellationToken);
    }

    private async Task SeedBusinessPartnersAsync(CancellationToken cancellationToken)
    {
        if (await _context.BusinessPartners.AnyAsync(cancellationToken))
        {
            return;
        }

        var partners = new List<BusinessPartner>
        {
            BusinessPartner.Create("CUS-001", "Meridian Retail Group", PartnerType.Customer, "orders@meridianretail.example", "+880 2 55011001"),
            BusinessPartner.Create("CUS-002", "Northbridge Trading Co.", PartnerType.Customer, "purchasing@northbridge.example", "+880 2 55011002"),
            BusinessPartner.Create("CUS-003", "Delta Hospitality Ltd.", PartnerType.Customer, "accounts@deltahospitality.example", "+880 2 55011003"),
            BusinessPartner.Create("CUS-004", "Calder & Vane Stores", PartnerType.Customer, "supply@caldervane.example", "+880 2 55011004"),
            BusinessPartner.Create("SUP-001", "Orion Industrial Supplies", PartnerType.Supplier, "sales@orionsupplies.example", "+880 2 55022001"),
            BusinessPartner.Create("SUP-002", "Bluepeak Manufacturing", PartnerType.Supplier, "contact@bluepeakmfg.example", "+880 2 55022002"),
            BusinessPartner.Create("SUP-003", "Harborline Logistics", PartnerType.Supplier, "ops@harborline.example", "+880 2 55022003"),
            BusinessPartner.Create("SUP-004", "Vertex Components BD", PartnerType.Supplier, "info@vertexcomponents.example", "+880 2 55022004")
        };

        await _context.BusinessPartners.AddRangeAsync(partners, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} business partners", partners.Count);
    }

    private async Task SeedProductsAsync(CancellationToken cancellationToken)
    {
        if (await _context.Products.AnyAsync(cancellationToken))
        {
            return;
        }

        var products = new List<Product>
        {
            Product.Create("PRD-001", "A4 Copier Paper 80gsm", "Ream", 4.75m),
            Product.Create("PRD-002", "Ballpoint Pen (Box of 50)", "Box", 12.50m),
            Product.Create("PRD-003", "Laser Toner Cartridge", "Piece", 89.00m),
            Product.Create("PRD-004", "Steel Filing Cabinet", "Unit", 245.00m),
            Product.Create("PRD-005", "Ergonomic Office Chair", "Unit", 189.90m),
            Product.Create("PRD-006", "LED Desk Lamp", "Piece", 32.40m),
            Product.Create("PRD-007", "Corrugated Carton (Large)", "Piece", 1.85m),
            Product.Create("PRD-008", "Packing Tape 48mm", "Roll", 2.30m),
            Product.Create("PRD-009", "Industrial Safety Gloves", "Pair", 6.95m),
            Product.Create("PRD-010", "Stainless Steel Bolt M10", "Kg", 3.60m),
            Product.Create("PRD-011", "Thermal Receipt Roll", "Roll", 1.20m),
            Product.Create("PRD-012", "Warehouse Pallet (Wooden)", "Unit", 18.75m)
        };

        await _context.Products.AddRangeAsync(products, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} products", products.Count);
    }

    private async Task SeedTransactionsAsync(CancellationToken cancellationToken)
    {
        if (await _context.Transactions.AnyAsync(cancellationToken))
        {
            return;
        }

        var partnerIds = await _context.BusinessPartners
            .Where(partner => partner.IsActive)
            .Select(partner => partner.Id)
            .ToListAsync(cancellationToken);

        var products = await _context.Products
            .Where(product => product.IsActive)
            .Select(product => new { product.Id, product.DefaultUnitPrice })
            .ToListAsync(cancellationToken);

        if (partnerIds.Count == 0 || products.Count == 0)
        {
            return;
        }

        var random = new Random(RandomSeed);
        var today = DateTime.UtcNow.Date;
        var sequenceByMonth = new Dictionary<string, int>(StringComparer.Ordinal);
        var transactions = new List<Transaction>(SampleTransactionCount);

        for (var index = 0; index < SampleTransactionCount; index++)
        {
            var transactionDate = today.AddDays(-random.Next(0, 120));
            var monthToken = transactionDate.ToString("yyyyMM", CultureInfo.InvariantCulture);

            sequenceByMonth.TryGetValue(monthToken, out var sequence);
            sequence++;
            sequenceByMonth[monthToken] = sequence;

            var transactionNumber =
                $"TRX-{monthToken}-{sequence.ToString(CultureInfo.InvariantCulture).PadLeft(5, '0')}";

            var transaction = Transaction.Create(
                transactionNumber,
                transactionDate,
                partnerIds[random.Next(partnerIds.Count)],
                $"PO-{random.Next(10000, 99999)}",
                index % 5 == 0 ? "Seeded sample transaction." : null,
                today);

            var lineCount = random.Next(1, 6);

            for (var line = 0; line < lineCount; line++)
            {
                var product = products[random.Next(products.Count)];
                var quantity = random.Next(1, 25);
                var amount = decimal.Round(product.DefaultUnitPrice * quantity, 2);

                transaction.AddDetail(
                    product.Id,
                    transactionDate.AddDays(-random.Next(0, 5)),
                    line == 0 ? "Primary line" : $"Additional line {line + 1}",
                    quantity,
                    amount,
                    isActive: !(line > 0 && random.Next(0, 10) == 0));
            }

            transaction.EnsureAtLeastOneActiveDetail();
            transactions.Add(transaction);
        }

        await _context.Transactions.AddRangeAsync(transactions, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded {Count} sample transactions", transactions.Count);
    }
}
