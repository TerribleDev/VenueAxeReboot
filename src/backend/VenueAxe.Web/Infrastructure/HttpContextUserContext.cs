using System;
using Microsoft.AspNetCore.Http;
using VenueAxe.Domain.Common;

namespace VenueAxe.Web.Infrastructure;

public class HttpContextUserContext : UserContext
{
    public HttpContextUserContext(IHttpContextAccessor httpContextAccessor)
    {
        var context = httpContextAccessor.HttpContext;
        if (context?.User != null)
        {
            PopulateFromClaims(context.User);
        }
    }
}
