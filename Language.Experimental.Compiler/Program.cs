


using CliParser;
using Language.Experimental.Compiler.Services;
using Logger;

var startupService = new StartupService();


args = [
    "C:\\Users\\Jimmy\\Desktop\\Repositories\\FunctionLang\\Language.Experimental.Tests\\test.txt",
    "C:\\Users\\Jimmy\\Desktop\\Repositories\\FunctionLang\\Language.Experimental.Tests\\test.exe",
    "-n", "10",
    "-a", "C:\\Users\\Jimmy\\Desktop\\Repositories\\FunctionLang\\Language.Experimental.Tests\\test.asm",
    "-sc"
    ];


return args.ResolveWithTryCatch(startupService, -1, ex =>
   CliLogger.LogError(ex.InnerException?.Message ?? $"fatal error: {ex.Message}")
);
