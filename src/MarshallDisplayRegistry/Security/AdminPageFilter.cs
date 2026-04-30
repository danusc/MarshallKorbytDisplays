using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MarshallDisplayRegistry.Security;

public sealed class AdminPageFilter(AdminAuthService adminAuthService) : IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var path = context.HttpContext.Request.Path;
        if (path.StartsWithSegments("/Error") || adminAuthService.IsAdmin(context.HttpContext))
        {
            await next();
            return;
        }

        context.Result = new ContentResult
        {
            StatusCode = StatusCodes.Status403Forbidden,
            Content = "You are signed in, but you are not in the approved Marshall Korbyt Display admin group."
        };
    }
}
