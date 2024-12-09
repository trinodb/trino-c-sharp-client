using System.Net.Http;

namespace Trino.Client.Auth
{
    /// <summary>
    /// Interface defining a Trino user
    /// </summary>
    public interface ITrinoAuth
    {
        /// <summary>
        /// Triggers manual authorization.
        /// </summary>
        void AuthorizeAndValidate();

        /// <summary>
        /// Customize adding credential to request
        /// </summary>
        /// <param name="httpRequestMessage">Http request definition</param>
        void AddCredentialToRequest(HttpRequestMessage httpRequestMessage);
    }
}
