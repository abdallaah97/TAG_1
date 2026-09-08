using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace API.Middleware
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IMemoryCache _cache;
        private readonly int _limit;
        private readonly TimeSpan _window;

        public RateLimitMiddleware(RequestDelegate next, IMemoryCache cache, IConfiguration configuration)
        {
            _next = next;
            _cache = cache;
            _limit = configuration.GetValue("RateLimit:PermitLimit", 2);
            _window = TimeSpan.FromSeconds(configuration.GetValue("RateLimit:WindowSeconds", 60));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var key = $"ratelimit:{GetClientId(context)}";

            var counter = _cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _window;
                return new Counter();
            })!;

            if (Interlocked.Increment(ref counter.Value) > _limit)
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;

                await context.Response.WriteAsJsonAsync(new
                {
                    status = (int)HttpStatusCode.TooManyRequests,
                    message = "Too many requests, slow down."
                });

                return;
            }

            await _next(context);
        }
        private static string GetClientId(HttpContext context)
        {
            return context.User.FindFirst("id")?.Value
                   ?? context.Connection.RemoteIpAddress?.ToString()
                   ?? "unknown";
        }

        private sealed class Counter
        {
            public int Value;
        }
    }
}
