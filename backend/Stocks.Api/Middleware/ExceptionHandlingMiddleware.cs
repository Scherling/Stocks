using Market.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Stocks.Api.Middleware;

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
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var (status, title) = ex switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation Error"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            InsufficientCreditsException => (StatusCodes.Status422UnprocessableEntity, "Insufficient Credits"),
            InsufficientInventoryException => (StatusCodes.Status422UnprocessableEntity, "Insufficient Inventory"),
            OrderNotFillableException => (StatusCodes.Status422UnprocessableEntity, "Order Not Fillable"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
        else
            logger.LogWarning("Domain error {Status}: {Message}", status, ex.Message);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = ex.Message,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem);
    }
}
