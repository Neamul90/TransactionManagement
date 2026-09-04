using TransactionManagement.Domain.Exceptions;

namespace TransactionManagement.Domain.Common;

/// <summary>
/// Small guard helper used by entities to express invariants without repeating throw statements.
/// It throws <see cref="BusinessRuleViolationException"/> so that invariant breaches are
/// reported to the user as business errors rather than as unexpected failures.
/// </summary>
internal static class Guard
{
    internal static string AgainstNullOrWhiteSpace(string? value, string ruleMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BusinessRuleViolationException(ruleMessage);
        }

        return value.Trim();
    }

    internal static string AgainstExceedingLength(string value, int maxLength, string ruleMessage)
    {
        if (value.Length > maxLength)
        {
            throw new BusinessRuleViolationException(ruleMessage);
        }

        return value;
    }

    internal static int AgainstNonPositive(int value, string ruleMessage)
    {
        if (value <= 0)
        {
            throw new BusinessRuleViolationException(ruleMessage);
        }

        return value;
    }

    internal static decimal AgainstNonPositive(decimal value, string ruleMessage)
    {
        if (value <= 0m)
        {
            throw new BusinessRuleViolationException(ruleMessage);
        }

        return value;
    }

    internal static decimal AgainstNegative(decimal value, string ruleMessage)
    {
        if (value < 0m)
        {
            throw new BusinessRuleViolationException(ruleMessage);
        }

        return value;
    }

    internal static void Against(bool invalidCondition, string ruleMessage)
    {
        if (invalidCondition)
        {
            throw new BusinessRuleViolationException(ruleMessage);
        }
    }
}
