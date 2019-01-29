// Copyright (c) Arjen Post and contributors. See LICENSE and NOTICE in the project root for license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PartialResponse.AspNetCore.Mvc.Formatters;
using PartialResponse.Core;

namespace PartialResponse.AspNetCore.Mvc
{
    /// <summary>
    /// Provides programmatic configuration for JSON in the MVC framework.
    /// </summary>
    public class MvcPartialJsonOptions
    {
        private IEnumerable<string> ignoredFields;

        /// <summary>
        /// Gets or sets a value indicating whether partial response allows case-insensitive matching.
        /// </summary>
        public bool IgnoreCase { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether fields parse errors should be ignored or return a 400 status code.
        /// </summary>
        public bool IgnoreParseErrors { get; set; } = false;

        /// <summary>
        /// Gets or sets a list of fields which should always be serialized.
        /// </summary>
        public IEnumerable<string> IgnoredFields
        {
            get
            {
                return this.ignoredFields;
            }

            set
            {
                this.ignoredFields = value;

                this.ParsedIgnoredFields = this.ParseIgnoredFields(value);
            }
        }

        /// <summary>
        /// Gets the <see cref="JsonSerializerSettings"/> that are used by this application.
        /// </summary>
        public JsonSerializerSettings SerializerSettings { get; } = JsonSerializerSettingsProvider.CreateSerializerSettings();

        internal Fields? ParsedIgnoredFields { get; private set; }

        private Fields? ParseIgnoredFields(IEnumerable<string> value)
        {
            if (value != null)
            {
                if (!Fields.TryParse(string.Join(",", value), out var fields))
                {
                    throw new ArgumentException($"Unable to parse {nameof(MvcPartialJsonOptions.IgnoredFields)} property.", nameof(value));
                }

                return fields;
            }

            return null;
        }
    }
}