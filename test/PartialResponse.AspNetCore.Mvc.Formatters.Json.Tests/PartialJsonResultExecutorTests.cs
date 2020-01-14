// Copyright (c) Arjen Post and contributors. See LICENSE and NOTICE in the project root for license information.

using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using PartialResponse.AspNetCore.Mvc.Formatters.Json.Internal;
using PartialResponse.Core;
using Xunit;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Tests
{
    public class PartialJsonResultExecutorTests
    {
        private readonly PartialJsonResultExecutor executor;
        private readonly ActionContext actionContext;
        private readonly IFieldsParser fieldsParser = Mock.Of<IFieldsParser>();
        private readonly IHttpResponseStreamWriterFactory writerFactory = Mock.Of<IHttpResponseStreamWriterFactory>();
        private readonly ILogger<PartialJsonResultExecutor> logger = Mock.Of<ILogger<PartialJsonResultExecutor>>();
        private readonly IOptions<MvcPartialJsonOptions> optionsMvcPartialJsonOptions = Mock.Of<IOptions<MvcPartialJsonOptions>>();
        private readonly MvcPartialJsonOptions mvcPartialJsonOptions = new MvcPartialJsonOptions();
        private readonly IOptions<MvcOptions> optionsMvcOptions = Mock.Of<IOptions<MvcOptions>>();
        private readonly MvcOptions mvcOptions = new MvcOptions();
        private readonly HttpContext httpContext = Mock.Of<HttpContext>();
        private readonly HttpRequest httpRequest = Mock.Of<HttpRequest>();
        private readonly HttpResponse httpResponse = Mock.Of<HttpResponse>();
        private readonly StringBuilder body = new StringBuilder();

        public PartialJsonResultExecutorTests()
        {
            Mock.Get(this.httpContext)
                .SetupGet(httpContext => httpContext.Request)
                .Returns(this.httpRequest);

            Mock.Get(this.httpContext)
                .SetupGet(httpContext => httpContext.Response)
                .Returns(this.httpResponse);

            Mock.Get(this.writerFactory)
                .Setup(writerFactory => writerFactory.CreateWriter(It.IsAny<Stream>(), It.IsAny<Encoding>()))
                .Returns(new StringWriter(this.body));

            Mock.Get(this.optionsMvcPartialJsonOptions)
                .SetupGet(options => options.Value)
                .Returns(this.mvcPartialJsonOptions);

            Mock.Get(this.optionsMvcOptions)
                .SetupGet(optionsMvcOptions => optionsMvcOptions.Value)
                .Returns(this.mvcOptions);

#if ASPNETCORE2
            this.executor = new PartialJsonResultExecutor(this.writerFactory, this.logger, this.optionsMvcPartialJsonOptions, this.fieldsParser, Mock.Of<ArrayPool<char>>());
#else
            this.executor = new PartialJsonResultExecutor(this.writerFactory, this.logger, this.optionsMvcOptions, this.optionsMvcPartialJsonOptions, this.fieldsParser, Mock.Of<ArrayPool<char>>());
#endif
            this.actionContext = new ActionContext() { HttpContext = this.httpContext };
        }

        [Fact]
        public async Task TheExecuteAsyncMethodShouldReturnStatusCode400IfFieldsMalformed()
        {
            // Arrange
            Mock.Get(this.fieldsParser)
                .Setup(fieldsParser => fieldsParser.Parse(this.httpRequest))
                .Returns(FieldsParserResult.Failed());

            var partialJsonResult = new PartialJsonResult(new { });

            // Act
            await this.executor.ExecuteAsync(this.actionContext, partialJsonResult);

            // Assert
            Mock.Get(this.httpResponse)
                .VerifySet(httpResponse => httpResponse.StatusCode = 400);

            Assert.Equal(0, this.body.Length);
        }

        [Fact]
        public async Task TheExecuteAsyncMethodShouldNotReturnStatusCode400IfFieldsMalformedButParseErrorsIgnored()
        {
            // Arrange
            Mock.Get(this.fieldsParser)
                .Setup(fieldsParser => fieldsParser.Parse(this.httpRequest))
                .Returns(FieldsParserResult.Failed());

            var partialJsonResult = new PartialJsonResult(new { foo = "bar" });

            this.mvcPartialJsonOptions.IgnoreParseErrors = true;

            // Act
            await this.executor.ExecuteAsync(this.actionContext, partialJsonResult);

            // Assert
            Mock.Get(this.httpResponse)
                .VerifySet(httpResponse => httpResponse.StatusCode = 400, Times.Never);

            Assert.Equal("{\"foo\":\"bar\"}", this.body.ToString());
        }

        [Fact]
        public async Task TheExecuteAsyncMethodShouldNotApplyFieldsIfNotSupplied()
        {
            // Arrange
            Mock.Get(this.fieldsParser)
                .Setup(fieldsParser => fieldsParser.Parse(this.httpRequest))
                .Returns(FieldsParserResult.NoValue());

            var partialJsonResult = new PartialJsonResult(new { foo = "bar" }, new JsonSerializerSettings());

            // Act
            await this.executor.ExecuteAsync(this.actionContext, partialJsonResult);

            // Assert
            Assert.Equal("{\"foo\":\"bar\"}", this.body.ToString());
        }

        [Fact]
        public async Task TheExecuteAsyncMethodShouldApplyFieldsIfSupplied()
        {
            // Arrange
            Fields.TryParse("foo", out var fields);

            Mock.Get(this.fieldsParser)
                .Setup(fieldsParser => fieldsParser.Parse(this.httpRequest))
                .Returns(FieldsParserResult.Success(fields));

            var partialJsonResult = new PartialJsonResult(new { foo = "bar", baz = "qux" }, new JsonSerializerSettings());

            // Act
            await this.executor.ExecuteAsync(this.actionContext, partialJsonResult);

            // Assert
            Assert.Equal("{\"foo\":\"bar\"}", this.body.ToString());
        }

        [Fact]
        public async Task TheExecuteAsyncMethodShouldIgnoreCase()
        {
            // Arrange
            Fields.TryParse("FOO", out var fields);

            Mock.Get(this.fieldsParser)
                .Setup(fieldsParser => fieldsParser.Parse(this.httpRequest))
                .Returns(FieldsParserResult.Success(fields));

            this.mvcPartialJsonOptions.IgnoreCase = true;

            var partialJsonResult = new PartialJsonResult(new { foo = "bar", baz = "qux" }, new JsonSerializerSettings());

            // Act
            await this.executor.ExecuteAsync(this.actionContext, partialJsonResult);

            // Assert
            Assert.Equal("{\"foo\":\"bar\"}", this.body.ToString());
        }

        [Fact]
        public async Task TheExecuteAsyncMethodShouldNotIgnoreCase()
        {
            // Arrange
            Fields.TryParse("FOO", out var fields);

            Mock.Get(this.fieldsParser)
                .Setup(fieldsParser => fieldsParser.Parse(this.httpRequest))
                .Returns(FieldsParserResult.Success(fields));

            this.mvcPartialJsonOptions.IgnoreCase = false;

            var partialJsonResult = new PartialJsonResult(new { foo = "bar", baz = "qux" }, new JsonSerializerSettings());

            // Act
            await this.executor.ExecuteAsync(this.actionContext, partialJsonResult);

            // Assert
            Assert.Equal("{}", this.body.ToString());
        }
    }
}