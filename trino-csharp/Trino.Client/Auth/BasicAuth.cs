using System;
using System.Net.Http;
using System.Text;

namespace Trino.Client.Auth
{
    /// <summary>
    /// For testing purposes, you can use the BasicAuth class to authenticate with Trino.
    /// </summary>
    public class BasicAuth : ITrinoAuth
    {
        public BasicAuth()
        {
            User = "admin";
        }
        public BasicAuth(string user)
        {
            User = user;
        }

        public string User
        {
            get;
            set;
        }

        public void AuthorizeAndValidate()
        {
            if (string.IsNullOrEmpty(User) )
            {
                throw new ArgumentException("LDAPAuth: username property is null or empty");
            }
        }

        /// <summary>
        /// Modify the request with authentication
        /// </summary>
        /// <param name="httpRequestMessage">Http request message</param>
        public virtual void AddCredentialToRequest(HttpRequestMessage httpRequestMessage)
        {
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{User}" ));
            httpRequestMessage.Headers.Add("Authorization", "Basic " + credentials);
        }
    }
}
