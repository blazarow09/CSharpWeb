using SIS.HTTP.Headers;
using SIS.HTTP.Headers.Contracts;
using System.Net;

namespace SIS.HTTP.Responses.Contracts
{
    public interface IHttpResponse
    {
        HttpStatusCode StatusCode { get; set; }

        IHttpHeaderCollection Headers { get; }

        byte[] Content { get; set; }

        void AddHeader(HttpHeader header);

        byte[] GetBytes();
    }
}