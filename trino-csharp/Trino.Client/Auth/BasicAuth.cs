using System;
using System.Net.Http;
using System.Text;

namespace Trino.Client.Auth
{
    /// <summary>
    /// For setting up basic authentication with a username and optional password.
    /// </summary>
    public class BasicAuth : ITrinoAuth
    {
        public BasicAuth()
        {
        }

        public string User
        {
            get;
            set;
        }

        public string Password { 
            get; 
            set; 
        }

        public virtual void AuthorizeAndValidate()
        {
            if (string.IsNullOrEmpty(User) )
            {
                throw new ArgumentException("BasicAuth: username property is null or empty");
            }
        }

        /// <summary>
        /// Modify the request with authentication
        /// </summary>
        /// <param name="httpRequestMessage">Http request message</param>
        public virtual void AddCredentialToRequest(HttpRequestMessage httpRequestMessage)
        {
           var credentials = Convert.ToBase64String(string.IsNullOrEmpty(Password) ? 
                          Encoding.ASCII.GetBytes($"{User}") : 
                          Encoding.ASCII.GetBytes($"{User}:{Password}"));

            httpRequestMessage.Headers.Add("Authorization", "Basic " + credentials);
        }
    }
}
