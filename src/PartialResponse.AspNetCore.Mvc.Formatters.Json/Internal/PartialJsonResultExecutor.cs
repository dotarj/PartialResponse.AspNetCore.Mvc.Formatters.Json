// Copyright (c) Arjen Post and contributors. See LICENSE and NOTICE in the project root for license information.

#if !ASPNETCORE2
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Internal
{
    /// <summary>
    /// Executes a <see cref="PartialJsonResult"/> to write to the response.
    /// </summary>
    public class PartialJsonResultExecutor : IActionResultExecutor<PartialJsonResult>
    {
        private static readonly string DefaultContentType = new MediaTypeHeaderValue("application/json") { Encoding = Encoding.UTF8 }.ToString();

        private readonly IHttpResponseStreamWriterFactory writerFactory;
        private readonly ILogger logger;
        private readonly MvcOptions mvcOptions;
        private readonly MvcPartialJsonOptions jsonOptions;
        private readonly IFieldsParser fieldsParser;
        private readonly IArrayPool<char> charPool;
        private readonly AsyncEnumerableReader asyncEnumerableReader;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartialJsonResultExecutor"/> class.
        /// </summary>
        /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
        /// <param name="logger">The <see cref="ILogger{PartialJsonResultExecutor}"/>.</param>
        /// <param name="mvcOptions">Accessor to <see cref="MvcOptions"/>.</param>
        /// <param name="jsonOptions">Accessor to <see cref="MvcPartialJsonOptions"/>.</param>
        /// <param name="fieldsParser">The <see cref="IFieldsParser"/>.</param>
        /// <param name="charPool">The <see cref="ArrayPool{Char}"/> for creating <see cref="T:char[]"/> buffers.</param>
        public PartialJsonResultExecutor(IHttpResponseStreamWriterFactory writerFactory, ILogger<PartialJsonResultExecutor> logger, IOptions<MvcOptions> mvcOptions, IOptions<MvcPartialJsonOptions> jsonOptions, IFieldsParser fieldsParser, ArrayPool<char> charPool)
        {
            if (writerFactory == null)
            {
                throw new ArgumentNullException(nameof(writerFactory));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (jsonOptions == null)
            {
                throw new ArgumentNullException(nameof(jsonOptions));
            }

            if (fieldsParser == null)
            {
                throw new ArgumentNullException(nameof(fieldsParser));
            }

            if (charPool == null)
            {
                throw new ArgumentNullException(nameof(charPool));
            }

            this.writerFactory = writerFactory;
            this.logger = logger;
            this.mvcOptions = mvcOptions?.Value ?? throw new ArgumentNullException(nameof(mvcOptions));
            this.jsonOptions = jsonOptions.Value;
            this.fieldsParser = fieldsParser;
            this.charPool = new JsonArrayPool<char>(charPool);
            this.asyncEnumerableReader = new AsyncEnumerableReader(this.mvcOptions);
        }

        /// <summary>
        /// Executes the <see cref="PartialJsonResult"/> and writes the response.
        /// </summary>
        /// <param name="context">The <see cref="ActionContext"/>.</param>
        /// <param name="result">The <see cref="PartialJsonResult"/>.</param>
        /// <returns>A <see cref="Task"/> which will complete when writing has completed.</returns>
        public async Task ExecuteAsync(ActionContext context, PartialJsonResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var jsonSerializerSettings = this.GetSerializerSettings(result);

            var request = context.HttpContext.Request;
            var response = context.HttpContext.Response;

            var fieldsParserResult = this.fieldsParser.Parse(request);

            if (fieldsParserResult.HasError && !this.jsonOptions.IgnoreParseErrors)
            {
                response.StatusCode = 400;

                return;
            }

            ResponseContentTypeHelper.ResolveContentTypeAndEncoding(result.ContentType, response.ContentType, DefaultContentType, out var resolvedContentType, out var resolvedContentTypeEncoding);

            response.ContentType = resolvedContentType;

            if (result.StatusCode != null)
            {
                response.StatusCode = result.StatusCode.Value;
            }

            Log.PartialJsonResultExecuting(this.logger, result.Value);

            var responseStream = response.Body;
            FileBufferingWriteStream fileBufferingWriteStream = null;
            if (!this.mvcOptions.SuppressOutputFormatterBuffering)
            {
                fileBufferingWriteStream = new FileBufferingWriteStream();
                responseStream = fileBufferingWriteStream;
            }

            try
            {
                await using (var writer = this.writerFactory.CreateWriter(responseStream, resolvedContentTypeEncoding))
                {
                    using var jsonWriter = new JsonTextWriter(writer);
                    jsonWriter.ArrayPool = this.charPool;
                    jsonWriter.CloseOutput = false;
                    jsonWriter.AutoCompleteOnClose = false;

                    var jsonSerializer = JsonSerializer.Create(jsonSerializerSettings);
                    var value = result.Value;
                    if (result.Value is IAsyncEnumerable<object> asyncEnumerable)
                    {
                        Log.BufferingAsyncEnumerable(this.logger, asyncEnumerable);
                        value = await this.asyncEnumerableReader.ReadAsync(asyncEnumerable);
                    }

                    if (fieldsParserResult.IsFieldsSet && !fieldsParserResult.HasError)
                    {
                        jsonSerializer.Serialize(jsonWriter, result.Value, path => fieldsParserResult.Fields.Matches(path, this.jsonOptions.IgnoreCase));
                    }
                    else
                    {
                        jsonSerializer.Serialize(jsonWriter, result.Value);
                    }
                }

                if (fileBufferingWriteStream != null)
                {
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

        private JsonSerializerSettings GetSerializerSettings(PartialJsonResult result)
        {
            var serializerSettings = result.SerializerSettings;
            if (serializerSettings == null)
            {
                return this.jsonOptions.SerializerSettings;
            }
            else
            {
                if (serializerSettings is JsonSerializerSettings settingsFromResult)
                {
                    return settingsFromResult;
                }

                throw new InvalidOperationException("Resources.FormatProperty_MustBeInstanceOfType(nameof(PartialJsonResult), nameof(PartialJsonResult.SerializerSettings), typeof(JsonSerializerSettings))");
            }
        }

        private static class Log
        {
            private static readonly Action<ILogger, string, Exception> PartialJsonResultExecutingValue;
            private static readonly Action<ILogger, string, Exception> BufferingAsyncEnumerableValue;

            static Log()
            {
                PartialJsonResultExecutingValue = LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, "PartialJsonResultExecuting"), "Executing PartialJsonResult, writing value of type '{Type}'.");

                BufferingAsyncEnumerableValue = LoggerMessage.Define<string>(LogLevel.Debug, new EventId(1, "BufferingAsyncEnumerable"), "Buffering IAsyncEnumerable instance of type '{Type}'.");
            }

            public static void PartialJsonResultExecuting(ILogger logger, object value)
            {
                var type = value == null ? "null" : value.GetType().FullName;
                PartialJsonResultExecutingValue(logger, type, null);
            }

            public static void BufferingAsyncEnumerable(ILogger logger, IAsyncEnumerable<object> asyncEnumerable) => BufferingAsyncEnumerableValue(logger, asyncEnumerable.GetType().FullName, null);
        }
    }
}
#else
using System;
using System.Buffers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Internal
{
    /// <summary>
    /// Executes a <see cref="PartialJsonResult"/> to write to the response.
    /// </summary>
    public class PartialJsonResultExecutor
    {
        private static readonly string DefaultContentType = new MediaTypeHeaderValue("application/json") { Encoding = Encoding.UTF8 }.ToString();

        private readonly IFieldsParser fieldsParser;
        private readonly IArrayPool<char> charPool;

        /// <summary>
        /// Initializes a new instance of the <see cref="PartialJsonResultExecutor"/> class.
        /// </summary>
        /// <param name="writerFactory">The <see cref="IHttpResponseStreamWriterFactory"/>.</param>
        /// <param name="logger">The <see cref="ILogger{PartialJsonResultExecutor}"/>.</param>
        /// <param name="options">The <see cref="IOptions{MvcPartialJsonOptions}"/>.</param>
        /// <param name="fieldsParser">The <see cref="fieldsParser"/>.</param>
        /// <param name="charPool">The <see cref="ArrayPool{Char}"/> for creating <see cref="T:char[]"/> buffers.</param>
        public PartialJsonResultExecutor(IHttpResponseStreamWriterFactory writerFactory, ILogger<PartialJsonResultExecutor> logger, IOptions<MvcPartialJsonOptions> options, IFieldsParser fieldsParser, ArrayPool<char> charPool)
        {
            if (writerFactory == null)
            {
                throw new ArgumentNullException(nameof(writerFactory));
            }

            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (fieldsParser == null)
            {
                throw new ArgumentNullException(nameof(fieldsParser));
            }

            if (charPool == null)
            {
                throw new ArgumentNullException(nameof(charPool));
            }

            this.WriterFactory = writerFactory;
            this.Logger = logger;
            this.Options = options.Value;
            this.fieldsParser = fieldsParser;
            this.charPool = new JsonArrayPool<char>(charPool);
        }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the <see cref="MvcPartialJsonOptions"/>.
        /// </summary>
        protected MvcPartialJsonOptions Options { get; }

        /// <summary>
        /// Gets the <see cref="IHttpResponseStreamWriterFactory"/>.
        /// </summary>
        protected IHttpResponseStreamWriterFactory WriterFactory { get; }

        /// <summary>
        /// Executes the <see cref="PartialJsonResult"/> and writes the response.
        /// </summary>
        /// <param name="context">The <see cref="ActionContext"/>.</param>
        /// <param name="result">The <see cref="PartialJsonResult"/>.</param>
        /// <returns>A <see cref="Task"/> which will complete when writing has completed.</returns>
        public Task ExecuteAsync(ActionContext context, PartialJsonResult result)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var request = context.HttpContext.Request;
            var response = context.HttpContext.Response;

            var fieldsParserResult = this.fieldsParser.Parse(request);

            if (fieldsParserResult.HasError && !this.Options.IgnoreParseErrors)
            {
                response.StatusCode = 400;

                return Task.CompletedTask;
            }

            string resolvedContentType = null;
            Encoding resolvedContentTypeEncoding = null;

            ResponseContentTypeHelper.ResolveContentTypeAndEncoding(result.ContentType, response.ContentType, DefaultContentType, out resolvedContentType, out resolvedContentTypeEncoding);

            response.ContentType = resolvedContentType;

            if (result.StatusCode != null)
            {
                response.StatusCode = result.StatusCode.Value;
            }

            var serializerSettings = result.SerializerSettings ?? this.Options.SerializerSettings;

            this.Logger.PartialJsonResultExecuting(result.Value);

            using (var writer = this.WriterFactory.CreateWriter(response.Body, resolvedContentTypeEncoding))
            {
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    jsonWriter.ArrayPool = this.charPool;
                    jsonWriter.CloseOutput = false;

                    var jsonSerializer = JsonSerializer.Create(serializerSettings);

                    if (fieldsParserResult.IsFieldsSet && !fieldsParserResult.HasError)
                    {
                        jsonSerializer.Serialize(jsonWriter, result.Value, path => fieldsParserResult.Fields.Matches(path, this.Options.IgnoreCase));
                    }
                    else
                    {
                        jsonSerializer.Serialize(jsonWriter, result.Value);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
#endif
