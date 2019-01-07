// Copyright (c) Arjen Post. See LICENSE and NOTICE in the project root for license information.

using Microsoft.AspNetCore.Http;
using PartialResponse.AspNetCore.Mvc.Formatters.Json;

namespace PartialResponse.Extensions.DependencyInjection
{
    /// <summary>
    /// Parses the fields from the request.
    /// </summary>
    public interface IFieldsParser
    {
        /// <summary>
        /// Parses the fields from the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>The parsed fields.</returns>
        FieldsParserResult Parse(HttpRequest request);
    }
}