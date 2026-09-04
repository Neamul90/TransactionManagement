namespace TransactionManagement.Web.Reporting;

/// <summary>
/// Bound from the <c>ReportSettings</c> configuration section. The letterhead itself lives in
/// <see cref="Configuration.CompanySettings"/> so the printed document and the RDLC report cannot
/// disagree about who published them.
/// </summary>
public sealed class ReportSettings
{
    public const string SectionName = "ReportSettings";

    public string ReportFolder { get; init; } = "Reports";
}
