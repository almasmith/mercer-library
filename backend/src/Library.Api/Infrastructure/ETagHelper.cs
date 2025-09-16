using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Library.Api.Infrastructure
{
    internal static class ETagHelper
    {
        public static string ToStrongEtag(byte[] rowVersion)
        {
            var bytes = rowVersion ?? Array.Empty<byte>();
            var base64 = Convert.ToBase64String(bytes);
            return $"\"{base64}\"";
        }

        public static bool IfNoneMatchSatisfied(HttpRequest req, string currentEtag)
        {
            if (req == null)
            {
                throw new ArgumentNullException(nameof(req));
            }

            if (string.IsNullOrWhiteSpace(currentEtag))
            {
                return false;
            }

            StringValues headerValues = req.Headers[HeaderNames.IfNoneMatch];
            if (StringValues.IsNullOrEmpty(headerValues))
            {
                return false;
            }

            // Fast path for wildcard
            foreach (var raw in headerValues)
            {
                if (string.Equals(raw?.Trim(), "*", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            // Parse header values into entity tags
            if (!EntityTagHeaderValue.TryParse(currentEtag, out var current))
            {
                return false;
            }

            if (EntityTagHeaderValue.TryParseList(headerValues, out var parsedEtags))
            {
                // RFC 7232: For GET/HEAD, If-None-Match uses weak comparison. We accept either strong or weak match on the tag value.
                var currentTagValue = current.Tag.Value;
                foreach (var tag in parsedEtags)
                {
                    if (string.Equals(tag.Tag.Value, currentTagValue, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}


