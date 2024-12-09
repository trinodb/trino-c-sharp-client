using System.Net.Http;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Core;
using System;

namespace Trino.Client.Auth
{
    public class TrinoAzureDefaultAuth : ITrinoAuth
    {
        private readonly DefaultAzureCredential _credential;
        private AccessToken _accessToken;
        private readonly string _scope;

        public TrinoAzureDefaultAuth(string scope)
        {
            _credential = new DefaultAzureCredential();
            _scope = scope;
            _accessToken = GetTokenAsync().GetAwaiter().GetResult();
        }

        public void AuthorizeAndValidate()
        {
            // This method can be used to trigger manual authorization if needed.
            // For example, you could prompt the user to login or refresh the token.
            // Here, we'll just fetch the token.
            _accessToken = GetTokenAsync().GetAwaiter().GetResult();
        }

        public void AddCredentialToRequest(HttpRequestMessage httpRequestMessage)
        {
            if (_accessToken.ExpiresOn <= DateTimeOffset.UtcNow)
            {
                _accessToken = GetTokenAsync().GetAwaiter().GetResult();
            }

            httpRequestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken.Token);
        }

        private async Task<AccessToken> GetTokenAsync()
        {
            return await _credential.GetTokenAsync(new TokenRequestContext(new string[] { _scope }));
        }
    }
}
