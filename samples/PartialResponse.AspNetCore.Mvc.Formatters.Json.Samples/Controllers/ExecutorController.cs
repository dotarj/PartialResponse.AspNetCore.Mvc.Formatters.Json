// Copyright (c) Arjen Post. See LICENSE and NOTICE in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PartialResponse.Extensions.DependencyInjection;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Samples.Controllers
{
    public class ExecutorController : Controller
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutorController"/> class.
        /// </summary>
        /// <param name="fieldsParser">The fields parser.</param>
        public ExecutorController(IFieldsParser fieldsParser)
        {
            this.FieldsParser = fieldsParser;
        }

        /// <summary>
        /// Gets the <see cref="FieldsParser"/>
        /// </summary>
        protected IFieldsParser FieldsParser { get; }

        public IActionResult Index([FromServices]IOptions<MvcPartialJsonOptions> options)
        {
            // This one computes the fields
            var fields1 = this.FieldsParser.Parse(this.Request);

            // This one gets the fields from cache
            var fields2 = this.FieldsParser.Parse(this.Request);
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
                        Fields = fields1.IsFieldsSet ? string.Join(",", fields1.Fields.Values) : null,
                        fields1.HasError,
                        fields1.IsFieldsSet
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
                        Fields = fields2.IsFieldsSet ? string.Join(",", fields2.Fields.Values) : null,
                        fields2.HasError,
                        fields2.IsFieldsSet
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
                    Fields = (object)null
                }
            };

            return this.PartialJson(response);
        }
    }
}
