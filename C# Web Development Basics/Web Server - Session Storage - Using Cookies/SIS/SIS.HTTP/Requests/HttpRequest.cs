﻿namespace SIS.HTTP.Requests
{
    using SIS.HTTP.Common;
    using SIS.HTTP.Cookies;
    using SIS.HTTP.Cookies.Contracts;
    using SIS.HTTP.Enums;
    using SIS.HTTP.Exceptions;
    using SIS.HTTP.Extensions;
    using SIS.HTTP.Headers;
    using SIS.HTTP.Headers.Contracts;
    using SIS.HTTP.Requests.Contracts;
    using SIS.HTTP.Sessions.Contracts;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    public class HttpRequest : IHttpRequest
    {
        private const char HttpRequestUrlQuerySeparator = '?';

        private const char HttpRequestUrlFragmentSeparator = '#';

        private const string HttpRequestHeaderNameValueSeparator = ": ";

        private const string HttpRequestCookiesSeparator = "; ";

        private const char HttpRequestCookieNameValueSeparator = '=';

        private const char HttpRequestParameterSeparator = '&';

        private const char HttpRequestParameterNameValueSeparator = '=';

        public HttpRequest(string requestString)
        {
            this.FormData = new Dictionary<string, object>();
            this.QueryData = new Dictionary<string, object>();
            this.Headers = new HttpHeaderCollection();
            this.Cookies = new HttpCookieCollection();

            this.ParseRequest(requestString);
        }

        public string Path { get; private set; }

        public string Url { get; private set; }

        public Dictionary<string, object> FormData { get; }

        public Dictionary<string, object> QueryData { get; }

        public IHttpHeaderCollection Headers { get; }

        public IHttpCookieCollection Cookies { get; }

        public HttpRequestMethod RequestMethod { get; private set; }

        public IHttpSession Session { get; set; }

        private void ParseRequest(string requestString)
        {
            var splitRequestContent = requestString.Split(GlobalConstants.HttpNewLine, StringSplitOptions.None);

            var requestLine = splitRequestContent[0].Trim()
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            if (!this.IsValidRequestLine(requestLine))
            {
                throw new BadRequestException();
            }

            this.ParseRequestMethod(requestLine);
            this.ParseRequestUrl(requestLine);
            this.ParseRequestPath();

            this.ParseHeaders(splitRequestContent.Skip(1).ToArray());
            this.ParseCookies();

            this.ParseRequestParameters(splitRequestContent[splitRequestContent.Length - 1]);
        }

        private void ParseCookies()
        {
            if (!this.Headers.ContainsHeader(HttpHeader.Cookie))
            {
                return;
            }

            string cookiesString = this.Headers.GetHeader(HttpHeader.Cookie).Value;

            if (string.IsNullOrEmpty(cookiesString))
            {
                return;
            }

            var splitCookies = cookiesString.Split(HttpRequestCookiesSeparator);

            foreach (var splitCookie in splitCookies)
            {
                var cookieParts = splitCookie.Split(HttpRequestCookieNameValueSeparator, 2, StringSplitOptions.RemoveEmptyEntries);

                if (cookieParts.Length != 2)
                {
                    continue;
                }

                var key = cookieParts[0];
                var value = cookieParts[1];

                this.Cookies.Add(new HttpCookie(key, value, false));
            }
        }

        private void ParseRequestMethod(string[] requestLine)
        {
            var parseResult = Enum.TryParse<HttpRequestMethod>(requestLine[0].Capitalize(), out HttpRequestMethod parsedRequestMethod);

            if (!parseResult)
            {
                throw new BadRequestException();
            }

            this.RequestMethod = parsedRequestMethod;
        }

        private void ParseRequestUrl(string[] requestLine)
        {
            this.Url = requestLine[1];
        }

        private void ParseRequestPath()
        {
            this.Path = this.Url.Split(new[] { '?', '#' }, StringSplitOptions.RemoveEmptyEntries)[0];
        }

        private void ParseHeaders(string[] requestHeaders)
        {
            int currentIndex = 0;

            while (!string.IsNullOrEmpty(requestHeaders[currentIndex]))
            {
                string[] headerArguments = requestHeaders[currentIndex++].Split(": ");

                this.Headers.Add(new HttpHeader(headerArguments[0], headerArguments[1]));
            }

            if (!this.Headers.ContainsHeader(GlobalConstants.HostHeaderKey))
            {
                throw new BadRequestException();
            }
        }

        private void ParseRequestParameters(string bodyParameters)
        {
            this.ParseQueryParameters();
            this.ParseFormDataParameters(bodyParameters);
        }

        private void ParseFormDataParameters(string formData)
        {
            if (string.IsNullOrEmpty(formData))
            {
                return;
            }

            string[] formDataParams = formData.Split("&");

            foreach (var formDataParameter in formDataParams)
            {
                string[] parameterArguments = formDataParameter
                    .Split("=", StringSplitOptions.None);

                if (this.FormData.ContainsKey(parameterArguments[0]))
                {
                    if (this.FormData[parameterArguments[0]] is string ||
                        !(this.FormData[parameterArguments[0]] is List<string>))
                    {
                        List<string> collection = new List<string> { this.FormData[parameterArguments[0]].ToString() };
                        this.FormData[parameterArguments[0]] = collection;
                    }

                    ((List<string>)this.FormData[parameterArguments[0]]).Add(HttpUtility.UrlDecode(parameterArguments[1]));
                }
                else
                {
                    this.FormData.Add(parameterArguments[0], HttpUtility.UrlDecode(parameterArguments[1]));
                }
            }
        }

        private void ParseQueryParameters()
        {
            if (!this.Url.Contains('?'))
            {
                return;
            }

            string queryString = this.Url
                .Split(new[] { '?', '#' }, StringSplitOptions.None)[1];

            if (string.IsNullOrWhiteSpace(queryString))
            {
                return;
            }

            string[] queryParameters = queryString.Split('&');

            if (!this.IsValidRequestQueryString(queryString, queryParameters))
            {
                throw new BadRequestException();
            }

            foreach (var queryParameter in queryParameters)
            {
                string[] parameterArguments = queryParameter
                    .Split('=', StringSplitOptions.None);

                this.QueryData.Add(parameterArguments[0], parameterArguments[1]);
            }
        }

        private bool IsValidRequestQueryString(string queryString, string[] queryParameters)
        {
            return !(string.IsNullOrEmpty(queryString) || queryParameters.Length < 1);
        }

        private bool IsValidRequestLine(string[] requestLine)
        {
            return requestLine.Length == 3
                   && requestLine[2].ToLower()
                   != GlobalConstants.HttpOneProtocolFragment;
        }
    }
}