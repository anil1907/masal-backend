using Core.CrossCuttingConcerns.Logging.Configurations;
using Serilog;

namespace Core.CrossCuttingConcerns.Logging.SeriLog.SeqLog;

public class SerilogSeqLogger : SerilogLoggerServiceBase
{
    public SerilogSeqLogger(SeqLogConfiguration configuration)
        : base(logger: null!)
    {
        Logger = new LoggerConfiguration()
            .WriteTo.Seq(configuration.Url, apiKey: configuration.Key)
            .MinimumLevel.Information()
            .Enrich.WithProperty("Environment", configuration.Environment)
            .Enrich.WithProperty("Application", configuration.Application)
            .CreateLogger();
    }
}