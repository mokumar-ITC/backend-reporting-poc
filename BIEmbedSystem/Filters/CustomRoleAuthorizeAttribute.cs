using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Linq;

public class CustomRoleAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
{
    private readonly string[] _roles;

    public CustomRoleAuthorizeAttribute(params string[] roles)
    {
        _roles = roles;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        if (user == null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Check if user has at least one required role
        if (!_roles.Any(role => user.IsInRole(role)))
        {
            context.Result = new ForbidResult();
        }
    }
}
