// Copyright (c) Arjen Post. See LICENSE and NOTICE in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using PartialResponse.Extensions.DependencyInjection;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Samples.Controllers
{
    public class ExecutorController : Controller
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutorController"/> class.
        /// </summary>
        /// <param name="mvcPartialJsonFields"></param>
        public ExecutorController(MvcPartialJsonFields mvcPartialJsonFields)
        {
            this.MvcPartialJsonFields = mvcPartialJsonFields;
        }

        /// <summary>
        /// Gets the <see cref="MvcPartialJsonFields"/>
        /// </summary>
        protected MvcPartialJsonFields MvcPartialJsonFields { get; }

        public IActionResult Index()
        {
            // This one computes the fields
            var fields1 = this.MvcPartialJsonFields.GetFieldsResult();

            // This one gets the fields from cache
            var fields2 = this.MvcPartialJsonFields.GetFieldsResult();
            var response = new List<dynamic>()
            {
                new
                {
                    Foo = 1,
                    Bar = new
                    {
                        Baz = 2,
                        Qux = 3
                    },
                    Fields = new
                    {
                        Fields = fields1.IsPresent ? string.Join(",", fields1.Fields.Values) : null,
                        fields1.IsError,
                        fields1.IsPresent
                    }
                },
                new
                {
                    Foo = 2,
                    Bar = new
                    {
                        Baz = 3,
                        Qux = 4
                    },
                    Fields = new
                    {
                        Fields = fields2.IsPresent ? string.Join(",", fields2.Fields.Values) : null,
                        fields2.IsError,
                        fields2.IsPresent
                    }
                },
                new
                {
                    Foo = 3,
                    Bar = new
                    {
                        Baz = 5,
                        Qux = 6
                    },
                    Fields = (MvcPartialJsonFields)null
                }
            };

            return this.PartialJson(response);
        }
    }
}
