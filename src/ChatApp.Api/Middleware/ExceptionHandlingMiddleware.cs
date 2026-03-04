using ChatApp.Domain.Exceptions;
using FluentValidation;
using System;
using System.Net;
using System.Text.Json;

namespace ChatApp.Api.Middleware;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Validation exception: {Errors}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            var errors = ex.Errors.Select(e => e.ErrorMessage);
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { errors }));
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Domain exception: {Message}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var isDevelopment = environment.IsDevelopment();

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "An unexpected error occurred.",
                detail = isDevelopment ? ex.Message : null,
                stackTrace = isDevelopment ? ex.StackTrace : null
            }));
        }
    }
}
