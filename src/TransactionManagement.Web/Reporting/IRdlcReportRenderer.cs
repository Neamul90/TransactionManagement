using TransactionManagement.Application.Dtos.Transactions;

namespace TransactionManagement.Web.Reporting;

/// <summary>
/// Renders a report DTO through RDLC. This abstraction lives in the Web layer on purpose:
/// rendering is a presentation concern, and RDLC is a Windows-only dependency that the
/// Application and Infrastructure layers must stay free of.
/// </summary>
public interface IRdlcReportRenderer
{
    RenderedReport RenderTransactionReport(TransactionReportDto report, ReportFormat format);
}
