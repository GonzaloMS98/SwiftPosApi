using SwiftPos.Domain.Pos;

namespace SwiftPos.Tests;

public sealed class PosTotalsTests
{
    [Fact]
    public void CalculateLine_ReturnsSubtotalTaxAndTotal()
    {
        var totals = PosTotals.CalculateLine(unitPrice: 3.50m, taxRate: 0.1600m, quantity: 2);

        Assert.Equal(7.00m, totals.Subtotal);
        Assert.Equal(1.12m, totals.TaxTotal);
        Assert.Equal(8.12m, totals.Total);
    }

    [Fact]
    public void CalculateLine_RoundsMoneyAwayFromZero()
    {
        var totals = PosTotals.CalculateLine(unitPrice: 0.05m, taxRate: 0.10m, quantity: 1);

        Assert.Equal(0.05m, totals.Subtotal);
        Assert.Equal(0.01m, totals.TaxTotal);
        Assert.Equal(0.06m, totals.Total);
    }

    [Fact]
    public void CalculateOrder_SumsLineTotals()
    {
        var totals = PosTotals.CalculateOrder(
        [
            new PosLineTotals(7.00m, 1.12m, 8.12m),
            new PosLineTotals(3.25m, 0.52m, 3.77m)
        ]);

        Assert.Equal(10.25m, totals.Subtotal);
        Assert.Equal(1.64m, totals.TaxTotal);
        Assert.Equal(11.89m, totals.Total);
    }
}
