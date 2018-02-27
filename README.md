# ASP.NET Core MVC Partial Response

[![apache](https://img.shields.io/badge/license-Apache%202-green.svg)](https://raw.githubusercontent.com/dotarj/PartialResponse.AspNetCore.Mvc.Formatters.Json/master/LICENSE)
[![nuget](https://img.shields.io/nuget/v/PartialResponse.AspNetCore.Mvc.Formatters.Json.svg)](https://www.nuget.org/packages/PartialResponse.AspNetCore.Mvc.Formatters.Json)
[![myget](https://img.shields.io/myget/partialresponse/v/PartialResponse.AspNetCore.Mvc.Formatters.Json.svg)](https://www.myget.org/feed/partialresponse/package/nuget/PartialResponse.AspNetCore.Mvc.Formatters.Json)
[![Build status](https://ci.appveyor.com/api/projects/status/y8kahoej4avaqwwm?svg=true)](https://ci.appveyor.com/project/dotarj/partialresponse-aspnetcore-mvc-formatters-json)
[![codecov](https://codecov.io/gh/dotarj/PartialResponse.AspNetCore.Mvc.Formatters.Json/branch/master/graph/badge.svg)](https://codecov.io/gh/dotarj/PartialResponse.AspNetCore.Mvc.Formatters.Json)

PartialResponse.AspNetCore.Mvc.Formatters.Json provides JSON partial response (partial resource) support for ASP.NET Core MVC. This package is also [available for ASP.NET Web API](https://github.com/dotarj/PartialResponse/).

## Getting started

First, add a dependency to PartialResponse.AspNetCore.Mvc.Formatters.Json using the NuGet package manager (console) or by adding a package reference to the .csproj:

```xml
<ItemGroup>
  <PackageReference Include="PartialResponse.AspNetCore.Mvc.Formatters.Json" Version="x.x.x" />
</ItemGroup>
```

Then, remove the `JsonOutputFormatter` from the output formatters and add the `PartialJsonOutputFormatter`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddMvc(options => options.OutputFormatters.RemoveType<JsonOutputFormatter>())
        .AddPartialJsonFormatters();
}
```

The `fields` parameter value, which is used to filter the API response, is case-sensitive by default, but this can be changed using the `MvcPartialJsonOptions`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.Configure<MvcPartialJsonOptions>(options => options.IgnoreCase = true);
}
```

That's it!

## Understanding the fields parameter

The `fields` parameter filters the API response so that the response only includes a specific set of fields. The `fields` parameter lets you remove nested properties from an API response and thereby reduce your bandwidth usage.

The following rules explain the supported syntax for the `fields` parameter value, which is loosely based on XPath syntax:

* Use a comma-separated list (`fields=a,b`) to select multiple fields.
* Use an asterisk (`fields=*`) as a wildcard to identify all fields.
* Use parentheses (`fields=a(b,c)`) to specify a group of nested properties that will be included in the API response.
* Use a forward slash (`fields=a/b`) to identify a nested property.

In practice, these rules often allow several different `fields` parameter values to retrieve the same API response. For example, if you want to retrieve the playlist item ID, title, and position for every item in a playlist, you could use any of the following values:

* `fields=items/id,playlistItems/snippet/title,playlistItems/snippet/position`
* `fields=items(id,snippet/title,snippet/position)`
* `fields=items(id,snippet(title,position))`

**Note:** As with all query parameter values, the 'fields' parameter value must be URL encoded. For better readability, the examples in this document omit the encoding.

**Note:** Due to the relatively slow performance of LINQ to JSON (Json.NET), the usage of PartialJsonOutputFormatter has a performance impact compared to the regular Json.NET serializer. Because of the reduced traffic, the overhead in time could be neglected.