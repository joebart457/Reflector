using CliParser;
using Language.Experimental.Services;
using Logger;

var startupService = new StartupService();


return args.ResolveWithTryCatch(startupService, -1, ex =>
{
    CliLogger.LogError(ex.InnerException?.Message ?? $"fatal error: {ex.Message}");
});