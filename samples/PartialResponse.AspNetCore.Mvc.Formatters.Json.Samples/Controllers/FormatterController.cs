// Copyright (c) Arjen Post and contributors. See LICENSE and NOTICE in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Samples.Controllers
{
    public class FormatterController : Controller
    {
        public List<dynamic> Index()
        {
            return new List<dynamic>()
            {
                new
                {
                    Foo = 1,
                    Bar = new
                    {
                        Baz = 2,
                        Qux = 3
                    }
                },
                new
                {
                    Foo = 2,
                    Bar = new
                    {
                        Baz = 3,
                        Qux = 4
                    }
                },
                new
                {
                    Foo = 3,
                    Bar = new
                    {
                        Baz = 5,
                        Qux = 6
                    }
                }
            };
        }
    }
}
