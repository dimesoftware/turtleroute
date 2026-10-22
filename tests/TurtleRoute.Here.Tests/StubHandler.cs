using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TurtleRoute.Here.Tests
{
    /// <summary>
    /// Answers each request with canned JSON chosen by the request path, and records the requests and their bodies.
    /// </summary>
    internal class StubHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, (HttpStatusCode Status, string Body)> _responses = [];

        public List<HttpRequestMessage> Requests { get; } = [];

        public List<string> RequestBodies { get; } = [];

        public StubHandler On(string pathSuffix, string body, HttpStatusCode status = HttpStatusCode.OK)
        {
            _responses[pathSuffix] = (status, body);
            return this;
        }

        public HttpRequestMessage Last(string pathSuffix) => Requests.FindLast(r => r.RequestUri.AbsolutePath.EndsWith(pathSuffix, StringComparison.Ordinal));

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            RequestBodies.Add(request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken));

            foreach (KeyValuePair<string, (HttpStatusCode Status, string Body)> response in _responses)
            {
                if (request.RequestUri.AbsolutePath.EndsWith(response.Key, StringComparison.Ordinal))
                    return new HttpResponseMessage(response.Value.Status) { Content = new StringContent(response.Value.Body, Encoding.UTF8, "application/json") };
            }

            throw new InvalidOperationException($"No stubbed response for {request.RequestUri}.");
        }
    }
}
