using System.Collections.Generic;

namespace Trino.Client
{
    /// <summary>
    /// Contains client session properties that are recieved from Trino.
    /// </summary>
    internal class ClientSessionOutput
    {
        public ClientSessionOutput() {
            this.ResponseAddedPrepare = new Dictionary<string, string>();
            this.ResponseDeallocatedPrepare = new Dictionary<string, string>();
        }

        internal string SetCatalog { get; set; }
        internal string SetSchema { get; set; }
        internal string SetPath { get; set; }
        internal string SetAuthorizationUser { get; set; }
        internal bool ResetAuthorizationUser { get; set; }
        internal Dictionary<string, string> SetSessionProperties { get; set; } = new Dictionary<string, string>();
        internal Dictionary<string, string> ResponseAddedPrepare { get; set; }
        internal Dictionary<string, string> ResponseDeallocatedPrepare { get; set; }
    }
}
