// Copyright (c) Arjen Post and contributors. See LICENSE and NOTICE in the project root for license information.

#if !ASPNETCORE2
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ReaderFunc = System.Func<System.Collections.Generic.IAsyncEnumerable<object>, System.Threading.Tasks.Task<System.Collections.ICollection>>;

namespace PartialResponse.AspNetCore.Mvc.Formatters.Json.Internal
{
    /// <summary>
    /// Type that reads an <see cref="IAsyncEnumerable{T}"/> instance into a
    /// generic collection instance.
    /// </summary>
    /// <remarks>
    /// This type is used to create a strongly typed synchronous <see cref="ICollection{T}"/> instance from
    /// an <see cref="IAsyncEnumerable{T}"/>. An accurate <see cref="ICollection{T}"/> is required for XML formatters to
    /// correctly serialize.
    /// </remarks>
    internal sealed class AsyncEnumerableReader
    {
        private readonly MethodInfo converter = typeof(AsyncEnumerableReader).GetMethod(nameof(ReadInternal), BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly ConcurrentDictionary<Type, ReaderFunc> asyncEnumerableConverters = new ConcurrentDictionary<Type, ReaderFunc>();
        private readonly MvcOptions mvcOptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncEnumerableReader"/> class.
        /// </summary>
        /// <param name="mvcOptions">Accessor to <see cref="MvcOptions"/>.</param>
        public AsyncEnumerableReader(MvcOptions mvcOptions)
        {
            this.mvcOptions = mvcOptions;
        }

        /// <summary>
        /// Reads a <see cref="IAsyncEnumerable{T}"/> into an <see cref="ICollection{T}"/>.
        /// </summary>
        /// <param name="value">The <see cref="IAsyncEnumerable{T}"/> to read.</param>
        /// <returns>The <see cref="ICollection"/>.</returns>
        public Task<ICollection> ReadAsync(IAsyncEnumerable<object> value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var type = value.GetType();
            if (!this.asyncEnumerableConverters.TryGetValue(type, out var result))
            {
                var enumerableType = ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IAsyncEnumerable<>));

                var enumeratedObjectType = enumerableType.GetGenericArguments()[0];

                var converter = (ReaderFunc)this.converter
                    .MakeGenericMethod(enumeratedObjectType)
                    .CreateDelegate(typeof(ReaderFunc), this);

                this.asyncEnumerableConverters.TryAdd(type, converter);
                result = converter;
            }

            return result(value);
        }

        private async Task<ICollection> ReadInternal<T>(IAsyncEnumerable<object> value)
        {
            var asyncEnumerable = (IAsyncEnumerable<T>)value;
            var result = new List<T>();
            var count = 0;

            await foreach (var item in asyncEnumerable)
            {
                if (count++ >= this.mvcOptions.MaxIAsyncEnumerableBufferLimit)
                {
                    throw new InvalidOperationException("Resources.FormatObjectResultExecutor_MaxEnumerationExceeded(nameof(AsyncEnumerableReader), value.GetType())");
                }

                result.Add(item);
            }

            return result;
        }
    }
}
#endif
