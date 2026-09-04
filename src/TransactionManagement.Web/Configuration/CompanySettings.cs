namespace TransactionManagement.Web.Configuration;

/// <summary>
/// The letterhead shown on the printable transaction document and used as the report header.
/// Bound from the <c>CompanySettings</c> configuration section so the deployment owns its own
/// identity: nothing here is compiled into the application.
/// </summary>
public sealed class CompanySettings
{
    public const string SectionName = "CompanySettings";

    public string Name { get; init; } = "Transaction Management";

    public string? Tagline { get; init; }

    public string? AddressLine1 { get; init; }

    public string? AddressLine2 { get; init; }

    public string? Phone { get; init; }

    public string? Email { get; init; }

    public string? Website { get; init; }

    /// <summary>Application-relative path to the letterhead logo, for example <c>/img/logo.svg</c>.</summary>
    public string? LogoPath { get; init; }

    /// <summary>Symbol placed before monetary amounts on the document. Empty renders plain numbers.</summary>
    public string CurrencySymbol { get; init; } = string.Empty;

    /// <summary>Currency name used by the "amount in words" line, for example "Taka".</summary>
    public string CurrencyName { get; init; } = string.Empty;

    public bool HasLogo => !string.IsNullOrWhiteSpace(LogoPath);

    public IEnumerable<string> AddressLines =>
        new[] { AddressLine1, AddressLine2 }
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line!);

    public IEnumerable<string> ContactLines
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Phone))
            {
                yield return $"Tel: {Phone}";
            }

            if (!string.IsNullOrWhiteSpace(Email))
            {
                yield return Email!;
            }

            if (!string.IsNullOrWhiteSpace(Website))
            {
                yield return Website!;
            }
        }
    }
}
