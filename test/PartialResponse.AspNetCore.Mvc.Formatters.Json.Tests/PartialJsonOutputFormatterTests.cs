// Copyright (c) Arjen Post. See LICENSE and NOTICE in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using PartialResponse.Extensions.DependencyInjection;
using Xunit;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Tests
{
    public class PartialJsonOutputFormatterTests
    {
        private readonly MvcPartialJsonFields mvcPartialJsonFields;
        private readonly IOptions<MvcPartialJsonOptions> options = Mock.Of<IOptions<MvcPartialJsonOptions>>();
        private readonly MvcPartialJsonOptions partialJsonOptions = new MvcPartialJsonOptions();
        private readonly ILogger<MvcPartialJsonFields> loggerMvcPartialJsonFields = Mock.Of<ILogger<MvcPartialJsonFields>>();
        private readonly HttpContext httpContext = Mock.Of<HttpContext>();
        private readonly Dictionary<object, object> httpContextItems = new Dictionary<object, object>();
        private readonly HttpRequest httpRequest = Mock.Of<HttpRequest>();
        private readonly HttpResponse httpResponse = Mock.Of<HttpResponse>();
        private readonly IQueryCollection queryCollection = Mock.Of<IQueryCollection>();
        private readonly StringBuilder body = new StringBuilder();
        private readonly IServiceProvider serviceProvider = Mock.Of<IServiceProvider>();
        private readonly IHttpContextAccessor httpContextAccessor = Mock.Of<IHttpContextAccessor>();

        public PartialJsonOutputFormatterTests()
        {
            Mock.Get(this.httpRequest)
                .SetupGet(httpRequest => httpRequest.Query)
                .Returns(this.queryCollection);

            Mock.Get(this.httpContext)
                .SetupGet(httpContext => httpContext.Request)
                .Returns(this.httpRequest);

            Mock.Get(this.httpContext)
                .SetupGet(httpContext => httpContext.Response)
                .Returns(this.httpResponse);

            Mock.Get(this.httpContext)
                .SetupGet(httpContext => httpContext.Items)
                .Returns(this.httpContextItems);

            Mock.Get(this.httpContext)
                .SetupGet(httpContext => httpContext.RequestServices)
                .Returns(this.serviceProvider);

            Mock.Get(this.httpRequest)
                .SetupGet(httpRequest => httpRequest.HttpContext)
                .Returns(this.httpContext);

            Mock.Get(this.options)
                .SetupGet(options => options.Value)
                .Returns(this.partialJsonOptions);

            Mock.Get(this.serviceProvider)
                .Setup(provider => provider.GetService(typeof(IMvcPartialJsonFields)))
                .Returns(() => this.mvcPartialJsonFields);

            Mock.Get(this.serviceProvider)
                .Setup(provider => provider.GetService(typeof(IOptions<MvcPartialJsonOptions>)))
                .Returns(() => this.options);

            Mock.Get(this.httpContextAccessor)
                .SetupGet(httpContextAccessor => httpContextAccessor.HttpContext)
                .Returns(this.httpContext);

            this.mvcPartialJsonFields = new MvcPartialJsonFields(this.httpContextAccessor, this.loggerMvcPartialJsonFields, this.options);
        }

        [Fact]
        public async Task TheWriteResponseBodyAsyncMethodShouldReturnStatusCode400IfFieldsMalformed()
        {
            // Arrange
            Mock.Get(this.httpResponse)
                .SetupGet(httpResponse => httpResponse.StatusCode)
                .Returns(200);

            Mock.Get(this.queryCollection)
                .Setup(queryCollection => queryCollection.ContainsKey("fields"))
                .Returns(true);

            Mock.Get(this.queryCollection)
                .SetupGet(queryCollection => queryCollection["fields"])
                .Returns("foo/");

            this.partialJsonOptions.IgnoreParseErrors = false;
            this.partialJsonOptions.IgnoreCase = false;

            var writeContext = new OutputFormatterWriteContext(this.httpContext, (stream, encoding) => new StringWriter(this.body), typeof(object), new { });
            var formatter = new PartialJsonOutputFormatter(new JsonSerializerSettings(), Mock.Of<ArrayPool<char>>(), this.partialJsonOptions);

            // Act
            await formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

            // Assert
            Mock.Get(this.httpResponse)
                .VerifySet(httpResponse => httpResponse.StatusCode = 400);
        }

        [Fact]
        public async Task TheWriteResponseBodyAsyncMethodShouldReturnStatusCode400IfFieldsMalformedUnlessIgnored()
        {
            // Arrange
            Mock.Get(this.httpResponse)
                .SetupGet(httpResponse => httpResponse.StatusCode)
                .Returns(200);

            Mock.Get(this.queryCollection)
                .Setup(queryCollection => queryCollection.ContainsKey("fields"))
                .Returns(true);

            Mock.Get(this.queryCollection)
                .SetupGet(queryCollection => queryCollection["fields"])
                .Returns("foo/");

            this.partialJsonOptions.IgnoreParseErrors = true;
            this.partialJsonOptions.IgnoreCase = false;

            var writeContext = new OutputFormatterWriteContext(this.httpContext, (stream, encoding) => new StringWriter(this.body), typeof(object), new { });
            var formatter = new PartialJsonOutputFormatter(new JsonSerializerSettings(), Mock.Of<ArrayPool<char>>(), this.partialJsonOptions);

            // Act
            await formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

            // Assert
            Assert.Equal("{}", this.body.ToString());
        }

        [Fact]
        public async Task TheWriteResponseBodyAsyncMethodShouldNotWriteBodyIfFieldsMalformed()
        {
            // Arrange
            Mock.Get(this.httpResponse)
                .SetupGet(httpResponse => httpResponse.StatusCode)
                .Returns(200);

            Mock.Get(this.queryCollection)
                .Setup(queryCollection => queryCollection.ContainsKey("fields"))
                .Returns(true);

            Mock.Get(this.queryCollection)
                .SetupGet(queryCollection => queryCollection["fields"])
                .Returns("foo/");

            this.partialJsonOptions.IgnoreCase = false;

            var writeContext = new OutputFormatterWriteContext(this.httpContext, (stream, encoding) => new StringWriter(this.body), typeof(object), new { });
            var formatter = new PartialJsonOutputFormatter(new JsonSerializerSettings(), Mock.Of<ArrayPool<char>>(), this.partialJsonOptions);

            // Act
            await formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

            // Assert
            Assert.Equal(0, this.body.Length);
        }

        [Fact]
        public async Task TheWriteResponseBodyAsyncMethodShouldNotApplyFieldsIfNotSupplied()
        {
            // Arrange
            Mock.Get(this.httpResponse)
                .SetupGet(httpResponse => httpResponse.StatusCode)
                .Returns(200);

            Mock.Get(this.queryCollection)
                .Setup(queryCollection => queryCollection.ContainsKey("fields"))
                .Returns(false);

            this.partialJsonOptions.IgnoreCase = false;

            var value = new { foo = "bar" };

            var writeContext = new OutputFormatterWriteContext(this.httpContext, (stream, encoding) => new StringWriter(this.body), typeof(object), value);
            var formatter = new PartialJsonOutputFormatter(new JsonSerializerSettings(), Mock.Of<ArrayPool<char>>(), this.partialJsonOptions);

            // Act
            await formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

            // Assert
            Assert.Equal("{\"foo\":\"bar\"}", this.body.ToString());
        }

        [Fact]
        public async Task TheWriteResponseBodyAsyncMethodShouldApplyFieldsIfSupplied()
        {
            // Arrange
            Mock.Get(this.httpResponse)
                .SetupGet(httpResponse => httpResponse.StatusCode)
                .Returns(200);

            Mock.Get(this.queryCollection)
                .Setup(queryCollection => queryCollection.ContainsKey("fields"))
                .Returns(true);

            Mock.Get(this.queryCollection)
                .SetupGet(queryCollection => queryCollection["fields"])
                .Returns("foo");

            this.partialJsonOptions.IgnoreCase = false;

            var value = new { foo = "bar", baz = "qux" };

            var writeContext = new OutputFormatterWriteContext(this.httpContext, (stream, encoding) => new StringWriter(this.body), typeof(object), value);
            var formatter = new PartialJsonOutputFormatter(new JsonSerializerSettings(), Mock.Of<ArrayPool<char>>(), this.partialJsonOptions);

            // Act
            await formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

            // Assert
            Assert.Equal("{\"foo\":\"bar\"}", this.body.ToString());
        }

        [Fact]
        public async Task TheWriteResponseBodyAsyncMethodShouldIgnoreCase()
        {
            // Arrange
            Mock.Get(this.httpResponse)
                .SetupGet(httpResponse => httpResponse.StatusCode)
                .Returns(200);

            Mock.Get(this.queryCollection)
                .Setup(queryCollection => queryCollection.ContainsKey("fields"))
                .Returns(true);

            Mock.Get(this.queryCollection)
                .SetupGet(queryCollection => queryCollection["fields"])
                .Returns("FOO");

            this.partialJsonOptions.IgnoreCase = true;

            var value = new { foo = "bar" };

            var writeContext = new OutputFormatterWriteContext(this.httpContext, (stream, encoding) => new StringWriter(this.body), typeof(object), value);
            var formatter = new PartialJsonOutputFormatter(new JsonSerializerSettings(), Mock.Of<ArrayPool<char>>(), this.partialJsonOptions);

            // Act
            await formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

            // Assert
            Assert.Equal("{\"foo\":\"bar\"}", this.body.ToString());
        }

        [Fact]
        public async Task TheWriteResponseBodyAsyncMethodShouldNotIgnoreCase()
        {
            // Arrange
            Mock.Get(this.httpResponse)
                .SetupGet(httpResponse => httpResponse.StatusCode)
                .Returns(200);

            Mock.Get(this.queryCollection)
                .Setup(queryCollection => queryCollection.ContainsKey("fields"))
                .Returns(true);

            Mock.Get(this.queryCollection)
                .SetupGet(queryCollection => queryCollection["fields"])
                .Returns("FOO");

            this.partialJsonOptions.IgnoreCase = false;

            var value = new { foo = "bar" };

            var writeContext = new OutputFormatterWriteContext(this.httpContext, (stream, encoding) => new StringWriter(this.body), typeof(object), value);
            var formatter = new PartialJsonOutputFormatter(new JsonSerializerSettings(), Mock.Of<ArrayPool<char>>(), this.partialJsonOptions);

            // Act
            await formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

            // Assert
            Assert.Equal("{}", this.body.ToString());
        }

        [Fact]
        public async Task TheWriteResponseBodyAsyncMethodShouldBypassPartialResponseIfConfigured()
        {
            // Arrange
            Mock.Get(this.httpResponse)
                .SetupGet(httpResponse => httpResponse.StatusCode)
                .Returns(200);

            Mock.Get(this.queryCollection)
                .Setup(queryCollection => queryCollection.ContainsKey("fields"))
                .Returns(true);

            Mock.Get(this.queryCollection)
                .SetupGet(queryCollection => queryCollection["fields"])
                .Returns(string.Empty);

            this.httpRequest.BypassPartialResponse();
            this.partialJsonOptions.IgnoreCase = false;

            var value = new { foo = "bar" };

            var writeContext = new OutputFormatterWriteContext(this.httpContext, (stream, encoding) => new StringWriter(this.body), typeof(object), value);
            var formatter = new PartialJsonOutputFormatter(new JsonSerializerSettings(), Mock.Of<ArrayPool<char>>(), this.partialJsonOptions);

            // Act
            await formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

            // Assert
            Assert.Equal("{\"foo\":\"bar\"}", this.body.ToString());
        }

        [Fact]
        public async Task TheWriteResponseBodyAsyncMethodShouldBypassPartialResponseIfStatusCodeIsNot200()
        {
            // Arrange
            Mock.Get(this.httpResponse)
                .SetupGet(httpResponse => httpResponse.StatusCode)
                .Returns(500);

            Mock.Get(this.queryCollection)
                .Setup(queryCollection => queryCollection.ContainsKey("fields"))
                .Returns(true);

            Mock.Get(this.queryCollection)
                .SetupGet(queryCollection => queryCollection["fields"])
                .Returns(string.Empty);

            this.partialJsonOptions.IgnoreCase = false;

            var value = new { foo = "bar" };

            var writeContext = new OutputFormatterWriteContext(this.httpContext, (stream, encoding) => new StringWriter(this.body), typeof(object), value);
            var formatter = new PartialJsonOutputFormatter(new JsonSerializerSettings(), Mock.Of<ArrayPool<char>>(), this.partialJsonOptions);

            // Act
            await formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

            // Assert
            Assert.Equal("{\"foo\":\"bar\"}", this.body.ToString());
        }
    }
}