using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.FeatureManagement.FeatureFilters;

namespace AspNetCoreFeatureManagement
{
    public static class ClaimTypes
    {
        public const string GroupName = "http://schemas.featureflagdemo.featuremanagement.microsoft.com/claims/groupname";
    }

    public class HttpContextTargetingContextAccessor : ITargetingContextAccessor
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public HttpContextTargetingContextAccessor(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public ValueTask<TargetingContext> GetContextAsync()
        {
            var httpContext = this.httpContextAccessor.HttpContext;

            return new ValueTask<TargetingContext>(
                new TargetingContext
                {
                    UserId = string.IsNullOrEmpty(httpContext.User.Identity.Name)
                                 ? Guid.NewGuid().ToString()
                                 : httpContext.User.Identity.Name,
                    Groups = httpContext.User.Claims.Where(x => x.Type == ClaimTypes.GroupName).Select(x => x.Value).ToList()
                });
        }
    }
}