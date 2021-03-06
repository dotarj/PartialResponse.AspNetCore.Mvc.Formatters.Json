// Copyright (c) Arjen Post and contributors. See LICENSE and NOTICE in the project root for license information.

using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Internal
{
    /// <summary>
    /// Provides a method for serializing objects to JSON.
    /// </summary>
    /// <remarks>This type supports the <see cref="PartialResponse.Core.Fields"/> infrastructure and is not intended to be used directly
    /// from your code.</remarks>
    public static class JsonSerializerExtensions
    {
        /// <summary>
        /// Serializes the specified <see cref="object"/> and writes the JSON structure
        /// using the specified <see cref="JsonWriter"/>.
        /// </summary>
        /// <param name="jsonSerializer">The <see cref="JsonSerializer"/> used to serialize the specified <see cref="object"/>.</param>
        /// <param name="jsonWriter">The <see cref="JsonWriter"/> used to write the JSON structure.</param>
        /// <param name="value">The <see cref="object"/> to serialize.</param>
        /// <param name="shouldSerialize">A <see cref="Func{T, TResult}"/> that is called for every field in the
        /// <see cref="object"/> to serialize, indicating whether the field should be serialized.</param>
        public static void Serialize(this JsonSerializer jsonSerializer, JsonWriter jsonWriter, object value, Func<string, bool> shouldSerialize)
        {
            if (value == null)
            {
                jsonSerializer.Serialize(jsonWriter, value);
            }
            else
            {
                var context = new SerializerContext(shouldSerialize);
                var token = JToken.FromObject(value, jsonSerializer);

                if (token is JArray array)
                {
                    RemoveArrayElements(array, null, context);

                    array.WriteTo(jsonWriter);
                }
                else
                {
                    if (token is JObject @object)
                    {
                        RemoveObjectProperties(@object, null, context);

                        @object.WriteTo(jsonWriter);
                    }
                    else
                    {
                        token.WriteTo(jsonWriter);
                    }
                }
            }
        }

        private static void RemoveArrayElements(JArray array, string currentPath, SerializerContext context)
        {
            var containsChildItems = array.Count > 0;

            array.OfType<JObject>()
                .ToList()
                .ForEach(childObject => RemoveObjectProperties(childObject, currentPath, context));

            // PartialResponse should only remove an array when it initially contained items, but was empty after filtering.
            if (containsChildItems)
            {
                RemoveArrayIfEmpty(array);
            }
        }

        private static void RemoveArrayIfEmpty(JArray array)
        {
            if (array.Count == 0)
            {
                if (array.Parent is JProperty && array.Parent.Parent != null)
                {
                    array.Parent.Remove();
                }
                else if (array.Parent is JArray)
                {
                    array.Remove();
                }
            }
        }

        private static void RemoveObjectProperties(JObject @object, string currentPath, SerializerContext context)
        {
            @object.Properties()
                .Where(property =>
                {
                    var path = CombinePath(currentPath, property.Name);

                    return !context.ShouldSerialize(path);
                })
                .ToList()
                .ForEach(property => property.Remove());

            @object.Properties()
                .Where(property => property.Value is JObject)
                .ToList()
                .ForEach(property =>
                {
                    var path = CombinePath(currentPath, property.Name);

                    RemoveObjectProperties((JObject)property.Value, path, context);
                });

            @object.Properties()
                .Where(property => property.Value is JArray)
                .ToList()
                .ForEach(property =>
                {
                    var path = CombinePath(currentPath, property.Name);

                    RemoveArrayElements((JArray)property.Value, path, context);
                });

            RemoveObjectIfEmpty(@object);
        }

        private static void RemoveObjectIfEmpty(JObject @object)
        {
            if (!@object.Properties().Any())
            {
                if (@object.Parent is JProperty && @object.Parent.Parent != null)
                {
                    @object.Parent.Remove();
                }
                else if (@object.Parent is JArray)
                {
                    @object.Remove();
                }
            }
        }

        private static string CombinePath(string path, string name)
        {
            if (string.IsNullOrEmpty(path))
            {
                return name;
            }

            return $"{path}/{name}";
        }
    }
}
