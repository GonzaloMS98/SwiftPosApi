namespace SwiftPos.Domain.Pos;

public sealed record PosLineTotals(decimal Subtotal, decimal TaxTotal, decimal Total);

public sealed record PosOrderTotals(decimal Subtotal, decimal TaxTotal, decimal Total);

public static class PosTotals
{
    public static PosLineTotals CalculateLine(decimal unitPrice, decimal taxRate, int quantity)
    {
        var subtotal = decimal.Round(unitPrice * quantity, 2, MidpointRounding.AwayFromZero);
        var taxTotal = decimal.Round(subtotal * taxRate, 2, MidpointRounding.AwayFromZero);
        var total = subtotal + taxTotal;

        return new PosLineTotals(subtotal, taxTotal, total);
    }

    public static PosOrderTotals CalculateOrder(IEnumerable<PosLineTotals> lines)
    {
        var subtotal = decimal.Round(lines.Sum(line => line.Subtotal), 2, MidpointRounding.AwayFromZero);
        var taxTotal = decimal.Round(lines.Sum(line => line.TaxTotal), 2, MidpointRounding.AwayFromZero);
        var total = subtotal + taxTotal;

        return new PosOrderTotals(subtotal, taxTotal, total);
    }
}
