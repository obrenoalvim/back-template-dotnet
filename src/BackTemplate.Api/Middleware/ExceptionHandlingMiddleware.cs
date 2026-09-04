using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace BackTemplate.Api.Middleware;

/// <summary>
/// Converte exceções não tratadas em ProblemDetails (RFC 7807) em vez de vazar
/// stack trace pro cliente. Loga a exceção completa; devolve só o essencial.
/// </summary>
public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

            var problem = new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "An unexpected error occurred",
                Detail = context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                    ? ex.ToString()
                    : null,
            };

            context.Response.StatusCode = problem.Status.Value;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
