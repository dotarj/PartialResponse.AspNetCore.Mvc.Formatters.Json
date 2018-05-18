// Copyright (c) Arjen Post. See LICENSE and NOTICE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PartialResponse.AspNetCore.Mvc.Formatters.Json;

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
        public MvcPartialJsonFields(IHttpContextAccessor httpContextAccessor, ILogger<MvcPartialJsonFields> logger)
        {
            this.HttpContextAccessor = httpContextAccessor;
            this.Logger = logger;
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
        /// Gets the fields parsing results
        /// </summary>
        /// <returns>The fields parsing results</returns>
        public MvcPartialJsonFieldsResult GetFieldsResult()
        {
            MvcPartialJsonFieldsResult result = null;
            if (!this.HttpContextAccessor.HttpContext.Items.ContainsKey(KeyContextItems))
            {
                result = new MvcPartialJsonFieldsResult();
                if (!this.HttpContextAccessor.HttpContext.Request.TryGetFields(out var fields))
                {
                    result.IsPresent = true;
                    result.IsError = true;

                    this.Logger.LogWarning("Failed to parse fields for partial response");
                }
                else
                {
                    if (fields != null)
                    {
                        result.Fields = fields.Value;
                    }

                    result.IsPresent = fields != null;
                    result.IsError = false;
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