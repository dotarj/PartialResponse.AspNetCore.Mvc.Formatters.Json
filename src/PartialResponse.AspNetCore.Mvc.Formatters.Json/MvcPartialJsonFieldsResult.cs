// Copyright (c) Arjen Post. See LICENSE and NOTICE in the project root for license information.

using PartialResponse.Core;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json
{
    /// <summary>
    /// Class to store fields parse result
    /// </summary>
    public class MvcPartialJsonFieldsResult
    {
        /// <summary>
        /// Gets or sets the <see cref="Fields"/>, if any.
        /// </summary>
        public Fields Fields { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether field was present.
        /// </summary>
        public bool IsPresent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether an error while parsing the fields.
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Field"/> property holds a valid object.
        /// </summary>
        public bool IsValid => this.IsPresent && !this.IsError;
    }
}