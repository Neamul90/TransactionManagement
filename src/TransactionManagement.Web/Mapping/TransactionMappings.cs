using TransactionManagement.Application.Dtos.Transactions;
using TransactionManagement.Application.Transactions.Commands.CreateTransaction;
using TransactionManagement.Application.Transactions.Commands.UpdateTransaction;
using TransactionManagement.Web.Extensions;
using TransactionManagement.Web.Models.Transactions;

namespace TransactionManagement.Web.Mapping;

/// <summary>
/// Translation between the MVC boundary (ViewModels) and the application boundary (commands/DTOs).
/// Keeping it here means neither Razor nor the Application layer has to know about the other.
/// </summary>
public static class TransactionMappings
{
    public static CreateTransactionCommand ToCommand(this TransactionCreateViewModel viewModel) =>
        new(
            viewModel.TransactionDate,
            viewModel.BusinessPartnerId,
            viewModel.Reference,
            viewModel.Remarks,
            viewModel.Details
                .Select(detail => new CreateTransactionDetailCommand(
                    detail.ProductId,
                    detail.DetailDate,
                    detail.Description,
                    detail.Quantity,
                    detail.Amount,
                    detail.IsActive))
                .ToList());

    public static UpdateTransactionCommand ToCommand(this TransactionEditViewModel viewModel) =>
        new(
            viewModel.Id,
            viewModel.TransactionDate,
            viewModel.BusinessPartnerId,
            viewModel.Reference,
            viewModel.Remarks,
            RowVersionToken.FromToken(viewModel.RowVersion),
            viewModel.Details
                .Select(detail => new UpdateTransactionDetailCommand(
                    detail.Id,
                    detail.ProductId,
                    detail.DetailDate,
                    detail.Description,
                    detail.Quantity,
                    detail.Amount,
                    detail.IsActive))
                .ToList());

    public static TransactionEditViewModel ToEditViewModel(this TransactionDetailsDto transaction) =>
        new()
        {
            Id = transaction.Id,
            TransactionNumber = transaction.TransactionNumber,
            TransactionDate = transaction.TransactionDate,
            BusinessPartnerId = transaction.BusinessPartnerId,
            Reference = transaction.Reference,
            Remarks = transaction.Remarks,
            RowVersion = RowVersionToken.ToToken(transaction.RowVersion),
            Details = transaction.Details
                .Select(detail => new TransactionDetailRowViewModel
                {
                    Id = detail.Id,
                    ProductId = detail.ProductId,
                    DetailDate = detail.DetailDate,
                    Description = detail.Description,
                    Quantity = detail.Quantity,
                    Amount = detail.Amount,
                    IsActive = detail.IsActive,
                    ProductDisplayText = $"{detail.ProductCode} - {detail.ProductName}"
                })
                .ToList()
        };
}
