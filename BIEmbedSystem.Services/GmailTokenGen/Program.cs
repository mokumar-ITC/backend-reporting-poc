using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Util.Store;
using Google.Apis.Services;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🔐 Generating Gmail OAuth Refresh Token...");

        var secrets = GoogleClientSecrets.FromFile("credentials.json").Secrets;

        string[] scopes = { GmailService.Scope.GmailSend };

        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            scopes,
            "user",
            CancellationToken.None,
            new FileDataStore("TokenStore", true)
        );

        Console.WriteLine("\n=========================================");
        Console.WriteLine("⭐ ACCESS TOKEN:");
        Console.WriteLine(credential.Token.AccessToken);
        Console.WriteLine("\n⭐ REFRESH TOKEN:");
        Console.WriteLine(credential.Token.RefreshToken);
        Console.WriteLine("=========================================\n");

        Console.WriteLine("✔ Copy the refresh token into your appsettings.json");
    }
}
