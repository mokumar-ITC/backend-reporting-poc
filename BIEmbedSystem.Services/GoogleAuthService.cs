using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;

namespace BIEmbedSystem.Services
{
    public class GoogleAuthService
    {
        public async Task<UserCredential> AuthenticateAsync()
        {
            string[] scopes = { "https://mail.google.com/" };

            using (var stream = new FileStream("client_secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = "GoogleCred/token.json";

                return await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)
                );
            }
        }
    }
}



