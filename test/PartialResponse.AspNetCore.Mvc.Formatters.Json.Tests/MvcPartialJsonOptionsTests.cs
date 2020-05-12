// Copyright (c) Arjen Post and contributors. See LICENSE and NOTICE in the project root for license information.

using System;
using Xunit;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Tests
{
    public class MvcPartialJsonOptionsTests
    {
        [Fact]
        public void TheIgnoredFieldsPropertyShouldThrowExceptionIfInvalid()
        {
            // Arrange
            var options = new MvcPartialJsonOptions();

            // Assert
            Assert.Throws<ArgumentException>("value", () => options.IgnoredFields = new[] { "/foo" });
        }

        [Fact]
        public void TheIgnoredFieldsPropertyShouldParseIgnoredFields()
        {
            // Arrange
            var options = new MvcPartialJsonOptions();
            var value = new[] { "foo" };

            // Act
            options.IgnoredFields = value;

            // Assert
            Assert.Same(value, options.IgnoredFields);
        }
    }
}
