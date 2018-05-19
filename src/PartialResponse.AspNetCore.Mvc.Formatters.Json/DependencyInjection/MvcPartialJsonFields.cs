// Copyright (c) Arjen Post. See LICENSE and NOTICE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PartialResponse.AspNetCore.Mvc;
using PartialResponse.AspNetCore.Mvc.Formatters.Json;
using PartialResponse.Core;

namespace PartialResponse.Extensions.DependencyInjection
{
    /// <summary>
    /// Class to access the fields via dependency injection
    /// </summary>
    public class MvcPartialJsonFields
    {
        /// <summary>
        /// Key used to store fields object
        /// </summary>
        protected static readonly string KeyContextItems = nameof(MvcPartialJsonFieldsResult);

        /// <summary>
        /// Initializes a new instance of the <see cref="MvcPartialJsonFields"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The <see cref="IHttpContextAccessor"/></param>
        /// <param name="logger">The <see cref="ILogger{MvcPartialJsonFields}"/></param>
        /// <param name="options">The <see cref="IOptions{MvcPartialJsonOptions}"/></param>
        public MvcPartialJsonFields(IHttpContextAccessor httpContextAccessor, ILogger<MvcPartialJsonFields> logger, IOptions<MvcPartialJsonOptions> options)
        {
            this.HttpContextAccessor = httpContextAccessor;
            this.Logger = logger;
            this.Options = options;
        }

        /// <summary>
        /// Gets <see cref="IHttpContextAccessor"/>
        /// </summary>
        protected IHttpContextAccessor HttpContextAccessor { get; }

        /// <summary>
        /// Gets <see cref="ILogger{MvcPartialJsonFields}"/>
        /// </summary>
        protected ILogger<MvcPartialJsonFields> Logger { get; }

        /// <summary>
        /// Gets <see cref="IOptions{MvcPartialJsonOptions}"/>
        /// </summary>
        protected IOptions<MvcPartialJsonOptions> Options { get; }

        /// <summary>
        /// Gets the fields parsing results
        /// </summary>
        /// <returns>The fields parsing results</returns>
        public MvcPartialJsonFieldsResult GetFieldsResult()
        {
            MvcPartialJsonFieldsResult result = null;
            var request = this.HttpContextAccessor.HttpContext.Request;

            if (!this.HttpContextAccessor.HttpContext.Items.ContainsKey(KeyContextItems))
            {
                result = new MvcPartialJsonFieldsResult
                {
                    IsPresent = request.Query.ContainsKey(this.Options.Value.FieldsParamName),
                    IsError = false
                };
                if (result.IsPresent)
                {
                    result.IsError = !Fields.TryParse(request.Query[this.Options.Value.FieldsParamName][0], out var fields);

                    if (!result.IsError)
                    {
                        result.Fields = fields;
                    }
                    else
                    {
                        this.Logger.LogWarning("Failed to parse fields for partial response");
                    }
                }

                this.HttpContextAccessor.HttpContext.Items.Add(KeyContextItems, result);
            }
            else
            {
                result = (MvcPartialJsonFieldsResult)this.HttpContextAccessor.HttpContext.Items[KeyContextItems];
            }

            return result;
        }
    }
}