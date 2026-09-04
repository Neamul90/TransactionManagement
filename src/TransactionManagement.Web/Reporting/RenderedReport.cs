namespace TransactionManagement.Web.Reporting;

/// <summary>A rendered report ready to be streamed to the browser.</summary>
public sealed record RenderedReport(byte[] Content, string ContentType, string FileName);
