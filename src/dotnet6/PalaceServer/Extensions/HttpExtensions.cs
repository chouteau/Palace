using Microsoft.Extensions.Primitives;

namespace PalaceServer.Extensions
{
    public static class HttpExtensions
    {
        internal static string GetUserHostAddress(this HttpContext context)
        {
            string userHostAddress;
            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out StringValues value))
            {
                userHostAddress = value.FirstOrDefault();
            }
            else if (context.Request.Headers.TryGetValue("X-Remote-Ip", out value))
            {
                userHostAddress = value.FirstOrDefault();
            }
            else
            {
                userHostAddress = context.Connection.RemoteIpAddress.ToString();
            }

            return userHostAddress;
        }

        internal static string GetUserAgent(this HttpContext httpContext)
        {
            var ua = httpContext.Request.Headers["UserAgent"].FirstOrDefault();
            return ua;
        }

    }
}
