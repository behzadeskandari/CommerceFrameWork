namespace Commerce.Framework.Domain.ValueObjects;

public sealed record Money : IComparable<Money>
{
    public const int DefaultScale = 4;

    public decimal Amount { get; }
    public Currency Currency { get; }

    private Money(decimal amount, Currency currency)
    {
        Amount = Round(amount);
        Currency = currency;
    }

    public static Money Create(decimal amount, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount cannot be negative.");
        }

        if (!HasValidScale(amount))
        {
            throw new ArgumentOutOfRangeException(nameof(amount), $"Amount cannot have more than {DefaultScale} decimal places.");
        }

        return new Money(amount, currency);
    }

    public static Money Zero(Currency currency) => Create(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        var result = Amount - other.Amount;
        if (result < 0)
        {
            throw new InvalidOperationException("Subtraction would result in a negative amount.");
        }

        return new Money(result, Currency);
    }

    public Money Multiply(decimal factor)
    {
        if (factor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(factor), "Multiplication factor cannot be negative.");
        }

        return new Money(Amount * factor, Currency);
    }

    public int CompareTo(Money? other)
    {
        if (other is null)
        {
            return 1;
        }

        EnsureSameCurrency(other);
        return Amount.CompareTo(other.Amount);
    }

    public static bool operator >(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) > 0;
    }

    public static bool operator <(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.CompareTo(right) < 0;
    }

    public static bool operator >=(Money left, Money right) => left > right || left.Equals(right);

    public static bool operator <=(Money left, Money right) => left < right || left.Equals(right);

    private void EnsureSameCurrency(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);

        if (!Currency.Equals(other.Currency))
        {
            throw new InvalidOperationException(
                $"Cannot operate on amounts with different currencies: {Currency.Code} and {other.Currency.Code}.");
        }
    }

    private static decimal Round(decimal amount) =>
        decimal.Round(amount, DefaultScale, MidpointRounding.ToEven);

    private static bool HasValidScale(decimal amount)
    {
        var bits = decimal.GetBits(amount);
        var scale = (bits[3] >> 16) & 0x7F;
        return scale <= DefaultScale;
    }
}
