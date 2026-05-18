namespace SwiftPos.Api.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "SwiftPOS";
    public string Audience { get; set; } = "SwiftPOS.Clients";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 480;
}
