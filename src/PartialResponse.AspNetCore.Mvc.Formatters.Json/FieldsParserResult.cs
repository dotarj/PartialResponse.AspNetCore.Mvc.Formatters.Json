// Copyright (c) Arjen Post. See LICENSE and NOTICE in the project root for license information.

using PartialResponse.Core;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json
{
    /// <summary>
    /// Result of a <see cref="IFieldsParser.Parse"/> operation.
    /// </summary>
    public class FieldsParserResult
    {
        private static readonly FieldsParserResult FailureValue = new FieldsParserResult(hasError: true);
        private static readonly FieldsParserResult NoValueValue = new FieldsParserResult(hasError: false);

        private FieldsParserResult(bool hasError)
        {
            this.HasError = hasError;
        }

        private FieldsParserResult(Fields fields)
        {
            this.Fields = fields;
            this.IsFieldsSet = true;
        }

        /// <summary>
        /// Gets a collection of fields if present; otherwise, null.
        /// </summary>
        public Fields Fields { get; }

        /// <summary>
        /// Gets a value indicating whether or not the <see cref="Fields"/> value has been set.
        /// </summary>
        public bool IsFieldsSet { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="IFieldsParser.Parse"/> operation had an error.
        /// </summary>
        public bool HasError { get; }

        /// <summary>
        /// Returns an <see cref="FieldsParserResult"/> indicating the <see cref="IFieldsParser.Parse"/>
        /// operation failed.
        /// </summary>
        /// <returns>An <see cref="FieldsParserResult"/> indicating the <see cref="IFieldsParser.Parse"/>
        /// operation failed i.e. with <see cref="HasError"/> <c>true</c>.
        /// </returns>
        public static FieldsParserResult Failure()
        {
            return FailureValue;
        }

        /// <summary>
        /// Returns an <see cref="FieldsParserResult"/> indicating the <see cref="IFieldsParser.Parse"/>
        /// operation was successful.
        /// </summary>
        /// <param name="fields">The <see cref="Fields"/>.</param>
        /// <returns>
        /// An <see cref="FieldsParserResult"/> indicating the <see cref="IFieldsParser.Parse"/>
        /// operation succeeded i.e. with <see cref="HasError"/> <c>false</c>.
        /// </returns>
        public static FieldsParserResult Success(Fields fields)
        {
            return new FieldsParserResult(fields);
        }

        /// <summary>
        /// Returns an <see cref="FieldsParserResult"/> indicating the <see cref="IFieldsParser.Parse"/>
        /// operation produced no value.
        /// </summary>
        /// <returns>
        /// An <see cref="FieldsParserResult"/> indicating the <see cref="IFieldsParser.Parse"/>
        /// operation produced no value.
        /// </returns>
        public static FieldsParserResult NoValue()
        {
            return NoValueValue;
        }
    }
}