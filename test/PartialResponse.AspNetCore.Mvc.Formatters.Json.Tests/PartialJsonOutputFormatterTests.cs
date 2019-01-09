// Copyright (c) Arjen Post. See LICENSE and NOTICE in the project root for license information.

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using PartialResponse.Core;
using Xunit;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Tests
{
    public class PartialJsonOutputFormatterTests
    {
        private readonly PartialJsonOutputFormatter formatter;
        private readonly IFieldsParser fieldsParser = Mock.Of<IFieldsParser>();
        private readonly IOptions<MvcPartialJsonOptions> options = Mock.Of<IOptions<MvcPartialJsonOptions>>();
        private readonly MvcPartialJsonOptions partialJsonOptions = new MvcPartialJsonOptions();
        private readonly HttpContext httpContext = Mock.Of<HttpContext>();
        private readonly Dictionary<object, object> httpContextItems = new Dictionary<object, object>();
        private readonly HttpRequest httpRequest = Mock.Of<HttpRequest>();
        private readonly HttpResponse httpResponse = Mock.Of<HttpResponse>();
        private readonly StringBuilder body = new StringBuilder();

        public PartialJsonOutputFormatterTests()
        {
            Mock.Get(this.httpContext)
                .SetupGet(httpContext => httpContext.Request)
                .Returns(this.httpRequest);

            Mock.Get(this.httpContext)
                .SetupGet(httpContext => httpContext.Response)
                .Returns(this.httpResponse);

            Mock.Get(this.httpContext)
                .SetupGet(httpContext => httpContext.Items)
                .Returns(this.httpContextItems);

            Mock.Get(this.httpRequest)
                .SetupGet(httpRequest => httpRequest.HttpContext)
                .Returns(this.httpContext);

            Mock.Get(this.options)
                .SetupGet(options => options.Value)
                .Returns(this.partialJsonOptions);

            this.formatter = new PartialJsonOutputFormatter(new JsonSerializerSettings(), this.fieldsParser, Mock.Of<ArrayPool<char>>(), this.partialJsonOptions);
        }

        [Fact]
        public async Task TheWriteResponseBodyAsyncMethodShouldReturnStatusCode400IfFieldsMalformed()
        {
            // Arrange
            Mock.Get(this.httpResponse)
                .SetupGet(httpResponse => httpResponse.StatusCode)
                .Returns(200);

            Mock.Get(this.fieldsParser)
                .Setup(fieldsParser => fieldsParser.Parse(this.httpRequest))
                .Returns(FieldsParserResult.Failure());

            this.partialJsonOptions.IgnoreParseErrors = false;
            this.partialJsonOptions.IgnoreCase = false;

            var writeContext = this.CreateWriteContext(new { });

            // Act
            await this.formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

            // Assert
            Mock.Get(this.httpResponse)
                .VerifySet(httpResponse => httpResponse.StatusCode = 400);

            Assert.Equal(0, this.body.Length);
        }

        [Fact]
        public async Task TheWriteResponseBodyAsyncMethodShouldNotReturnStatusCode400IfFieldsMalformedButParseErrorsIgnored()
        {
            // Arrange
            Mock.Get(this.httpResponse)
                .SetupGet(httpResponse => httpResponse.StatusCode)
                .Returns(200);

            Mock.Get(this.fieldsParser)
                .Setup(fieldsParser => fieldsParser.Parse(this.httpRequest))
                .Returns(FieldsParserResult.Failure());

            this.partialJsonOptions.IgnoreParseErrors = true;
            this.partialJsonOptions.IgnoreCase = false;

            var writeContext = this.CreateWriteContext(new { foo = "bar" });

            // Act
            await this.formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

            // Assert
            Mock.Get(this.httpResponse)
                .VerifySet(httpResponse => httpResponse.StatusCode = 400, Times.Never);

            Assert.Equal("{\"foo\":\"bar\"}", this.body.ToString());
        }

        [Fact]
        public async Task TheWriteResponseBodyAsyncMethodShouldNotApplyFieldsIfNotSupplied()
        {
            // Arrange
            Mock.Get(this.httpResponse)
                .SetupGet(httpResponse => httpResponse.StatusCode)
                .Returns(200);

            Mock.Get(this.fieldsParser)
                .Setup(fieldsParser => fieldsParser.Parse(this.httpRequest))
                .Returns(FieldsParserResult.NoValue());

            var writeContext = this.CreateWriteContext(new { foo = "bar" });

            // Act
            await this.formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

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

            Fields.TryParse("foo", out var fields);

            Mock.Get(this.fieldsParser)
                .Setup(fieldsParser => fieldsParser.Parse(this.httpRequest))
                .Returns(FieldsParserResult.Success(fields));

            var writeContext = this.CreateWriteContext(new { foo = "bar", baz = "qux" });

            // Act
            await this.formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

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

            Fields.TryParse("FOO", out var fields);

            Mock.Get(this.fieldsParser)
                .Setup(fieldsParser => fieldsParser.Parse(this.httpRequest))
                .Returns(FieldsParserResult.Success(fields));

            this.partialJsonOptions.IgnoreCase = true;

            var writeContext = this.CreateWriteContext(new { foo = "bar" });

            // Act
            await this.formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

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

            Fields.TryParse("FOO", out var fields);

            Mock.Get(this.fieldsParser)
                .Setup(fieldsParser => fieldsParser.Parse(this.httpRequest))
                .Returns(FieldsParserResult.Success(fields));

            this.partialJsonOptions.IgnoreCase = false;

            var writeContext = this.CreateWriteContext(new { foo = "bar" });

            // Act
            await this.formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

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

            Fields.TryParse(string.Empty, out var fields);

            Mock.Get(this.fieldsParser)
                .Setup(fieldsParser => fieldsParser.Parse(this.httpRequest))
                .Returns(FieldsParserResult.Success(fields));

            this.httpRequest.BypassPartialResponse();

            var writeContext = this.CreateWriteContext(new { foo = "bar" });

            // Act
            await this.formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

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

            Fields.TryParse(string.Empty, out var fields);

            Mock.Get(this.fieldsParser)
                .Setup(fieldsParser => fieldsParser.Parse(this.httpRequest))
                .Returns(FieldsParserResult.Success(fields));

            var writeContext = this.CreateWriteContext(new { foo = "bar" });

            // Act
            await this.formatter.WriteResponseBodyAsync(writeContext, Encoding.UTF8);

            // Assert
            Assert.Equal("{\"foo\":\"bar\"}", this.body.ToString());
        }

        private OutputFormatterWriteContext CreateWriteContext(object value)
        {
            return new OutputFormatterWriteContext(this.httpContext, (stream, encoding) => new StringWriter(this.body), typeof(object), value);
        }
    }
}