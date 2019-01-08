// Copyright (c) Arjen Post. See LICENSE and NOTICE in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Tests
{
    public class FieldsParserTests
    {
        private readonly FieldsParser fieldsParser = new FieldsParser();
        private readonly HttpRequest request = Mock.Of<HttpRequest>();
        private readonly HttpContext context = new DefaultHttpContext();
        private readonly IQueryCollection queryCollection = Mock.Of<IQueryCollection>();

        public FieldsParserTests()
        {
            Mock.Get(this.request)
                .SetupGet(request => request.HttpContext)
                .Returns(this.context);

            Mock.Get(this.request)
                .SetupGet(request => request.Query)
                .Returns(this.queryCollection);
        }

        [Fact]
        public void TheParseMethodShouldThrowIfRequestIsNull()
        {
            // Assert
            Assert.Throws<ArgumentNullException>("request", () => this.fieldsParser.Parse(null));
        }

        [Fact]
        public void TheParseMethodShouldReturnEmptyFieldsResult()
        {
            // Arrange
            Mock.Get(this.queryCollection)
                .Setup(queryCollection => queryCollection.ContainsKey("fields"))
                .Returns(false);

            // Act
            var result = this.fieldsParser.Parse(this.request);

            // Assert
            Assert.False(result.IsFieldsSet);
            Assert.False(result.HasError);
            Assert.Empty(result.Fields.Values);
        }

        [Fact]
        public void TheParseMethodShouldReturnErrorFieldsResult()
        {
            // Arrange
            Mock.Get(this.queryCollection)
                .Setup(queryCollection => queryCollection.ContainsKey("fields"))
                .Returns(true);

            Mock.Get(this.queryCollection)
                .SetupGet(queryCollection => queryCollection["fields"])
                .Returns("foo/");

            // Act
            var result = this.fieldsParser.Parse(this.request);

            // Assert
            Assert.False(result.IsFieldsSet);
            Assert.True(result.HasError);
            Assert.Empty(result.Fields.Values);
        }

        [Fact]
        public void TheParseMethodShouldReturnFields()
        {
            // Arrange
            Mock.Get(this.queryCollection)
                .Setup(queryCollection => queryCollection.ContainsKey("fields"))
                .Returns(true);

            Mock.Get(this.queryCollection)
                .SetupGet(queryCollection => queryCollection["fields"])
                .Returns("foo");

            // Act
            var result = this.fieldsParser.Parse(this.request);

            // Assert
            Assert.True(result.IsFieldsSet);
            Assert.False(result.HasError);
            Assert.NotEmpty(result.Fields.Values);
        }

        [Fact]
        public void TheParseMethodShouldCacheFieldsParserResult()
        {
            // Arrange
            Mock.Get(this.queryCollection)
                .Setup(queryCollection => queryCollection.ContainsKey("fields"))
                .Returns(true);

            Mock.Get(this.queryCollection)
                .SetupGet(queryCollection => queryCollection["fields"])
                .Returns("foo");

            this.fieldsParser.Parse(this.request);

            // Act
            this.fieldsParser.Parse(this.request);

            // Assert
            Mock.Get(this.queryCollection)
                .Verify(queryCollection => queryCollection.ContainsKey("fields"), Times.Once);
        }
    }
}
