using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Trino.Client.Logging;
using System.Net.Http.Headers;

namespace Trino.Client
{
    public abstract class AbstractClient<T>
    {
        private const string TrinoClientName = ".NET Trino Client";
        private static readonly HashSet<HttpStatusCode> defaultExpectedResponseCodes = new HashSet<HttpStatusCode> { HttpStatusCode.OK };
        protected abstract string ResourcePath { get; }
        protected internal HttpClient httpClient;
        protected internal ClientSession Session { get; set; }
        protected internal ILoggerWrapper logger;
        protected internal CancellationToken cancellationToken;
        protected internal ProtocolHeaders protocolHeaders;

        // HTTP status codes that allow for a retry
        protected internal HashSet<HttpStatusCode> RetryableResponses = new HashSet<HttpStatusCode>() { HttpStatusCode.BadGateway, HttpStatusCode.ServiceUnavailable, HttpStatusCode.GatewayTimeout };

        protected AbstractClient(ClientSession session, ILoggerWrapper logger, CancellationToken cancellationToken)
        {
            this.httpClient = new HttpClient();
            Session = session;
            this.logger = logger;
            this.cancellationToken = cancellationToken;
            this.protocolHeaders = new ProtocolHeaders(session.Properties.ServerType);
        }

        /// <summary>
        /// The URI of the Trino resource.
        /// </summary>
        protected virtual internal Uri ResourceUri => new Uri($"{Session.Properties.Server}{this.ResourcePath}");

        /// <summary>
        /// Performs HTTP request to Trino to fetch the requested resource and deserializes it to the specified type.
        /// </summary>
        public T Get()
        {
            return this.GetAsync().SafeResult();
        }

        /// <summary>
        /// Performs HTTP request to Trino to fetch the requested resource and deserializes it to the specified type.
        /// </summary>
        protected internal async Task<T> GetAsync()
        {
            return await this.GetAsync(this.ResourceUri).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs HTTP request to Trino to fetch the requested resource and deserializes it to the specified type.
        /// </summary>
        protected internal async Task<T> GetAsync(Uri uri)
        {
            string resourceContent = await GetAsync(uri, defaultExpectedResponseCodes).ConfigureAwait(false);
            T deserializedResult = JsonConvert.DeserializeObject<T>(resourceContent);
            return deserializedResult;
        }

        /// <summary>
        /// Perform actual HTTP request to Trino to fetch the requested resource
        /// </summary>
        protected async Task<string> GetAsync(Uri uri, HashSet<HttpStatusCode> expectedResponses)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                return await GetResourceAsync(
                    httpClient,
                    this.RetryableResponses,
                    this.Session,
                    request,
                    expectedResponses,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Perform actual HTTP request to Trino to fetch the requested resource
        /// </summary>
        protected async Task<string> GetResourceAsync(
            HttpClient httpClient,
            HashSet<HttpStatusCode> retryableResponses,
            ClientSession session,
            HttpRequestMessage request,
            HashSet<HttpStatusCode> expectedResponses,
            CancellationToken token)
        {
            string responseContent = string.Empty;
            AddHeaders(protocolHeaders, request, session);

            // Continually retry until erroring or a valid response
            while (true)
            {
                try
                {
                    HttpStatusCode statusCode;
                    using (HttpResponseMessage page = await httpClient.SendAsync(request, token).ConfigureAwait(false))
                    {
                        responseContent = await page.Content.ReadAsStringAsync().ConfigureAwait(false);
#if TEST_OUTPUT
                        // For UT generation, write the response to a file.
                        // First get the headers from the response and serialize them into k=v pairs
                        StringBuilder headers = new StringBuilder();
                        HashSet<string> excludedHeaders = new HashSet<string>() { "Connection", "X-Content-Type-Options", "Vary", "Strict-Transport-Security" };
                        for (int i = 0; i < page.Headers.Count(); i++)
                        {
                            string headerName = page.Headers.ElementAt(i).Key;
                            if (!excludedHeaders.Contains(headerName))
                            {
                                string headerValue = string.Join(",", page.Headers.ElementAt(i).Value);
                                headers.Append(headerName + "=" + headerValue + "|");
                            }
                        }
                        string responseStrWithUpdatedHost = responseContent.Replace(this.Session.Properties.Server.ToString(), "http://localhost/");
                        string fileName = "response.json";
                        File.AppendAllText(fileName, headers.ToString() + Environment.NewLine);
                        File.AppendAllText(fileName, responseStrWithUpdatedHost.Trim() + Environment.NewLine);
#endif
                        statusCode = page.StatusCode;
                        if (retryableResponses.Contains(statusCode))
                        {
                            continue;
                        }

                        if (!expectedResponses.Contains(statusCode))
                        {
                            throw new TrinoException($"HTTP {(int)statusCode} ({statusCode}): {responseContent}");
                        }

                        this.ProcessResponseHeaders(page.Headers);
                    }

                    return responseContent;
                }
                catch (WebException ex)
                {
                    using (var stream = ex.Response.GetResponseStream())
                    using (var reader = new StreamReader(stream))
                    {
                        string responseStr;
                        responseStr = reader.ReadToEnd();
                        throw new TrinoException(responseStr, ex);
                    }
                }
                catch (Exception ex)
                {
                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        throw new TrinoException(responseContent, ex);
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
        }

        protected virtual void ProcessResponseHeaders(HttpResponseHeaders headers)
        {
        }

        /// <summary>
        /// Adds headers that are common to all requests
        /// </summary>
        protected static internal void AddHeaders(ProtocolHeaders protocolHeaders, HttpRequestMessage request, ClientSession session)
        {
            session.Auth?.AddCredentialToRequest(request);

            if (!string.IsNullOrEmpty(session.Properties.User))
            {
                request.Headers.Add(protocolHeaders.RequestUser, session.Properties.User);
            }
            else if (session.Auth == null)
            {
                // A user is always required, if no user is provided, use the user agent
                request.Headers.Add(protocolHeaders.RequestUser, TrinoClientName);
            }

            request.Headers.Add("User-Agent", TrinoClientName);
        }
    }
}
