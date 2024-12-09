using System.Net;
using Trino.Data.ADO.Server;

namespace Trino.Client.Test
{
    internal class TrinoTestServer : IDisposable
    {
        public int Port { get; private set; }
        private readonly HttpListener listener = new();
        private Task? serverTask;
        private bool cancelled = false;

        private TrinoTestServer()
        {
            // Pick a random port to listen on
            Port = new Random().Next(10000) + 10000;
        }

        public static TrinoTestServer Create(string testFile)
        {
            return Create(testFile, TimeSpan.Zero);
        }

        /// <summary>
        /// Create a server to respond with pre-recorded Trino responses.
        /// </summary>
        /// <param name="waitBetweenResponses">Allows for the simulation of a slow server.</param>
        public static TrinoTestServer Create(string testFile, TimeSpan waitBetweenResponses)
        {
            TrinoTestServer server = new();
            server.StartServer(testFile, waitBetweenResponses);
            return server;
        }

        private void StartServer(string testFile, TimeSpan waitBetweenResponses)
        {
            // Check testFile exists before starting
            if (!File.Exists(testFile))
            {
                throw new FileNotFoundException(testFile);
            }

            this.serverTask = new Task(() =>
            {
                this.ConfigureTest(testFile, waitBetweenResponses);
            });
            this.serverTask.Start();
        }

        /// <summary>
        /// Represents a Trino HTTP response: headers and payload
        /// </summary>
        internal class TestStep
        {
            public Dictionary<string, List<string>> Headers;
            public string Payload { get; set; }

            internal TestStep()
            {
                Headers = [];
                Payload = string.Empty;
            }
        }

        internal void ConfigureTest(string testFile, TimeSpan waitBetweenResponses)
        {
            bool isHeader = true;
            TestStep current = new();
            List<TestStep> testSteps = [];
            try
            {
                foreach (string line in File.ReadAllLines(testFile))
                {
                    if (isHeader)
                    {
                        isHeader = PrepareHeaders(current, line);
                    }
                    else
                    {
                        // replace port in response
                        string portUpdatedResponse = line.Replace("localhost", "localhost:" + this.Port);
                        current.Payload = portUpdatedResponse;
                        testSteps.Add(current);
                        current = new TestStep();
                        isHeader = true;
                    }
                }
            }
            catch (Exception e)
            {
                Assert.Fail(e.Message);
            }

            QueueUpResponses(testSteps, waitBetweenResponses);
        }

        private static bool PrepareHeaders(TestStep current, string line)
        {
            bool isHeader;
            IEnumerable<KeyValuePair<string, string>> headerValues = line.Split('|')
                                        .Where(l => !string.IsNullOrEmpty(l))
                                        .Select(l => new KeyValuePair<string, string>(l[..l.IndexOf('=')], l[(l.IndexOf('=') + 1)..]));
            foreach (KeyValuePair<string, string> header in headerValues)
            {
                if (!current.Headers.TryGetValue(header.Key, out List<string>? value))
                {
                    value = [];
                    current.Headers.Add(header.Key, value);
                }

                value.Add(header.Value);
            }
            isHeader = false;
            return isHeader;
        }

        /// <summary>
        /// Runs local webserver to respond with Trino HTTP responses.
        /// </summary>
        /// <param name="responses"></param>
        private void QueueUpResponses(List<TestStep> responses, TimeSpan waitBetweenResponses)
        {
            Console.WriteLine("Starting test server on port " + this.Port);
            // Add the prefixes.
            listener.Prefixes.Add($"http://localhost:{Port}/v1/");
            listener.Start();
            Console.WriteLine("Listening...");
            // Note: The GetContext method blocks while waiting for a request.
            foreach (TestStep response in responses)
            {
                // Listener.GetContext() blocks while waiting for a request.
                Console.WriteLine("Waiting for requests...");
                Task<HttpListenerContext> contextTask = listener.GetContextAsync();
                while (!contextTask.IsCompleted)
                {
                    if (cancelled)
                    {
                        return;
                    }
                    contextTask.Wait(1000);
                }

                HttpListenerRequest request = contextTask.Result.Request;
                long contentLength = request.ContentLength64;
                if (contentLength > 0)
                {
                    byte[] buffer = new byte[contentLength];
                    request.InputStream.Read(buffer, 0, (int)contentLength);
                    string requestContent = System.Text.Encoding.UTF8.GetString(buffer);
                    Console.WriteLine($"Request recieved with body: {requestContent}");
                }
                else
                {
                    Console.WriteLine("Request recieved.");
                }
                // Obtain a response object.
                using (HttpListenerResponse httpListenerResponse = contextTask.Result.Response)
                {
                    // Construct a response.
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(response.Payload);

                    // add headers
                    foreach (KeyValuePair<string, List<string>> header in response.Headers)
                    {
                        foreach (string value in header.Value)
                        {
                            httpListenerResponse.Headers.Add(header.Key, value);
                        }
                    }

                    Console.WriteLine("Starting response.");
                    // Get a response stream and write the response to it.
                    httpListenerResponse.ContentLength64 = buffer.Length;
                    using (Stream output = httpListenerResponse.OutputStream)
                    {
                        output.Write(buffer, 0, buffer.Length);
                    }
                    Console.WriteLine("Written response.");
                    if (waitBetweenResponses.Ticks > 0)
                    {
                        Thread.Sleep(waitBetweenResponses);
                    }
                }
            }
        }

        internal TrinoConnectionProperties GetConnectionProperties()
        {
            TrinoConnectionProperties properties = new()
            {
                Catalog = "tpch",
                Host = "localhost",
                Port = this.Port,
                EnableSsl = false
            };
            return properties;
        }

        public void Dispose()
        {
            if (this.serverTask != null)
            {
                this.cancelled = true;
                this.serverTask.Wait();
            }
        }
    }
}
