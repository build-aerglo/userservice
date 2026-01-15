namespace UserService.Application.DTOs.Auth;

public static class SocialProvider
{
    public const string Google = "google-oauth2";
    public const string Facebook = "facebook";
    public const string Apple = "apple";
    public const string GitHub = "github";
    public const string Twitter = "twitter";
    public const string LinkedIn = "linkedin";

    public static readonly string[] All = [Google, Facebook, Apple, GitHub, Twitter, LinkedIn];

    public static bool IsValid(string provider)
    {
        return All.Contains(provider.ToLowerInvariant()) ||
               GetAuth0Connection(provider) != null;
    }

    public static string GetAuth0Connection(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "google" or "google-oauth2" => "google-oauth2",
            "facebook" => "Facebook",
            "apple" => "Apple",
            "github" => "GitHub",
            "twitter" or "x" => "Twitter",
            "linkedin" => "linkedin",
            _ => provider
        };
    }

    public static string GetDisplayName(string provider)
    {
        return provider.ToLowerInvariant() switch
        {
            "google" or "google-oauth2" => "Google",
            "facebook" => "Facebook",
            "apple" => "Apple",
            "github" => "GitHub",
            "twitter" or "x" => "Twitter/X",
            "linkedin" => "LinkedIn",
            _ => provider
        };
    }
}
