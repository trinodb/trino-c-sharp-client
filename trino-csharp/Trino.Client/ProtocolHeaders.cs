namespace Trino.Client
{
    /// <summary>
    /// Define the protocol header names for a Trino connection.
    /// </summary>
    public class ProtocolHeaders
    {
        internal string Name { get; private set; }
        internal string RequestUser { get; private set; }
        internal string RequestOriginalUser { get; private set; }
        internal string RequestSource { get; private set; }
        internal string RequestCatalog { get; private set; }
        internal string RequestSchema { get; private set; }
        internal string RequestPath { get; private set; }
        internal string RequestTimeZone { get; private set; }
        internal string RequestLanguage { get; private set; }
        internal string RequestTraceToken { get; private set; }
        internal string RequestSession { get; private set; }
        internal string RequestRole { get; private set; }
        internal string RequestPreparedStatement { get; private set; }
        internal string RequestTransactionId { get; private set; }
        internal string RequestClientInfo { get; private set; }
        internal string RequestClientTags { get; private set; }
        internal string RequestClientCapabilities { get; private set; }
        internal string RequestResourceEstimate { get; private set; }
        internal string RequestExtraCredential { get; private set; }
        internal string ResponseSetCatalog { get; private set; }
        internal string ResponseSetSchema { get; private set; }
        internal string ResponseSetPath { get; private set; }
        internal string ResponseSetSession { get; private set; }
        internal string ResponseClearSession { get; private set; }
        internal string ResponseSetRole { get; private set; }
        internal string ResponseAddedPrepare { get; private set; }
        internal string ResponseDeallocatedPrepare { get; private set; }
        internal string ResponseStartedTransactionId { get; private set; }
        internal string ResponseClearTransactionId { get; private set; }
        internal string ResponseSetAuthorizationUser { get; private set; }
        internal string ResponseResetAuthorizationUser { get; private set; }

        internal ProtocolHeaders(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new TrinoException("Protocol name was not provided.");
            }

            this.Name = name;
            string prefix = "X-" + name + "-";
            RequestUser = prefix + "User";
            RequestOriginalUser = prefix + "Original-User";
            RequestSource = prefix + "Source";
            RequestCatalog = prefix + "Catalog";
            RequestSchema = prefix + "Schema";
            RequestPath = prefix + "Path";
            RequestTimeZone = prefix + "Time-Zone";
            RequestLanguage = prefix + "Language";
            RequestTraceToken = prefix + "Trace-Token";
            RequestSession = prefix + "Session";
            RequestRole = prefix + "Role";
            RequestPreparedStatement = prefix + "Prepared-Statement";
            RequestTransactionId = prefix + "Transaction-Id";
            RequestClientInfo = prefix + "Client-Info";
            RequestClientTags = prefix + "Client-Tags";
            RequestClientCapabilities = prefix + "Client-Capabilities";
            RequestResourceEstimate = prefix + "Resource-Estimate";
            RequestExtraCredential = prefix + "Extra-Credential";
            ResponseSetCatalog = prefix + "Set-Catalog";
            ResponseSetSchema = prefix + "Set-Schema";
            ResponseSetPath = prefix + "Set-Path";
            ResponseSetSession = prefix + "Set-Session";
            ResponseClearSession = prefix + "Clear-Session";
            ResponseSetRole = prefix + "Set-Role";
            ResponseAddedPrepare = prefix + "Added-Prepare";
            ResponseDeallocatedPrepare = prefix + "Deallocated-Prepare";
            ResponseStartedTransactionId = prefix + "Started-Transaction-Id";
            ResponseClearTransactionId = prefix + "Clear-Transaction-Id";
            ResponseSetAuthorizationUser = prefix + "Set-Authorization-User";
            ResponseResetAuthorizationUser = prefix + "Reset-Authorization-User";
        }
    }
}
