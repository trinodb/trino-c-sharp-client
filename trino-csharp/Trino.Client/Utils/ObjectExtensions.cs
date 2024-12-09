using System;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace Trino.Client.Utils
{
    internal static class ObjectExtensions
    {
        internal static T IsNullArgument<T>(this T value, string arg)
        {
            if (value == null)
            {
                throw new ArgumentNullException(arg);
            }
            return value;
        }

        internal static IEnumerable<string> GetValuesOrEmpty(this HttpResponseHeaders headers, string headerName)
        {
            if (headers.TryGetValues(headerName, out IEnumerable<string> values))
            {
                return values;
            }
            return new List<string>();
        }
    }
}
