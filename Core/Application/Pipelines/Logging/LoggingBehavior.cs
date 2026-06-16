using System.Diagnostics;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Core.Application.Pipelines.Logging;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ILoggableRequest
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoggingBehavior(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var httpContext = _httpContextAccessor.HttpContext;
        Guid transactionId = Guid.NewGuid();
        if (httpContext != null)
        {
            httpContext.Response.Headers["X-Transaction-Id"] = transactionId.ToString();
            httpContext.Items["TransactionId"] = transactionId;
        }

        string endpoint = httpContext?.Request.Path.ToString() ?? string.Empty;
        string queryString = httpContext?.Request.QueryString.ToString() ?? string.Empty;
        string body;
        string headers;
        string responseBody = string.Empty;
        try
        {
            body = JsonSerializer.Serialize(request);
        }
        catch (Exception)
        {
            body = string.Empty;
        }

        try
        {
            headers = httpContext != null
                ? JsonSerializer.Serialize(
                    httpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
                )
                : string.Empty;
        }
        catch (Exception)
        {
            headers = string.Empty;
        }

        Stopwatch sw = Stopwatch.StartNew();
        try
        {
            TResponse response = await next();
            sw.Stop();

            try
            {
                responseBody = JsonSerializer.Serialize(response);
            }
            catch (Exception)
            {
                responseBody = string.Empty;
            }

            Log
                .ForContext("CommandName", typeof(TRequest).Name)
                .ForContext("Endpoint", endpoint)
                .ForContext("QueryString", queryString)
                .ForContext("Body", body)
                .ForContext("Headers", headers)
                .ForContext("Response", responseBody)
                .ForContext("TransactionId", transactionId)
                .ForContext("ResponseTime", sw.Elapsed.TotalMilliseconds)
                .Information("{CommandName} | Endpoint: {Endpoint} | Transaction: {TransactionId} | Duration: {ResponseTime}ms", 
                    typeof(TRequest).Name, endpoint, transactionId, sw.Elapsed.TotalMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();

            Log
                .ForContext("CommandName", typeof(TRequest).Name)
                .ForContext("Endpoint", endpoint)
                .ForContext("QueryString", queryString)
                .ForContext("Body", body)
                .ForContext("Headers", headers)
                .ForContext("Response", responseBody)
                .ForContext("TransactionId", transactionId)
                .ForContext("ResponseTime", sw.Elapsed.TotalMilliseconds)
                .ForContext("Error", ex.Message)
                .Error(ex, "{CommandName} | Endpoint: {Endpoint} | Transaction: {TransactionId} | Duration: {ResponseTime}ms | Exception: {ExceptionType} | Message: {ErrorMessage}", 
                    typeof(TRequest).Name, endpoint, transactionId, sw.Elapsed.TotalMilliseconds, ex.GetType().Name, ex.Message);

            throw;
        }
    }
}
