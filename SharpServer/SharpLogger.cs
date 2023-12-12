using System.Reflection;
using DotNetEnv;
using Serilog;
using Serilog.Core;

namespace SharpServer;

public class SharpLogger
{
    public static Logger Initialize()
    {
        var logger = new LoggerConfiguration()
            .WriteTo
            .File($"Logs/{Assembly.GetExecutingAssembly().GetName().Name}.log")
            .WriteTo
            .Console();

        if (Env.GetBool("LOG_LEVEL_DEBUG", false))
            logger.MinimumLevel.Debug();
        else
            logger.MinimumLevel.Information();

        return logger.CreateLogger();
    }
}
