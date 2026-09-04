using Microsoft.AspNetCore.Mvc.ModelBinding;
using ValidationException = TransactionManagement.Application.Common.Exceptions.ValidationException;

namespace TransactionManagement.Web.Extensions;

public static class ModelStateExtensions
{
    /// <summary>
    /// Copies FluentValidation failures raised in the MediatR pipeline into <c>ModelState</c> so the
    /// form is redisplayed with the same messages the server enforced — the browser is never the
    /// authority on validity, it only gets to show the result.
    /// </summary>
    public static void AddValidationErrors(
        this ModelStateDictionary modelState,
        ValidationException exception)
    {
        foreach (var (propertyName, errorMessages) in exception.Errors)
        {
            foreach (var errorMessage in errorMessages)
            {
                modelState.AddModelError(propertyName, errorMessage);
            }
        }
    }
}
