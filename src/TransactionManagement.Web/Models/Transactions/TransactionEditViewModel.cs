using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TransactionManagement.Application.Dtos.Lookups;

namespace TransactionManagement.Web.Models.Transactions;

/// <summary>
/// Edit form model. <see cref="RowVersion"/> is the Base64 concurrency token that was rendered
/// with the form and is posted back so a stale save can be detected and refused.
/// </summary>
public sealed class TransactionEditViewModel
{
    public int Id { get; set; }

    [Display(Name = "Transaction No.")]
    [BindNever]
    public string TransactionNumber { get; set; } = string.Empty;

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

    [Required(ErrorMessage = "The concurrency token is missing. Please reload the transaction.")]
    public string RowVersion { get; set; } = string.Empty;

    public List<TransactionDetailRowViewModel> Details { get; set; } = [];

    [BindNever]
    public IReadOnlyCollection<BusinessPartnerLookupDto> BusinessPartners { get; set; } = [];

    [BindNever]
    public IReadOnlyCollection<ProductLookupDto> Products { get; set; } = [];
}
