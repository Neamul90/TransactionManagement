using MediatR;
using Microsoft.AspNetCore.Mvc;
using TransactionManagement.Application.Dtos.Lookups;
using TransactionManagement.Application.Lookups.Queries.GetBusinessPartnerLookup;
using TransactionManagement.Application.Lookups.Queries.GetProductLookup;
using TransactionManagement.Application.Transactions.Commands.CreateTransaction;
using TransactionManagement.Application.Transactions.Commands.DeleteTransaction;
using TransactionManagement.Application.Transactions.Commands.UpdateTransaction;
using TransactionManagement.Application.Transactions.Queries.GetTransactionById;
using TransactionManagement.Application.Transactions.Queries.GetTransactionReport;
using TransactionManagement.Web.Extensions;
using TransactionManagement.Web.Mapping;
using TransactionManagement.Web.Models.Transactions;
using TransactionManagement.Web.Reporting;
using ValidationException = TransactionManagement.Application.Common.Exceptions.ValidationException;

namespace TransactionManagement.Web.Controllers;

/// <summary>
/// Presentation only: bind, dispatch, render. There is no business rule, no EF Core query and no
/// repository call anywhere in this file, and the controller never sees a domain entity.
/// <para>
/// The single <c>catch (ValidationException)</c> in each POST is a presentation concern — it turns
/// server-side validation failures into <c>ModelState</c> so the form can be redisplayed. Every
/// other exception is left to the global exception handler.
/// </para>
/// </summary>
public sealed class TransactionsController : Controller
{
    private readonly ISender _sender;

    public TransactionsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        TransactionListFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        var page = await _sender.Send(filter.ToQuery(), cancellationToken);

        return View(new TransactionListViewModel
        {
            Page = page,
            SearchTerm = filter.SearchTerm,
            SortBy = filter.SortBy,
            SortDescending = filter.SortDescending
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var transaction = await _sender.Send(new GetTransactionByIdQuery(id), cancellationToken);

        if (transaction is null)
        {
            return NotFound();
        }

        return View(new TransactionDetailsViewModel { Transaction = transaction });
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var (businessPartners, products) = await LoadLookupsAsync(cancellationToken);

        var viewModel = new TransactionCreateViewModel
        {
            TransactionDate = DateTime.Today,
            BusinessPartners = businessPartners,
            Products = products,
            Details = [new TransactionDetailRowViewModel { DetailDate = DateTime.Today }]
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        TransactionCreateViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var transactionId = await _sender.Send(viewModel.ToCommand(), cancellationToken);

                TempData["StatusMessage"] = "The transaction was created successfully.";

                return RedirectToAction(nameof(Details), new { id = transactionId });
            }
            catch (ValidationException exception)
            {
                ModelState.AddValidationErrors(exception);
            }
        }

        await RepopulateLookupsAsync(viewModel, cancellationToken);

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var transaction = await _sender.Send(new GetTransactionByIdQuery(id), cancellationToken);

        if (transaction is null)
        {
            return NotFound();
        }

        var (businessPartners, products) = await LoadLookupsAsync(cancellationToken);

        return View(transaction.ToEditViewModel(businessPartners, products));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        TransactionEditViewModel viewModel,
        CancellationToken cancellationToken)
    {
        // The route identifier is authoritative; a mismatched body is a tampering attempt.
        if (id != viewModel.Id)
        {
            return BadRequest();
        }

        if (ModelState.IsValid)
        {
            try
            {
                await _sender.Send(viewModel.ToCommand(), cancellationToken);

                TempData["StatusMessage"] = "The transaction was updated successfully.";

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (ValidationException exception)
            {
                ModelState.AddValidationErrors(exception);
            }
        }

        await RepopulateLookupsAsync(viewModel, cancellationToken);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int id,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new DeleteTransactionCommand(id, RowVersionToken.FromToken(rowVersion)),
            cancellationToken);

        TempData["StatusMessage"] = "The transaction was deleted.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Transaction List → Select Transaction → View/Print Report.
    /// Renders the RDLC to the requested format and streams it back; PDF opens inline in the
    /// browser's viewer, the other formats download.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Report(
        int id,
        ReportFormat format,
        [FromServices] IRdlcReportRenderer reportRenderer,
        CancellationToken cancellationToken)
    {
        var report = await _sender.Send(new GetTransactionReportQuery(id), cancellationToken);

        if (report is null)
        {
            return NotFound();
        }

        var renderedReport = reportRenderer.RenderTransactionReport(report, format);

        if (format == ReportFormat.Pdf)
        {
            Response.Headers.ContentDisposition = $"inline; filename=\"{renderedReport.FileName}\"";

            return File(renderedReport.Content, renderedReport.ContentType);
        }

        return File(renderedReport.Content, renderedReport.ContentType, renderedReport.FileName);
    }

    private async Task<(IReadOnlyCollection<BusinessPartnerLookupDto> BusinessPartners,
        IReadOnlyCollection<ProductLookupDto> Products)> LoadLookupsAsync(
        CancellationToken cancellationToken)
    {
        var businessPartners = await _sender.Send(new GetBusinessPartnerLookupQuery(), cancellationToken);
        var products = await _sender.Send(new GetProductLookupQuery(), cancellationToken);

        return (businessPartners, products);
    }

    private async Task RepopulateLookupsAsync(
        TransactionCreateViewModel viewModel,
        CancellationToken cancellationToken)
    {
        var (businessPartners, products) = await LoadLookupsAsync(cancellationToken);

        viewModel.BusinessPartners = businessPartners;
        viewModel.Products = products;
    }

    private async Task RepopulateLookupsAsync(
        TransactionEditViewModel viewModel,
        CancellationToken cancellationToken)
    {
        var (businessPartners, products) = await LoadLookupsAsync(cancellationToken);

        viewModel.BusinessPartners = businessPartners;
        viewModel.Products = products;
    }
}
