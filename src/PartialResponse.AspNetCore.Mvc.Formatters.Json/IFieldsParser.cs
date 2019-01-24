// Copyright (c) Arjen Post and contributors. See LICENSE and NOTICE in the project root for license information.
using Microsoft.AspNetCore.Http;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json
{
    /// <summary>
    /// Parses the fields parameter from the request.
    /// </summary>
    public interface IFieldsParser
    {
        /// <summary>
        /// Parses the fields parameter from the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The parsed fields.</returns>
        FieldsParserResult Parse(HttpRequest request);
    }
}