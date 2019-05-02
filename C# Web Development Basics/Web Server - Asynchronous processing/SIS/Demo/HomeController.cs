using SIS.HTTP.Responses.Contracts;
using SIS.WebServer.Results;
using System.Net;

namespace SIS.Demo
{
    public class HomeController
    {
        public IHttpResponse Index()
        {
            string content = "<h1>Hello, World!</h1>";

            return new HtmlResult(content, HttpStatusCode.OK);
        }
    }
}