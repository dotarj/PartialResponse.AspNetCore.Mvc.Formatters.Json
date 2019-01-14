// Copyright (c) Arjen Post. See LICENSE and NOTICE in the project root for license information.
using System;
using Microsoft.AspNetCore.Http;
using PartialResponse.Core;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json
{
    /// <summary>
    /// Parses the fields parameter from the request.
    /// </summary>
    public class FieldsParser : IFieldsParser
    {
        private const string FieldsCacheKey = "FieldsCache";
        private const string FieldsParameterName = "fields";

        /// <summary>
        /// Parses the fields parameter from the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The parsed fields.</returns>
        public FieldsParserResult Parse(HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            FieldsParserResult fieldsResult = null;

            var httpContext = request.HttpContext;

            if (!httpContext.Items.ContainsKey(FieldsCacheKey))
            {
                httpContext.Items[FieldsCacheKey] = fieldsResult = this.ParseImpl(request);
            }
            else
            {
                fieldsResult = (FieldsParserResult)httpContext.Items[FieldsCacheKey];
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