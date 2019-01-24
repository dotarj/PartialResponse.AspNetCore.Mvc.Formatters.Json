// Copyright (c) Arjen Post and contributors. See LICENSE and NOTICE in the project root for license information.

using System;
using System.Buffers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Internal
{
    /// <summary>
    /// Sets up JSON formatter options for <see cref="MvcOptions"/>.
    /// </summary>
    public class MvcPartialJsonMvcOptionsSetup : IConfigureOptions<MvcOptions>
    {
        private readonly IFieldsParser fieldsParser;
        private readonly MvcPartialJsonOptions partialJsonOptions;
        private readonly ArrayPool<char> charPool;

        /// <summary>
        /// Initializes a new instance of the <see cref="MvcPartialJsonMvcOptionsSetup"/> class.
        /// </summary>
        /// <param name="fieldsParser">The fields parser.</param>
        /// <param name="partialJsonOptions">The options.</param>
        /// <param name="charPool">The character array pool.</param>
        public MvcPartialJsonMvcOptionsSetup(IFieldsParser fieldsParser, IOptions<MvcPartialJsonOptions> partialJsonOptions, ArrayPool<char> charPool)
        {
            if (fieldsParser == null)
            {
                throw new ArgumentNullException(nameof(fieldsParser));
            }

            if (partialJsonOptions == null)
            {
                throw new ArgumentNullException(nameof(partialJsonOptions));
            }

            if (charPool == null)
            {
                throw new ArgumentNullException(nameof(charPool));
            }

            this.fieldsParser = fieldsParser;
            this.partialJsonOptions = partialJsonOptions.Value;
            this.charPool = charPool;
        }

        /// <summary>
        /// Configures the <see cref="MvcOptions"/> by adding the <see cref="PartialJsonOutputFormatter"/>.
        /// </summary>
        /// <param name="options">The MVC options.</param>
        public void Configure(MvcOptions options)
        {
            options.OutputFormatters.Add(new PartialJsonOutputFormatter(this.partialJsonOptions.SerializerSettings, this.fieldsParser, this.charPool, this.partialJsonOptions));
            options.FormatterMappings.SetMediaTypeMappingForFormat("json", MediaTypeHeaderValue.Parse("application/json"));
            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(JToken)));
        }
    }
}
