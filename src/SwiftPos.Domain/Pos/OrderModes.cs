namespace SwiftPos.Domain.Pos;

public static class OrderModes
{
    public const string DineIn = "DINE_IN";
    public const string Takeaway = "TAKEAWAY";
    public const string PickGo = "PICK_GO";

    public static bool IsValid(string value)
    {
        return value is DineIn or Takeaway or PickGo;
    }
}
