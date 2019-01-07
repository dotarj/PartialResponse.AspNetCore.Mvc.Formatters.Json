// Copyright (c) Arjen Post. See LICENSE and NOTICE in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using PartialResponse.AspNetCore.Mvc.Formatters.Json;
using PartialResponse.Core;

namespace PartialResponse.Extensions.DependencyInjection
{
    /// <summary>
    /// Parses the fields from the request.
    /// </summary>
    public class FieldsParser : IFieldsParser
    {
        private const string KeyContextItems = nameof(FieldsParserResult);
        private const string FieldsParameterName = "fields";

        /// <summary>
        /// Parses the fields from the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The parsed fields.</returns>
        public FieldsParserResult Parse(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            FieldsParserResult fieldsResult = default;

            var httpContext = request.HttpContext;

            if (!httpContext.Items.ContainsKey(KeyContextItems))
            {
                httpContext.Items[KeyContextItems] = fieldsResult = this.ParseImpl(request);
            }
            else
            {
                fieldsResult = (FieldsParserResult)httpContext.Items[KeyContextItems];
            }

            return fieldsResult;
        }

        private FieldsParserResult ParseImpl(HttpRequest request)
        {
            if (!request.Query.ContainsKey(FieldsParameterName))
            {
                return FieldsParserResult.NoValue();
            }

            var isError = !Fields.TryParse(request.Query[FieldsParameterName][0], out var fields);

            if (isError)
            {
                return FieldsParserResult.Failure();
            }

            return FieldsParserResult.Success(fields);
        }
    }
}