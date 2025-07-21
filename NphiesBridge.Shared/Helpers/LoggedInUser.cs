using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;

namespace NphiesBridge.Shared.Helpers
{
    public static class LoggedInUserHelper
    {
        public static Guid GetCurrentHealthProviderId(HttpContext httpContext)
        {
            var userSession = httpContext.Session.GetString("ProviderCurrentUser");

            if (!string.IsNullOrEmpty(userSession))
            {
                var jObj = JObject.Parse(userSession);
                var idString = jObj["HealthProviderId"]?.ToString();

                if (Guid.TryParse(idString, out Guid sessionProviderId))
                {
                    return sessionProviderId;
                }
            }

            var userIdClaim = httpContext.User.FindFirst("HealthProviderId")?.Value;
            if (Guid.TryParse(userIdClaim, out Guid claimProviderId))
            {
                return claimProviderId;
            }

            // Default fallback
            return Guid.Parse("00000000-0000-0000-0000-000000000001");
        }
    }
}
