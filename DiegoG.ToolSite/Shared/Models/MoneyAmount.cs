using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NodaMoney;

namespace DiegoG.ToolSite.Shared.Models;

public readonly struct MoneyAmount
{
    public decimal Amount { get; }
    public string CurrencyCode { get; }

    public MoneyAmount(decimal amount, string currencyCode)
    {
        Amount = amount;
        CurrencyCode = currencyCode ?? throw new ArgumentNullException(nameof(currencyCode));
    }

    public Money ToNodaMoney()
        => new(Amount, CurrencyCode);
}

public static class MoneyExtensions
{
    public static MoneyAmount ToMoneyAmount(this Money money)
        => new(money.Amount, money.Currency.Code);
}