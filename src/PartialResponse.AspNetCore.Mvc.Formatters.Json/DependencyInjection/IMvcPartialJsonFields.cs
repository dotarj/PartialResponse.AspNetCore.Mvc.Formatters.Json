// Copyright (c) Arjen Post. See LICENSE and NOTICE in the project root for license information.

using PartialResponse.AspNetCore.Mvc.Formatters.Json;

namespace PartialResponse.Extensions.DependencyInjection
{
    /// <summary>
    /// Interface to access the fields via dependency injection
    /// </summary>
    public interface IMvcPartialJsonFields
    {
        /// <summary>
        /// Gets the fields parsing results
        /// </summary>
        /// <param name="paramName">The query parameter to parse</param>
        /// <returns>The fields parsing results</returns>
        MvcPartialJsonFieldsResult GetFieldsResult(string paramName);
    }
}