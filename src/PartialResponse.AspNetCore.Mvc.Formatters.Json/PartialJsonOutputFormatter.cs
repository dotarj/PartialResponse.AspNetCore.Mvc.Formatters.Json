// Copyright (c) Arjen Post and contributors. See LICENSE and NOTICE in the project root for license information.

#if !ASPNETCORE2
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using PartialResponse.AspNetCore.Mvc.Formatters.Json;
using PartialResponse.AspNetCore.Mvc.Formatters.Json.Internal;

namespace PartialResponse.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// A <see cref="TextOutputFormatter"/> for JSON content.
    /// </summary>
    public class PartialJsonOutputFormatter : TextOutputFormatter
    {
        internal const string BypassPartialResponseKey = "BypassPartialResponse";

        private readonly IFieldsParser fieldsParser;
        private readonly IArrayPool<char> charPool;
        private readonly MvcPartialJsonOptions options;
        private readonly MvcOptions mvcOptions;

        // Perf: JsonSerializers are relatively expensive to create, and are thread safe. We cache
        // the serializer and invalidate it when the settings change.
        private JsonSerializer serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartialJsonOutputFormatter"/> class.
        /// </summary>
        /// <param name="serializerSettings">
        /// The <see cref="JsonSerializerSettings"/>. Should be either the application-wide settings
        /// (<see cref="MvcPartialJsonOptions.SerializerSettings"/>) or an instance
        /// <see cref="JsonSerializerSettingsProvider.CreateSerializerSettings"/> initially returned.
        /// </param>
        /// <param name="fieldsParser">The <see cref="IFieldsParser"/>.</param>
        /// <param name="charPool">The <see cref="ArrayPool{Char}"/>.</param>
        /// <param name="options">The <see cref="MvcPartialJsonOptions"/>.</param>
        /// <param name="mvcOptions">The <see cref="MvcOptions"/>.</param>
        public PartialJsonOutputFormatter(JsonSerializerSettings serializerSettings, IFieldsParser fieldsParser, ArrayPool<char> charPool, MvcPartialJsonOptions options, MvcOptions mvcOptions)
        {
            if (serializerSettings == null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            if (fieldsParser == null)
            {
                throw new ArgumentNullException(nameof(fieldsParser));
            }

            if (charPool == null)
            {
                throw new ArgumentNullException(nameof(charPool));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (mvcOptions == null)
            {
                throw new ArgumentNullException(nameof(mvcOptions));
            }

            this.SerializerSettings = serializerSettings;
            this.fieldsParser = fieldsParser;
            this.charPool = new JsonArrayPool<char>(charPool);
            this.options = options;
            this.mvcOptions = mvcOptions;
            this.SupportedEncodings.Add(Encoding.UTF8);
            this.SupportedEncodings.Add(Encoding.Unicode);
            this.SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJson);
            this.SupportedMediaTypes.Add(MediaTypeHeaderValues.TextJson);
            this.SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationAnyJsonSyntax);
        }

        /// <summary>
        /// Gets the <see cref="JsonSerializerSettings"/> used to configure the <see cref="JsonSerializer"/>.
        /// </summary>
        /// <remarks>
        /// Any modifications to the <see cref="JsonSerializerSettings"/> object after this
        /// <see cref="PartialJsonOutputFormatter"/> has been used will have no effect.
        /// </remarks>
        protected JsonSerializerSettings SerializerSettings { get; }

        /// <inheritdoc />
        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (selectedEncoding == null)
            {
                throw new ArgumentNullException(nameof(selectedEncoding));
            }

            return this.WriteResponseBodyImplAsync(context, selectedEncoding);
        }

        /// <summary>
        /// Called during serialization to create the <see cref="JsonWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> used to write.</param>
        /// <returns>The <see cref="JsonWriter"/> used during serialization.</returns>
        protected virtual JsonWriter CreateJsonWriter(TextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var jsonWriter = new JsonTextWriter(writer) { ArrayPool = this.charPool, CloseOutput = false, AutoCompleteOnClose = false };

            return jsonWriter;
        }

        /// <summary>
        /// Called during serialization to create the <see cref="JsonSerializer"/>.
        /// </summary>
        /// <returns>The <see cref="JsonSerializer"/> used during serialization and deserialization.</returns>
        protected virtual JsonSerializer CreateJsonSerializer()
        {
            if (this.serializer == null)
            {
                this.serializer = JsonSerializer.Create(this.SerializerSettings);
            }

            return this.serializer;
        }

        private async Task WriteResponseBodyImplAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var response = context.HttpContext.Response;

            FieldsParserResult fieldsParserResult = default;

            if (!this.ShouldBypassPartialResponse(context.HttpContext))
            {
                fieldsParserResult = this.fieldsParser.Parse(context.HttpContext.Request);

                if (fieldsParserResult.HasError && !this.options.IgnoreParseErrors)
                {
                    response.StatusCode = 400;

                    return;
                }
            }

            var responseStream = response.Body;
            FileBufferingWriteStream fileBufferingWriteStream = null;
            if (!this.mvcOptions.SuppressOutputFormatterBuffering)
            {
                fileBufferingWriteStream = new FileBufferingWriteStream();
                responseStream = fileBufferingWriteStream;
            }

            try
            {
                await using (var writer = context.WriterFactory(responseStream, selectedEncoding))
                {
                    this.WriteObject(writer, context.Object, fieldsParserResult);
                }

                if (fileBufferingWriteStream != null)
                {
                    response.ContentLength = fileBufferingWriteStream.Length;
                    await fileBufferingWriteStream.DrainBufferAsync(response.Body);
                }
            }
            finally
            {
                if (fileBufferingWriteStream != null)
                {
                    await fileBufferingWriteStream.DisposeAsync();
                }
            }
        }

        private bool ShouldBypassPartialResponse(HttpContext httpContext)
        {
            if (httpContext.Items.ContainsKey(BypassPartialResponseKey))
            {
                return true;
            }

            return httpContext.Response.StatusCode != 200;
        }

        private void WriteObject(TextWriter writer, object value, FieldsParserResult fieldsParserResult)
        {
            using (var jsonWriter = this.CreateJsonWriter(writer))
            {
                var jsonSerializer = this.CreateJsonSerializer();

                if (fieldsParserResult.IsFieldsSet && !fieldsParserResult.HasError)
                {
                    jsonSerializer.Serialize(jsonWriter, value, path => fieldsParserResult.Fields.Matches(path, this.options.IgnoreCase));
                }
                else
                {
                    jsonSerializer.Serialize(jsonWriter, value);
                }
            }
        }
    }
}
#else
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;
using PartialResponse.AspNetCore.Mvc.Formatters.Json;
using PartialResponse.AspNetCore.Mvc.Formatters.Json.Internal;
using PartialResponse.Core;

namespace PartialResponse.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// A <see cref="TextOutputFormatter"/> for JSON content.
    /// </summary>
    public class PartialJsonOutputFormatter : TextOutputFormatter
    {
        internal const string BypassPartialResponseKey = "BypassPartialResponse";

        private readonly IFieldsParser fieldsParser;
        private readonly IArrayPool<char> charPool;
        private readonly MvcPartialJsonOptions options;

        // Perf: JsonSerializers are relatively expensive to create, and are thread safe. We cache
        // the serializer and invalidate it when the settings change.
        private JsonSerializer serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartialJsonOutputFormatter"/> class.
        /// </summary>
        /// <param name="serializerSettings">
        /// The <see cref="JsonSerializerSettings"/>. Should be either the application-wide settings
        /// (<see cref="MvcPartialJsonOptions.SerializerSettings"/>) or an instance
        /// <see cref="JsonSerializerSettingsProvider.CreateSerializerSettings"/> initially returned.
        /// </param>
        /// <param name="fieldsParser">The <see cref="IFieldsParser"/>.</param>
        /// <param name="charPool">The <see cref="ArrayPool{Char}"/>.</param>
        /// <param name="options">The <see cref="MvcPartialJsonOptions"/>.</param>
        public PartialJsonOutputFormatter(JsonSerializerSettings serializerSettings, IFieldsParser fieldsParser, ArrayPool<char> charPool, MvcPartialJsonOptions options)
        {
            if (serializerSettings == null)
            {
                throw new ArgumentNullException(nameof(serializerSettings));
            }

            if (fieldsParser == null)
            {
                throw new ArgumentNullException(nameof(fieldsParser));
            }

            if (charPool == null)
            {
                throw new ArgumentNullException(nameof(charPool));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            this.SerializerSettings = serializerSettings;
            this.fieldsParser = fieldsParser;
            this.charPool = new JsonArrayPool<char>(charPool);
            this.options = options;

            this.SupportedEncodings.Add(Encoding.UTF8);
            this.SupportedEncodings.Add(Encoding.Unicode);
            this.SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationJson);
            this.SupportedMediaTypes.Add(MediaTypeHeaderValues.ApplicationAnyJsonSyntax);
            this.SupportedMediaTypes.Add(MediaTypeHeaderValues.TextJson);
        }

        /// <summary>
        /// Gets the <see cref="JsonSerializerSettings"/> used to configure the <see cref="JsonSerializer"/>.
        /// </summary>
        /// <remarks>
        /// Any modifications to the <see cref="JsonSerializerSettings"/> object after this
        /// <see cref="PartialJsonOutputFormatter"/> has been used will have no effect.
        /// </remarks>
        protected JsonSerializerSettings SerializerSettings { get; }

        /// <inheritdoc />
        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (selectedEncoding == null)
            {
                throw new ArgumentNullException(nameof(selectedEncoding));
            }

            return this.WriteResponseBodyImplAsync(context, selectedEncoding);
        }

        /// <summary>
        /// Called during serialization to create the <see cref="JsonWriter"/>.
        /// </summary>
        /// <param name="writer">The <see cref="TextWriter"/> used to write.</param>
        /// <returns>The <see cref="JsonWriter"/> used during serialization.</returns>
        protected virtual JsonWriter CreateJsonWriter(TextWriter writer)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            var jsonWriter = new JsonTextWriter(writer) { ArrayPool = this.charPool, CloseOutput = false, };

            return jsonWriter;
        }

        /// <summary>
        /// Called during serialization to create the <see cref="JsonSerializer"/>.
        /// </summary>
        /// <returns>The <see cref="JsonSerializer"/> used during serialization and deserialization.</returns>
        protected virtual JsonSerializer CreateJsonSerializer()
        {
            if (this.serializer == null)
            {
                this.serializer = JsonSerializer.Create(this.SerializerSettings);
            }

            return this.serializer;
        }

        private async Task WriteResponseBodyImplAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var response = context.HttpContext.Response;

            FieldsParserResult fieldsParserResult = default;

            if (!this.ShouldBypassPartialResponse(context.HttpContext))
            {
                fieldsParserResult = this.fieldsParser.Parse(context.HttpContext.Request);

                if (fieldsParserResult.HasError && !this.options.IgnoreParseErrors)
                {
                    response.StatusCode = 400;

                    return;
                }
            }

            using (var writer = context.WriterFactory(response.Body, selectedEncoding))
            {
                this.WriteObject(writer, context.Object, fieldsParserResult);

                // Perf: call FlushAsync to call WriteAsync on the stream with any content left in the TextWriter's
                // buffers. This is better than just letting dispose handle it (which would result in a synchronous
                // write).
                await writer.FlushAsync();
            }
        }

        private bool ShouldBypassPartialResponse(HttpContext httpContext)
        {
            if (httpContext.Items.ContainsKey(BypassPartialResponseKey))
            {
                return true;
            }

            return httpContext.Response.StatusCode != 200;
        }

        private void WriteObject(TextWriter writer, object value, FieldsParserResult fieldsParserResult)
        {
            using (var jsonWriter = this.CreateJsonWriter(writer))
            {
                var jsonSerializer = this.CreateJsonSerializer();

                if (fieldsParserResult.IsFieldsSet && !fieldsParserResult.HasError)
                {
                    jsonSerializer.Serialize(jsonWriter, value, path => this.ShouldSerialize(path, fieldsParserResult.Fields, this.options));
                }
                else
                {
                    jsonSerializer.Serialize(jsonWriter, value);
                }
            }
        }

        private bool ShouldSerialize(string path, Fields fields, MvcPartialJsonOptions options)
        {
            var parsedIgnoredFields = options.ParsedIgnoredFields;

            if (parsedIgnoredFields.HasValue)
            {
                if (parsedIgnoredFields.Value.Matches(path, options.IgnoreCase))
                {
                    return true;
                }
            }

            return fields.Matches(path, options.IgnoreCase);
        }
    }
}
#endif
