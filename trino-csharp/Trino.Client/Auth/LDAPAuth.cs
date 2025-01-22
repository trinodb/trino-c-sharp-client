using System;
using System.Net.Http;
using System.Text;

namespace Trino.Client.Auth
{
    public class LDAPAuth : BasicAuth
    {
        public LDAPAuth()
        {
        }

        public override void AuthorizeAndValidate()
        {
            if (string.IsNullOrEmpty(User) || string.IsNullOrEmpty(Password))
            {
                throw new ArgumentException("LDAPAuth: username or password property is null or empty");
            }
        }
    }
}
