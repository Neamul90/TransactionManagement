namespace TransactionManagement.Web.Infrastructure;

/// <summary>
/// Status codes ASP.NET Core does not define. 499 is the widely used code for a request the
/// client abandoned, which is what a propagated <see cref="OperationCanceledException"/> means.
/// </summary>
internal static class ApplicationStatusCodes
{
    internal const int ClientClosedRequest = 499;
}
