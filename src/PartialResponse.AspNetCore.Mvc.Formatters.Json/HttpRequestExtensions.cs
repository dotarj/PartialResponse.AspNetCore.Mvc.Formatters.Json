// Copyright (c) Arjen Post. See LICENSE and NOTICE in the project root for license information.

using System;
using PartialResponse.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Provides extension methods for the <see cref="HttpRequest"/> class.
    /// </summary>
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Indicates that partial response should be bypassed for the current request.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        public static void BypassPartialResponse(this HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            request.HttpContext.Items[PartialJsonOutputFormatter.BypassPartialResponseKey] = null;
        }
    }
}
