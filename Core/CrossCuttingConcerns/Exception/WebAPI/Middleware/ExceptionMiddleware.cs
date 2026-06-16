using System.Net.Mime;
using Core.CrossCuttingConcerns.Exception.WebAPI.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Core.CrossCuttingConcerns.Exception.WebAPI.Middleware;

public class ExceptionMiddleware
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly HttpExceptionHandler _httpExceptionHandler;
    private readonly ILogger<ExceptionMiddleware> _loggerService;
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next, IHttpContextAccessor contextAccessor, ILogger<ExceptionMiddleware> loggerService)
    {
        _next = next;
        _contextAccessor = contextAccessor;
        _loggerService = loggerService;
        _httpExceptionHandler = new HttpExceptionHandler();
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (System.Exception exception)
        {
            await LogException(context, exception);
            await HandleExceptionAsync(context.Response, exception);
        }
    }

    protected virtual Task HandleExceptionAsync(HttpResponse response, dynamic exception)
    {
        response.ContentType = MediaTypeNames.Application.Json;
        _httpExceptionHandler.Response = response;

        return _httpExceptionHandler.HandleException(exception);
    }

    protected virtual async Task LogException(HttpContext context, System.Exception exception)
    {
        string requestBody = string.Empty;
        if (context.Request.ContentLength > 0)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        _loggerService.Log(LogLevel.Error,
            "Exception occurred. ExceptionType: {ExceptionType}, Message: {Message}, Path: {Path}, Method: {Method}, Query: {Query}, Body: {Body}, User: {User}, StackTrace: {StackTrace}",
            exception.GetType().Name,
            exception.Message,
            context.Request.Path,
            context.Request.Method,
            context.Request.QueryString.ToString(),
            requestBody,
            _contextAccessor.HttpContext?.User.Identity?.Name ?? "?",
            exception.StackTrace);
    }
}