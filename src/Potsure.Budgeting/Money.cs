using System.Globalization;

namespace Potsure.Budgeting;

/// <summary>
/// GBP monetary amount with scale 2; report emission uses banker's rounding.
/// </summary>
public readonly record struct Money(decimal Amount)
{
    public static Money Zero => new(0m);

    public static Money FromRaw(decimal amount) => new(amount);

    public Money RoundForReport() => new(Math.Round(Amount, 2, MidpointRounding.ToEven));

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static Money operator -(Money left, Money right) => new(left.Amount - right.Amount);

    public static Money operator -(Money value) => new(-value.Amount);

    public static Money operator *(Money left, decimal right) => new(left.Amount * right);

    public static Money operator /(Money left, decimal right) =>
        right == 0m ? throw new DivideByZeroException() : new(left.Amount / right);

    public static bool operator >=(Money left, Money right) => left.Amount >= right.Amount;

    public static bool operator <=(Money left, Money right) => left.Amount <= right.Amount;

    public static bool operator >(Money left, Money right) => left.Amount > right.Amount;

    public static bool operator <(Money left, Money right) => left.Amount < right.Amount;

    public override string ToString() => Amount.ToString("F2", CultureInfo.InvariantCulture);
}
