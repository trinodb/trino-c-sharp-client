using System;
using System.Net.Http;
using System.Text;

namespace Trino.Client.Auth
{
    public class LDAPAuth : ITrinoAuth
    {
        public LDAPAuth()
        {
        }

        public string User
        {
            get;
            set;
        }

        public string Password
        {
            get;
            set;
        }

        public void AuthorizeAndValidate()
        {
            if (string.IsNullOrEmpty(User) || string.IsNullOrEmpty(Password))
            {
                throw new ArgumentException("LDAPAuth: username or password property is null or empty");
            }
        }

        /// <summary>
        /// Modify the request with authentication
        /// </summary>
        /// <param name="httpRequestMessage">Http request message</param>
        public virtual void AddCredentialToRequest(HttpRequestMessage httpRequestMessage)
        {
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{User}:{Password}"));
            httpRequestMessage.Headers.Add("Authorization", "Basic " + credentials);
        }
    }
}
