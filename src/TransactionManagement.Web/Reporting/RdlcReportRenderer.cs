using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.Reporting.NETCore;
using TransactionManagement.Application.Common.Formatting;
using TransactionManagement.Application.Dtos.Transactions;
using TransactionManagement.Web.Configuration;

namespace TransactionManagement.Web.Reporting;

/// <summary>
/// Adapter over the local RDLC report engine. It is the only type in the solution that knows what
/// an RDLC is; everything upstream deals in <see cref="TransactionReportDto"/>.
/// </summary>
public sealed class RdlcReportRenderer : IRdlcReportRenderer
{
    private const string ReportFileName = "TransactionReport.rdlc";

    /// <summary>Must match the <c>DataSet Name</c> in the RDLC definition.</summary>
    private const string DataSetName = "TransactionDetailsDataSet";

    private readonly string _reportFilePath;
    private readonly CompanySettings _companySettings;
    private readonly ILogger<RdlcReportRenderer> _logger;

    public RdlcReportRenderer(
        IWebHostEnvironment environment,
        IOptions<ReportSettings> reportSettings,
        IOptions<CompanySettings> companySettings,
        ILogger<RdlcReportRenderer> logger)
    {
        _companySettings = companySettings.Value;
        _logger = logger;

        _reportFilePath = Path.Combine(
            environment.ContentRootPath,
            reportSettings.Value.ReportFolder,
            ReportFileName);
    }

    public RenderedReport RenderTransactionReport(TransactionReportDto report, ReportFormat format)
    {
        if (!File.Exists(_reportFilePath))
        {
            throw new FileNotFoundException(
                "The transaction report definition is missing from the deployment.",
                ReportFileName);
        }

        var (renderFormat, contentType, extension) = MapFormat(format);

        using var definitionStream = File.OpenRead(_reportFilePath);
        using var localReport = new LocalReport();

        localReport.LoadReportDefinition(definitionStream);
        localReport.DataSources.Add(new ReportDataSource(DataSetName, report.Lines.ToList()));
        localReport.SetParameters(BuildParameters(report));

        var content = localReport.Render(renderFormat);

        _logger.LogInformation(
            "Rendered {Format} report for transaction {TransactionNumber} ({ByteCount} bytes)",
            format,
            report.TransactionNumber,
            content.Length);

        return new RenderedReport(content, contentType, $"{report.TransactionNumber}.{extension}");
    }

    /// <summary>
    /// Every parameter the definition declares must be supplied or <c>SetParameters</c> throws, so
    /// the report is fed one flat set of pre-formatted strings. Formatting happens here rather than
    /// in RDLC expressions: the same values then appear on the printed document and on the screen.
    /// </summary>
    private ReportParameter[] BuildParameters(TransactionReportDto report)
    {
        var isCustomer = string.Equals(
            report.BusinessPartnerType,
            "Customer",
            StringComparison.OrdinalIgnoreCase);

        var activeLines = report.Lines.Count(line => line.IsActive);

        var addressBlock = string.Join(
            Environment.NewLine,
            _companySettings.AddressLines.Concat(_companySettings.ContactLines));

        var amountInWords = AmountInWords.Convert(
            report.TotalAmount,
            _companySettings.CurrencyName,
            _companySettings.CurrencyFractionName) + " only";

        // Wording matched to the details screen. The company name is not followed by a full stop of
        // its own, because names such as "… Ltd." already end in one.
        var printedOn = DateTime.Now.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);
        var legalNotice =
            $"This is a computer-generated document. {_companySettings.Name} \u2014 printed on {printedOn}.";

        return
        [
            new ReportParameter("CompanyName", _companySettings.Name),
            new ReportParameter("CompanyTagline", _companySettings.Tagline ?? string.Empty),
            new ReportParameter("CompanyAddressBlock", addressBlock),

            // Tracked-out to match the letter-spaced title on the details screen.
            new ReportParameter("DocumentType", "T R A N S A C T I O N"),

            new ReportParameter("TransactionNumber", report.TransactionNumber),
            new ReportParameter(
                "TransactionDate",
                report.TransactionDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)),
            new ReportParameter("Reference", Placeholder(report.Reference)),

            new ReportParameter("PartnerLabel", isCustomer ? "BILL TO" : "SUPPLIER"),
            new ReportParameter("PartnerName", report.BusinessPartnerName),
            new ReportParameter("PartnerType", report.BusinessPartnerType),

            new ReportParameter("ActiveLineCount", activeLines.ToString(CultureInfo.InvariantCulture)),

            new ReportParameter("TotalQuantity", report.TotalQuantity.ToString("N3", CultureInfo.InvariantCulture)),
            new ReportParameter(
                "TotalAmount",
                _companySettings.CurrencySymbol + report.TotalAmount.ToString("N2", CultureInfo.InvariantCulture)),
            new ReportParameter("AmountInWords", amountInWords),
            new ReportParameter("Remarks", Placeholder(report.Remarks)),
            new ReportParameter("CurrencySymbol", _companySettings.CurrencySymbol),

            new ReportParameter("LegalNotice", legalNotice)
        ];
    }

    private static string Placeholder(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "\u2014" : value;

    private static (string RenderFormat, string ContentType, string Extension) MapFormat(ReportFormat format) =>
        format switch
        {
            ReportFormat.Excel => (
                "EXCELOPENXML",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "xlsx"),

            ReportFormat.Word => (
                "WORDOPENXML",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "docx"),

            _ => ("PDF", "application/pdf", "pdf")
        };
}
