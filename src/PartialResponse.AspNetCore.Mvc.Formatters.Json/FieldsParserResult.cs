// Copyright (c) Arjen Post and contributors. See LICENSE and NOTICE in the project root for license information.

using PartialResponse.Core;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json
{
    /// <summary>
    /// Result of a <see cref="IFieldsParser.Parse"/> operation.
    /// </summary>
    public readonly struct FieldsParserResult
    {
        private FieldsParserResult(Fields? fields, bool hasError)
        {
            this.Fields = fields ?? default;
            this.IsFieldsSet = fields.HasValue;
            this.HasError = hasError;
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
        public static FieldsParserResult Failed()
        {
            return new FieldsParserResult(null, true);
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
            return new FieldsParserResult(fields, false);
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
            return new FieldsParserResult(null, false);
        }
    }
}