


using CliParser;
using Language.Experimental.Compiler.Services;
using Logger;

var startupService = new StartupService();


//args = [
//    "C:\\Users\\Jimmy\\Desktop\\Repositories\\FunctionLang\\Language.Experimental.Tests\\test.txt",
//    "C:\\Users\\Jimmy\\Desktop\\Repositories\\FunctionLang\\Language.Experimental.Tests\\test.exe",
//    "-n", "10",
//    "-a", "C:\\Users\\Jimmy\\Desktop\\Repositories\\FunctionLang\\Language.Experimental.Tests\\test.asm",
//    "-sc"
//    ];

args = [

    "-i", "c:\\Users\\Jimmy\\Downloads\\type test.abc", "-o", "out.exe", "-q", "-a", "out.asm"

    ];

//args = [
//    "fasm",
//    "C:\\Users\\Jimmy\\Desktop\\Repositories\\FunctionLang\\Language.Experimental.Compiler\\bin\\x86\\Debug\\net8.0\\win-x86\\out.asm",
//    "C:\\Users\\Jimmy\\Desktop\\Repositories\\FunctionLang\\Language.Experimental.Compiler\\bin\\x86\\Debug\\net8.0\\win-x86\\out.exe"
//    ];

return args.ResolveWithTryCatch(startupService, -1, ex =>
   CliLogger.LogError(ex.InnerException?.Message ?? $"fatal error: {ex.Message}")
);
