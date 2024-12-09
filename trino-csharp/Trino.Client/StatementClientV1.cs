// Enabling TEST_OUTPUT writes the JSON response and headers to a file in the current directory to use for UTs.
//#define TEST_OUTPUT
using Trino.Client.Logging;
using Trino.Client.Model;
using Trino.Client.Model.StatementV1;
using Trino.Client.Utils;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

using static Trino.Client.QueryState;

namespace Trino.Client
{
    /// <summary>
    /// Handles direct interactions with Trino statement rest API /v1/statement/
    /// </summary>
    internal class StatementClientV1 : AbstractClient<Statement>
    {
        // Initialize values for client response delay
        // Java client has 100ms initial delay, but 50ms provides noticably better performance in testing.
        private double readDelay = INITIAL_PAGE_READ_DELAY_MSEC;
        private int readCount = 0;
        private static readonly int INITIAL_PAGE_READ_DELAY_MSEC = (int)TimeSpan.FromMilliseconds(50).TotalMilliseconds;
        private static readonly int MAX_READ_DELAY_MSEC = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;
        // Java client does 100ms backoff but this affects query performance especially for metadata and cache operations.
        // This backoff produces less calls than the Java client.
        private static readonly double BACKOFF_AMOUNT = 1.2;

        private static readonly HashSet<HttpStatusCode> OK = new HashSet<HttpStatusCode> { HttpStatusCode.OK };
        private static readonly HashSet<HttpStatusCode> OKorNoContent = new HashSet<HttpStatusCode> { HttpStatusCode.OK, HttpStatusCode.NoContent };

        /// <summary>
        /// The default prefix for a parameterized query used when properties are provided.
        /// </summary>
        private string parameterizedQueryPrefix
        {
            get => Session.Properties.ServerType.ToLower();
        }

        /// <summary>
        /// Client capabilities is a comma separated list. Parametric datetime allows variable precision date times.
        /// </summary>
        private const string clientCapabilities = "PARAMETRIC_DATETIME";

        // Timeout properties
        private readonly Stopwatch stopwatch = new Stopwatch();

        /// <summary>
        /// Last statement v1 response. Used to get stats and status from the server.
        /// </summary>
        private Statement Statement { get; set; }
        private readonly ClientSessionOutput sessionSet = new ClientSessionOutput();

        /// <summary>
        /// The current executing state of the query.
        /// </summary>
        internal QueryState State { get; private set; }

        public bool IsTimeout
        {
            get => Session.Properties.Timeout.HasValue
                && Session.Properties.Timeout.Value.Ticks > 0
                && stopwatch.ElapsedTicks > Session.Properties.Timeout.Value.Ticks;
        }

        protected override string ResourcePath => throw new NotImplementedException();

        /// <summary>
        /// Constructor
        /// </summary>
        internal StatementClientV1(
            ClientSession session,
            CancellationToken cancellationToken,
            ILoggerWrapper logger = null) : base(session, logger, cancellationToken)
        {
            this.stopwatch.Start();
            this.State = new QueryState();

            HttpClientHandler handler = this.Session.Properties.CompressionDisabled ? new HttpClientHandler() : new HttpClientHandler() { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate };


            if (session.Properties.UseSystemTrustStore)
            {
                handler.ClientCertificateOptions = ClientCertificateOption.Automatic;
            }
            else
            {
                if (!string.IsNullOrEmpty(session.Properties.TrustedCertPath))
                {
                    try
                    {
                        X509Certificate2 cert = new X509Certificate2(session.Properties.TrustedCertPath);
                        handler.ClientCertificates.Add(cert);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Failed to load trusted certificate.", ex);
                    }
                }
                else if (!string.IsNullOrEmpty(session.Properties.TrustedCertificate))
                {
                    try
                    {
                        X509Certificate2 cert = ConvertPemToX509Certificate(session.Properties.TrustedCertificate);
                        handler.ClientCertificates.Add(cert);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Failed to load trusted certificate from PEM string.", ex);
                    }
                }
            }

            handler.ServerCertificateCustomValidationCallback = (HttpRequestMessage, X509Certificate2, x509Chain, sslPolicyErrors) =>
            {
                // Allow CN mismatch
                if (session.Properties.AllowHostNameCNMismatch
                    && sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
                {
                    return true;
                }

                // Allow self-signed certificates
                if (session.Properties.AllowSelfSignedServerCert
                    && sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors
                    && x509Chain.ChainStatus.Length == 1
                    && x509Chain.ChainStatus[0].Status == X509ChainStatusFlags.UntrustedRoot)
                {
                    return true;
                }

                // Default validation is not to allow any policy errors.
                return sslPolicyErrors == SslPolicyErrors.None;
            };

            HttpClient httpClient = new HttpClient(handler);
            this.httpClient.Timeout = Constants.HttpConnectionTimeout;

            if (!this.Session.Properties.CompressionDisabled)
            {
                httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
            }
        }

        /// <summary>
        /// Converts an embedded PEM-formatted certificate string into an X509Certificate2 object.
        /// </summary>
        /// <param name="pemString">The PEM-formatted certificate string, including "-----BEGIN CERTIFICATE-----" and "-----END CERTIFICATE-----" markers.</param>
        /// <returns>An X509Certificate2 object representing the certificate.</returns>
        internal static X509Certificate2 ConvertPemToX509Certificate(string pemString)
        {
            // Remove the PEM header and footer, extracting only the Base64 encoded portion
            string base64String = pemString
                .Replace("-----BEGIN CERTIFICATE-----", string.Empty)
                .Replace("-----END CERTIFICATE-----", string.Empty)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Trim();

            // Decode the Base64 string into a byte array
            byte[] certBytes = Convert.FromBase64String(base64String);

            // Create and return an X509Certificate2 object from the byte array
            return new X509Certificate2(certBytes);
        }

        /// <summary>
        /// Get response from Trino server.
        /// </summary>
        internal async Task<TrinoStats> GetInitialResponse(string statement, IEnumerable<QueryParameter> parameters, CancellationToken cancellationToken)
        {
            string responseContent = null;
            try
            {
                using (HttpRequestMessage queryRequest = this.BuildInitialQueryRequest(statement, parameters))
                {
                    logger?.LogDebug("Trino: sending request at {1} msec: {0}", queryRequest.RequestUri.ToString(), stopwatch.ElapsedMilliseconds);
                    responseContent = await GetResourceAsync(
                        httpClient,
                        this.RetryableResponses,
                        this.Session,
                        queryRequest,
                        OK,
                        cancellationToken).ConfigureAwait(false);

                    logger?.LogDebug("Trino: got response content: {0}", responseContent);
                    this.Statement = JsonConvert.DeserializeObject<Statement>(responseContent);
                    logger?.LogInformation("Query created queryId at {1} msec: {0}", Statement?.id, stopwatch.ElapsedMilliseconds);
                    return this.Statement.stats;
                }
            }
            catch (Exception e)
            {
                if (responseContent != null)
                {
                    throw new TrinoException("Error starting query. Got response: " + responseContent, e);
                }
                else
                {
                    throw e;
                }
            }
        }

        /// <summary>
        /// Build POST request to start query.
        /// </summary>
        private HttpRequestMessage BuildInitialQueryRequest(string query, IEnumerable<QueryParameter> parameters)
        {
            if (Session.Properties.Server == null)
            {
                throw new TrinoException("Invalid server URL: " + Session.Properties.Server);
            }
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"{Session.Properties.Server}v1/statement");

            // Handle parameterized queries on the server side by converting any parameterized query into a prepared statement.
            Dictionary<string, string> additionalPreparedStatements = new Dictionary<string, string>();
            if (parameters != null && parameters.Any())
            {
                string parameterizedQueryId = parameterizedQueryPrefix + Guid.NewGuid().ToString().Replace("-", "");
                additionalPreparedStatements.Add(parameterizedQueryId, query);
                logger?.LogDebug("Trino: Converting parameterized query to prepared statement: {0}", query);
                query = $"EXECUTE {parameterizedQueryId} USING {string.Join(", ", parameters.Select(p => p.SqlExpressionValue))}";
                logger?.LogDebug("Trino: Converted parameterized query to prepared statement: {0}", query);
            }
            AddHeadersToRequest(request, additionalPreparedStatements);
            request.Content = new StringContent(query);
            return request;
        }

        /// <summary>
        /// Delete query on server.
        /// </summary>
        internal async Task<bool> Cancel()
        {
            return await Cancel(QueryCancellationReason.USER_CANCEL).ConfigureAwait(false);
        }

        /// <summary>
        /// Delete query on server.
        /// </summary>
        private async Task<bool> Cancel(QueryCancellationReason reason = QueryCancellationReason.USER_CANCEL)
        {
            logger?.LogInformation("Cancelling due to {0} queryId:{1}", reason.ToString(), Statement?.id);
            // Sets client aborted state and terminates query.
            if (State.StateTransition(TrinoQueryStates.CLIENT_ABORTED, TrinoQueryStates.RUNNING))
            {
                logger?.LogInformation("Trino: Sending cancellation request queryId:{0}", Statement?.id);
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Delete, this.Statement.nextUri))
                {
                    // do not use cancellation token here as the query is already cancelled
                    string cancellationResponse = await GetResourceAsync(
                        httpClient,
                        this.RetryableResponses,
                        this.Session,
                        request,
                        OKorNoContent,
                        CancellationToken.None).ConfigureAwait(false);
                }
                logger?.LogInformation("Trino: Cancelled", Statement?.id);
            }
            else
            {
                logger?.LogInformation("Trino: Could not cancel query, already cancelled queryId:{0}, state:{1}", Statement?.id, this.State.ToString());
            }
            return this.State.IsClientAborted;
        }

        /// <summary>
        /// Fetches the next Trino page with data. Similar to Java client class with same name.
        /// </summary>
        internal async Task<ResponseQueueStatement> Advance()
        {
            if (this.Statement.nextUri.Contains("/executing"))
            {
                if (this.Statement.nextUri.Contains("?"))
                {
                    this.Statement.nextUri += $"&targetResultSize={Constants.MaxTargetResultSizeMB}MB";
                }
                else
                {
                    this.Statement.nextUri += $"?targetResultSize={Constants.MaxTargetResultSizeMB}MB";
                }
            }
            logger?.LogDebug("Trino: request: {1}", this.Statement.nextUri);

            string responseStr = await this.GetAsync(new Uri(this.Statement.nextUri), OK).ConfigureAwait(false);
            logger?.LogDebug("Trino: response: {1}", responseStr);
            QueryResultPage response = JsonConvert.DeserializeObject<QueryResultPage>(responseStr);
            logger?.LogDebug("Trino: response at {0} msec with state {1}", stopwatch.ElapsedMilliseconds, response.stats.state);

            // Note, the size is estimated based on the response string size which is not the actual deserialized size.
            ResponseQueueStatement responseQueueItem = new ResponseQueueStatement(response, responseStr.Length);
            if (responseQueueItem.Response.error != null)
            {
                State.StateTransition(TrinoQueryStates.CLIENT_ERROR, TrinoQueryStates.RUNNING);
                throw new TrinoException(responseQueueItem.Response.error.message, responseQueueItem.Response.error);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                await this.Cancel(QueryCancellationReason.USER_CANCEL).ConfigureAwait(false);
                throw new OperationCanceledException("Cancellation requested");
            }

            // Make status available
            this.Statement = responseQueueItem.Response;

            // If no next URI, the query is completed.
            if (this.Statement.IsLastPage)
            {
                this.Finish();
            }
            else if (this.IsTimeout)
            {
                logger?.LogInformation("Trino: Query timed out queryId:{0}, run time: {1} s, timeout {2} s.", Statement?.id, this.stopwatch.Elapsed.TotalSeconds, Session.Properties.ClientRequestTimeout.Value.TotalSeconds);
                await this.Cancel(QueryCancellationReason.TIMEOUT).ConfigureAwait(false);
                throw new TimeoutException($"Trino query ran for {this.stopwatch.Elapsed.TotalSeconds} s, exceeding the timeout of {Session.Properties.Timeout.Value.TotalSeconds} s.");
            }

            // Do not wait if the query had data - the next page may be ready immediately.
            if (!responseQueueItem.Response.HasData && !State.IsFinished && readCount > 4)
            {
                logger?.LogDebug("Trino: No data yet, backoff wait queryId:{0}, delay {1} msec", Statement?.id, readDelay);
                await Task.Delay((int)readDelay).ConfigureAwait(false);
                if (readDelay < MAX_READ_DELAY_MSEC)
                {
                    readDelay *= BACKOFF_AMOUNT;
                }
            }
            readCount++;
            return responseQueueItem;
        }

        /// <summary>
        /// Set states to indicate the query has finished.
        /// </summary>
        private void Finish()
        {
            this.stopwatch.Stop();
            this.Session.Update(this.sessionSet);
            State.StateTransition(TrinoQueryStates.FINISHED, TrinoQueryStates.RUNNING);
            logger?.LogInformation("Trino: Query finished queryId:{0}", Statement?.id);
        }

        /// <summary>
        /// Capture all response headers and set session properties.
        /// </summary>
        protected override void ProcessResponseHeaders(HttpResponseHeaders headers)
        {
            string setCatalog = headers.GetValuesOrEmpty(protocolHeaders.ResponseSetCatalog).FirstOrDefault();
            if (setCatalog != null)
            {
                this.sessionSet.SetCatalog = setCatalog;
            }

            string setSchema = headers.GetValuesOrEmpty(protocolHeaders.ResponseSetSchema).FirstOrDefault();
            if (setSchema != null)
            {
                this.sessionSet.SetSchema = setSchema;
            }

            this.sessionSet.SetPath = headers.GetValuesOrEmpty(protocolHeaders.ResponseSetPath).FirstOrDefault();

            string setAuthorizationUser = headers.GetValuesOrEmpty(protocolHeaders.ResponseSetAuthorizationUser).FirstOrDefault();
            if (setAuthorizationUser != null)
            {
                this.sessionSet.SetAuthorizationUser = setAuthorizationUser;
            }

            string resetAuthorizationUser = headers.GetValuesOrEmpty(protocolHeaders.ResponseSetAuthorizationUser).FirstOrDefault();
            if (setAuthorizationUser != null)
            {
                if (bool.TryParse(resetAuthorizationUser, out bool resetAuthorizationUserBool))
                {
                    this.sessionSet.ResetAuthorizationUser = resetAuthorizationUserBool;
                }
            }

            foreach (string session in headers.GetValuesOrEmpty(protocolHeaders.ResponseSetSession))
            {
                string[] keyValue = session.Split('=');
                if (keyValue.Length != 2)
                {
                    continue;
                }
                this.sessionSet.SetSessionProperties.Add(keyValue[0], HttpUtility.UrlDecode(keyValue[1]));
            }

            foreach (string preparedStatement in headers.GetValuesOrEmpty(protocolHeaders.ResponseAddedPrepare))
            {
                string[] keyValue = preparedStatement.Split('=');
                if (keyValue.Length != 2)
                {
                    throw new TrinoException("Invalid response header. Expecting key=value: " + protocolHeaders.ResponseAddedPrepare + ": " + preparedStatement);
                }
                string value = HttpUtility.UrlDecode(keyValue[1]);
                this.sessionSet.ResponseAddedPrepare.Add(keyValue[0], value);
            }

            foreach (string deallocateStatement in headers.GetValuesOrEmpty(protocolHeaders.ResponseDeallocatedPrepare))
            {
                string[] keyValue = deallocateStatement.Split('=');
                if (keyValue.Length != 2)
                {
                    throw new TrinoException("Invalid response header. Expecting key=value: " + protocolHeaders.ResponseDeallocatedPrepare + ": " + deallocateStatement);
                }
                string value = HttpUtility.UrlDecode(keyValue[1]);
                this.sessionSet.ResponseDeallocatedPrepare.Add(keyValue[0], value);
            }
        }

        private void AddHeadersToRequest(HttpRequestMessage request, Dictionary<string, string> additionalPreparedStatements)
        {
            request.Headers.Add(protocolHeaders.RequestClientCapabilities, clientCapabilities);

            if (Session.Properties.AdditionalHeaders != null)
            {
                foreach (KeyValuePair<string, string> header in Session.Properties.AdditionalHeaders)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            if (!string.IsNullOrEmpty(Session.Properties.Source))
            {
                request.Headers.Add(protocolHeaders.RequestSource, Session.Properties.Source);
            }

            if (!string.IsNullOrEmpty(Session.Properties.TraceToken))
            {
                request.Headers.Add(protocolHeaders.RequestTraceToken, Session.Properties.TraceToken);
            }

            if (Session.Properties.ClientTags.Count > 0)
            {
                request.Headers.Add(protocolHeaders.RequestClientTags, string.Join(",", Session.Properties.ClientTags));
            }

            if (!string.IsNullOrEmpty(Session.Properties.ClientInfo))
            {
                request.Headers.Add(protocolHeaders.RequestClientInfo, Session.Properties.ClientInfo);
            }

            if (!string.IsNullOrEmpty(Session.Properties.Catalog))
            {
                request.Headers.Add(protocolHeaders.RequestCatalog, Session.Properties.Catalog);
            }

            if (!string.IsNullOrEmpty(Session.Properties.Schema))
            {
                request.Headers.Add(protocolHeaders.RequestSchema, Session.Properties.Schema);
            }

            if (!string.IsNullOrEmpty(Session.Properties.Path))
            {
                request.Headers.Add(protocolHeaders.RequestPath, Session.Properties.Path);
            }

            if (Session.Properties.TimeZone != null)
            {
                request.Headers.Add(protocolHeaders.RequestTimeZone, Session.Properties.TimeZone);
            }

            if (Session.Properties.Locale != null)
            {
                request.Headers.Add(protocolHeaders.RequestLanguage, Session.Properties.Locale.ToString());
            }

            Dictionary<string, string> property = Session.Properties.Properties;
            foreach (KeyValuePair<string, String> pair in property)
            {
                request.Headers.Add(protocolHeaders.RequestSession, $"{pair.Key}={HttpUtility.UrlEncode(pair.Value)}");
            }

            Dictionary<string, string> resourceEstimates = Session.Properties.ResourceEstimates;
            foreach (KeyValuePair<string, String> pair in resourceEstimates)
            {
                request.Headers.Add(protocolHeaders.RequestResourceEstimate, $"{pair.Key}={HttpUtility.UrlEncode(pair.Value)}");
            }

            Dictionary<string, ClientSelectedRole> roles = Session.Properties.Roles;
            foreach (KeyValuePair<string, ClientSelectedRole> pair in roles)
            {
                request.Headers.Add(protocolHeaders.RequestRole, $"{pair.Key}={HttpUtility.UrlEncode(pair.Value.ToString())}");
            }

            Dictionary<string, string> extraCredentials = Session.Properties.ExtraCredentials;
            foreach (KeyValuePair<string, string> pair in extraCredentials)
            {
                request.Headers.Add(protocolHeaders.RequestExtraCredential, $"{pair.Key}={HttpUtility.UrlEncode(pair.Value.ToString())}");
            }

            foreach (KeyValuePair<string, string> pair in Session.Properties.PreparedStatements)
            {
                request.Headers.Add(protocolHeaders.RequestPreparedStatement, $"{pair.Key}={HttpUtility.UrlEncode(pair.Value.ToString())}");
            }

            if (additionalPreparedStatements != null)
            {
                foreach (KeyValuePair<string, string> pair in additionalPreparedStatements)
                {
                    request.Headers.Add(protocolHeaders.RequestPreparedStatement, $"{pair.Key}={HttpUtility.UrlEncode(pair.Value.ToString())}");
                }
            }

            if (string.IsNullOrEmpty(Session.Properties.TransactionId))
            {
                request.Headers.Add(protocolHeaders.RequestTransactionId, Session.Properties.TransactionId);
            }
        }

        private enum QueryCancellationReason
        {
            TIMEOUT,
            USER_CANCEL
        }
    }
}
