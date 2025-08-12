using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ClubDoorman.Tests.TestKit2.Fakes;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Dictionary<string, HttpResponseMessage> _responses = new();
    private readonly Dictionary<string, Exception> _exceptions = new();
    
    public void SetupResponse(string url, HttpResponseMessage response)
    {
        _responses[url] = response;
    }
    
    public void SetupException(string url, Exception exception)
    {
        _exceptions[url] = exception;
    }
    
    public void SetupDefaultResponse(HttpStatusCode statusCode = HttpStatusCode.OK, string content = "{}")
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content)
        };
        _responses["*"] = response;
    }
    
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri?.ToString() ?? "";
        
        if (_exceptions.ContainsKey(url))
            throw _exceptions[url];
            
        if (_responses.ContainsKey(url))
            return Task.FromResult(_responses[url]);
            
        if (_responses.ContainsKey("*"))
            return Task.FromResult(_responses["*"]);
            
        // Default response
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}")
        });
    }
    
    public void Reset()
    {
        _responses.Clear();
        _exceptions.Clear();
    }
}
