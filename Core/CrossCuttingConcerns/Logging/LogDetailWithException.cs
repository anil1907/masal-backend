namespace Core.CrossCuttingConcerns.Logging;

public class LogDetailWithException : LogDetail
{
    public string ExceptionMessage { get; set; }

    public LogDetailWithException()
    {
        ExceptionMessage = string.Empty;
    }

    public LogDetailWithException(
        string fullName,
        string methodName,
        string user,
        string requestPath,
        List<LogParameter> parameters,
        string exceptionMessage
    )
        : base(fullName, methodName, user, requestPath, parameters)
    {
        ExceptionMessage = exceptionMessage;
    }
}