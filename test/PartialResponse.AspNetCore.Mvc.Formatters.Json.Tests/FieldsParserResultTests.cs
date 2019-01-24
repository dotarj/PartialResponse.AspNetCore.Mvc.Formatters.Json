// Copyright (c) Arjen Post. See LICENSE and NOTICE in the project root for license information.

using System.Linq;
using PartialResponse.Core;
using Xunit;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Tests
{
    public class FieldsParserResultTests
    {
        [Fact]
        public void TheDefaultConstructorShouldSetIsFieldsSetToFalse()
        {
            // Act
            var result = default(FieldsParserResult);

            // Assert
            Assert.False(result.IsFieldsSet);
        }

        [Fact]
        public void TheDefaultConstructorShouldSetHasErrorToFalse()
        {
            // Act
            var result = default(FieldsParserResult);

            // Assert
            Assert.False(result.HasError);
        }

        [Fact]
        public void TheFailedMethodShouldSetIsFieldsSetToFalse()
        {
            // Act
            var result = FieldsParserResult.Failed();

            // Assert
            Assert.False(result.IsFieldsSet);
        }

        [Fact]
        public void TheFailedMethodShouldSetHasErrorToTrue()
        {
            // Act
            var result = FieldsParserResult.Failed();

            // Assert
            Assert.True(result.HasError);
        }

        [Fact]
        public void TheNoValueMethodShouldSetIsFieldsSetToFalse()
        {
            // Act
            var result = FieldsParserResult.NoValue();

            // Assert
            Assert.False(result.IsFieldsSet);
        }

        [Fact]
        public void TheNoValueMethodShouldSetHasErrorToFalse()
        {
            // Act
            var result = FieldsParserResult.NoValue();

            // Assert
            Assert.False(result.HasError);
        }

        [Fact]
        public void TheSuccessMethodShouldSetIsFieldsSetToTrue()
        {
            // Arrange
            Fields.TryParse("foo", out var fields);

            // Act
            var result = FieldsParserResult.Success(fields);

            // Assert
            Assert.True(result.IsFieldsSet);
        }

        [Fact]
        public void TheSuccessMethodShouldSetHasErrorToFalse()
        {
            // Arrange
            Fields.TryParse("foo", out var fields);

            // Act
            var result = FieldsParserResult.Success(fields);

            // Assert
            Assert.False(result.HasError);
        }

        [Fact]
        public void TheSuccessMethodShouldSetFields()
        {
            // Arrange
            Fields.TryParse("foo", out var fields);

            // Act
            var result = FieldsParserResult.Success(fields);

            // Assert
            var field = result.Fields.Values.First();
            var part = field.Parts.First();

            Assert.Equal("foo", part.ToString());
        }
    }
}
