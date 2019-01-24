// Copyright (c) Arjen Post and contributors. See LICENSE and NOTICE in the project root for license information.

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Samples
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build()
                .Run();
        }
    }
}
