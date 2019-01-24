// Copyright (c) Arjen Post and contributors. See LICENSE and NOTICE in the project root for license information.

using System;
using System.Collections.Generic;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Internal
{
    internal class SerializerContext
    {
        private readonly Func<string, bool> shouldSerialize;
        private readonly Dictionary<string, bool> cache = new Dictionary<string, bool>();

        public SerializerContext(Func<string, bool> shouldSerialize)
        {
            this.shouldSerialize = shouldSerialize;
        }

        public bool ShouldSerialize(string path)
        {
            if (this.cache.ContainsKey(path))
            {
                return this.cache[path];
            }

            var result = this.shouldSerialize(path);

            this.cache.Add(path, result);

            return result;
        }
    }
}