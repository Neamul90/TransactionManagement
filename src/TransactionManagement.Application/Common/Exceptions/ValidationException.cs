using FluentValidation.Results;

namespace TransactionManagement.Application.Common.Exceptions;

/// <summary>
/// Carries FluentValidation failures out of the MediatR pipeline in a presentation-agnostic shape.
/// The Web layer converts <see cref="Errors"/> into <c>ModelState</c> entries.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException()
        : base("One or more validation failures occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(failure => failure.PropertyName, failure => failure.ErrorMessage)
            .ToDictionary(group => group.Key, group => group.Distinct().ToArray());
    }

    public IDictionary<string, string[]> Errors { get; }
}
