using Microsoft.AspNetCore.Diagnostics;
using TransactionManagement.Application.Common.Exceptions;
using TransactionManagement.Domain.Exceptions;

namespace TransactionManagement.Web.Infrastructure;

/// <summary>
/// Single place where an unhandled exception is turned into a status code, a log entry and a
/// user-safe message. No controller action contains a try/catch for infrastructure failures.
/// <para>
/// It classifies the exception and stores the safe message on <see cref="HttpContext.Items"/>,
/// then returns <c>false</c> so the pipeline re-executes the error endpoint, which renders it.
/// </para>
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public const string ErrorMessageItemKey = "__SafeErrorMessage";
    public const string ErrorTitleItemKey = "__SafeErrorTitle";

    private const string GenericTitle = "Something went wrong";

    private const string GenericMessage =
        "An unexpected error occurred while processing your request. "
        + "The problem has been logged. Please try again, and contact support if it continues.";

    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, message) = Classify(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning(
                "Request {Method} {Path} failed with {StatusCode}: {Reason}",
                httpContext.Request.Method,
                httpContext.Request.Path,
                statusCode,
                exception.GetType().Name);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Items[ErrorTitleItemKey] = title;
        httpContext.Items[ErrorMessageItemKey] = message;

        // Returning false lets UseExceptionHandler re-execute the error page endpoint,
        // which renders the message above inside the normal application layout.
        return ValueTask.FromResult(false);
    }

    private static (int StatusCode, string Title, string Message) Classify(Exception exception) =>
        exception switch
        {
            EntityNotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Not found",
                notFound.Message),

            ConcurrencyConflictException conflict => (
                StatusCodes.Status409Conflict,
                "The record changed while you were editing",
                conflict.Message),

            BusinessRuleViolationException businessRule => (
                StatusCodes.Status400BadRequest,
                "That change is not allowed",
                businessRule.Message),

            ValidationException => (
                StatusCodes.Status400BadRequest,
                "Some details are not valid",
                "Please review the highlighted fields and try again."),

            OperationCanceledException => (
                ApplicationStatusCodes.ClientClosedRequest,
                "Request cancelled",
                "The request was cancelled before it completed."),

            // Everything else is unexpected: log it fully, tell the user nothing technical.
            _ => (StatusCodes.Status500InternalServerError, GenericTitle, GenericMessage)
        };
}
