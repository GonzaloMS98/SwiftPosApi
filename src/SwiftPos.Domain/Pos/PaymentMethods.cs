namespace SwiftPos.Domain.Pos;

public static class PaymentMethods
{
    public const string Cash = "CASH";
    public const string CardManual = "CARD_MANUAL";

    public static bool IsValid(string value)
    {
        return value is Cash or CardManual;
    }
}
