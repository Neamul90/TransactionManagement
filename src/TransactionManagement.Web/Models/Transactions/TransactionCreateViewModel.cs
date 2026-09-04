using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransactionManagement.Application.Dtos.Lookups;

namespace TransactionManagement.Web.Models.Transactions;

/// <summary>
/// Create form model. It deliberately has no identifier, no transaction number and no concurrency
/// token: those are server-owned values and must not be accepted from the browser.
/// </summary>
public sealed class TransactionCreateViewModel
{
    [Display(Name = "Transaction Date")]
    [DataType(DataType.Date)]
    [Required(ErrorMessage = "Transaction date is required.")]
    public DateTime TransactionDate { get; set; } = DateTime.Today;

    [Display(Name = "Customer / Supplier")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a customer or supplier.")]
    public int BusinessPartnerId { get; set; }

    [Display(Name = "Reference")]
    [StringLength(50)]
    public string? Reference { get; set; }

    [Display(Name = "Remarks")]
    [StringLength(500)]
    public string? Remarks { get; set; }

    public List<TransactionDetailRowViewModel> Details { get; set; } = [];

    [BindNever]
    public IReadOnlyCollection<BusinessPartnerLookupDto> BusinessPartners { get; set; } = [];

    [BindNever]
    public IReadOnlyCollection<ProductLookupDto> Products { get; set; } = [];
}
