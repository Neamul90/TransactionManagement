using MediatR;
using TransactionManagement.Application.Dtos.Lookups;

namespace TransactionManagement.Application.Lookups.Queries.GetProductLookup;

/// <summary>Active products for the detail grid's product drop-down.</summary>
public sealed record GetProductLookupQuery : IRequest<IReadOnlyCollection<ProductLookupDto>>;
