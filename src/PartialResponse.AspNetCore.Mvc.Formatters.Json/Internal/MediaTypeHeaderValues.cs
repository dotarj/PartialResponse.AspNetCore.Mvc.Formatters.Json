// Copyright (c) Arjen Post and contributors. See LICENSE and NOTICE in the project root for license information.

using Microsoft.Net.Http.Headers;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Internal
{
    internal static class MediaTypeHeaderValues
    {
        public static readonly MediaTypeHeaderValue ApplicationJson = MediaTypeHeaderValue.Parse("application/json").CopyAsReadOnly();

        public static readonly MediaTypeHeaderValue TextJson = MediaTypeHeaderValue.Parse("text/json").CopyAsReadOnly();

        public static readonly MediaTypeHeaderValue ApplicationAnyJsonSyntax = MediaTypeHeaderValue.Parse("application/*+json").CopyAsReadOnly();
    }
}
