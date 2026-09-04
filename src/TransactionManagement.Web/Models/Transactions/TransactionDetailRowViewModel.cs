using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TransactionManagement.Web.Models.Transactions;

/// <summary>
/// One row of the master–detail grid.
/// <para>
/// <see cref="Id"/> is zero for a row the user just added in the browser and carries the database
/// identifier for an existing row. It is a hint, not an authority: the aggregate re-checks that
/// the identifier really belongs to the transaction being saved.
/// </para>
/// </summary>
public sealed class TransactionDetailRowViewModel
{
    public int Id { get; set; }

    [Display(Name = "Product")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a product.")]
    public int ProductId { get; set; }

    [Display(Name = "Date")]
    [DataType(DataType.Date)]
    [Required(ErrorMessage = "Please supply a date.")]
    public DateTime DetailDate { get; set; } = DateTime.Today;

    [Display(Name = "Description")]
    [StringLength(250, ErrorMessage = "Description cannot exceed 250 characters.")]
    public string? Description { get; set; }

    [Display(Name = "Quantity")]
    [Range(0.001, 9_999_999, ErrorMessage = "Quantity must be greater than zero.")]
    public decimal Quantity { get; set; } = 1m;

    [Display(Name = "Amount")]
    [Range(0, 999_999_999_999, ErrorMessage = "Amount cannot be negative.")]
    public decimal Amount { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    /// <summary>Display-only; never bound from the request.</summary>
    [BindNever]
    public string ProductDisplayText { get; set; } = string.Empty;
}
