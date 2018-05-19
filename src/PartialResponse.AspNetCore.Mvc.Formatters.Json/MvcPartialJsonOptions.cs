// Copyright (c) Arjen Post. See LICENSE and NOTICE in the project root for license information.

using Newtonsoft.Json;
using PartialResponse.AspNetCore.Mvc.Formatters;

namespace PartialResponse.AspNetCore.Mvc
{
    /// <summary>
    /// Provides programmatic configuration for JSON in the MVC framework.
    /// </summary>
    public class MvcPartialJsonOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether partial response allows case-insensitive matching.
        /// </summary>
        public bool IgnoreCase { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether fields parse errors should be ignore or return a 400 error
        /// </summary>
        public bool IgnoreParseErrors { get; set; }

        /// <summary>
        /// Gets or sets the get parameter used to parse the fields
        /// </summary>
        public string FieldsParamName { get; set; } = "fields";

        /// <summary>
        /// Gets the <see cref="JsonSerializerSettings"/> that are used by this application.
        /// </summary>
        public JsonSerializerSettings SerializerSettings { get; } = JsonSerializerSettingsProvider.CreateSerializerSettings();
    }
}