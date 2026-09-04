namespace TransactionManagement.Web.Models.Shared;

/// <summary>
/// Everything the error page is allowed to show. <see cref="Message"/> has already been classified
/// as safe for end users — never an exception message, stack trace or SQL detail.
/// <para>
/// <see cref="DeveloperDetail"/> is populated only in the Development environment. It is the one
/// place technical detail is allowed to reach the browser, and it is off everywhere else.
/// </para>
/// </summary>
public sealed class ErrorViewModel
{
    public string Title { get; init; } = "Something went wrong";

    public string Message { get; init; } =
        "An unexpected error occurred while processing your request. Please try again.";

    public int StatusCode { get; init; } = 500;

    public string? RequestId { get; init; }

    public string? DeveloperDetail { get; init; }

    public bool ShowRequestId => !string.IsNullOrWhiteSpace(RequestId);

    public bool ShowDeveloperDetail => !string.IsNullOrWhiteSpace(DeveloperDetail);
}
