using MediatR;
using Microsoft.AspNetCore.Mvc;
using TransactionManagement.Application.Lookups.Queries;
using TransactionManagement.Application.Lookups.Queries.SearchBusinessPartners;
using TransactionManagement.Application.Lookups.Queries.SearchProducts;

namespace TransactionManagement.Web.Controllers;

/// <summary>
/// JSON type-ahead endpoints for the pickers on the transaction form.
/// <para>
/// This is the one place the application returns JSON rather than a view. It exists because the
/// alternative — rendering every product and partner into the page — does not survive a real
/// catalogue: 22,000 products would mean 22,000 option elements per detail row. The endpoints are
/// read-only, bounded by <see cref="LookupDefaults"/>, and dispatch through the same MediatR
/// pipeline as every screen, so validation, logging and error handling behave identically.
/// </para>
/// </summary>
public sealed class LookupsController : Controller
{
    private readonly ISender _sender;

    public LookupsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Shaped as <c>{ results: [{ id, text }] }</c>, which is what Select2 expects.</summary>
    [HttpGet]
    public async Task<IActionResult> Products(string? term, CancellationToken cancellationToken)
    {
        var products = await _sender.Send(new SearchProductsQuery(term), cancellationToken);

        return Json(new
        {
            results = products.Select(product => new
            {
                id = product.Id,
                text = product.DisplayText,
                unit = product.UnitOfMeasure,
                price = product.DefaultUnitPrice
            })
        });
    }

    [HttpGet]
    public async Task<IActionResult> BusinessPartners(string? term, CancellationToken cancellationToken)
    {
        var partners = await _sender.Send(new SearchBusinessPartnersQuery(term), cancellationToken);

        return Json(new
        {
            results = partners.Select(partner => new
            {
                id = partner.Id,
                text = partner.DisplayText
            })
        });
    }
}
