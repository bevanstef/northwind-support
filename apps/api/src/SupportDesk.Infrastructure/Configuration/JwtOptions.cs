namespace SupportDesk.Infrastructure.Configuration;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "NorthwindSupport";

    public string Audience { get; set; } = "NorthwindSupportWeb";

    public string Key { get; set; } = "Local_Dev_Jwt_Secret_Key_Change_In_Production_Minimum_32_Characters!";

    public int ExpiryMinutes { get; set; } = 480;
}
