using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TransactionManagement.Web.Infrastructure;
using TransactionManagement.Web.Models.Shared;

namespace TransactionManagement.Web.Controllers;

public sealed class HomeController : Controller
{
    private readonly IWebHostEnvironment _environment;

    public HomeController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet]
    public IActionResult Index() => RedirectToAction("Index", "Transactions");

    /// <summary>
    /// Re-executed by the exception middleware. It renders the message the global handler already
    /// classified as safe, and — in Development only — the underlying exception, so a failure is
    /// diagnosable without reading the console.
    /// </summary>
    [IgnoreAntiforgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Error()
    {
        var title = HttpContext.Items[GlobalExceptionHandler.ErrorTitleItemKey] as string;
        var message = HttpContext.Items[GlobalExceptionHandler.ErrorMessageItemKey] as string;

        return View(new ErrorViewModel
        {
            Title = title ?? "Something went wrong",
            Message = message
                ?? "An unexpected error occurred while processing your request. Please try again.",
            StatusCode = Response.StatusCode,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            DeveloperDetail = BuildDeveloperDetail()
        });
    }

    [IgnoreAntiforgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult HttpStatus(int code) =>
        View("Error", new ErrorViewModel
        {
            Title = code switch
            {
                StatusCodes.Status404NotFound => "Page not found",
                StatusCodes.Status403Forbidden => "Access denied",
                _ => "Request could not be completed"
            },
            Message = code switch
            {
                StatusCodes.Status404NotFound =>
                    "The page or record you asked for does not exist, or has been removed.",
                StatusCodes.Status403Forbidden =>
                    "You do not have permission to view this page.",
                _ => "The request could not be completed. Please try again."
            },
            StatusCode = code,
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });

    private string? BuildDeveloperDetail()
    {
        if (!_environment.IsDevelopment())
        {
            return null;
        }

        var exception = HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;

        if (exception is null)
        {
            return null;
        }

        // Innermost first: with report rendering the outer exception is usually a generic wrapper
        // and the actual cause is two or three levels down.
        var lines = new List<string>();

        for (var current = exception; current is not null; current = current.InnerException)
        {
            lines.Add($"{current.GetType().FullName}: {current.Message}");
        }

        lines.Add(string.Empty);
        lines.Add(exception.StackTrace ?? string.Empty);

        return string.Join(Environment.NewLine, lines);
    }
}
