using CliParser;
using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Parser;
using Language.Experimental.Services;
using Logger;
using System.Text;

var startupService = new StartupService();


args = [
    "C:\\Users\\Jimmy\\Desktop\\Repositories\\FunctionLang\\Language.Experimental.Tests\\test.txt",
    "C:\\Users\\Jimmy\\Desktop\\Repositories\\FunctionLang\\Language.Experimental.Tests\\test.exe",
    "-n", "10",
    "-a", "C:\\Users\\Jimmy\\Desktop\\Repositories\\FunctionLang\\Language.Experimental.Tests\\test.asm",
    "-sc"
    ];




var methods = typeof(X86Instructions).GetMethods(System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).ToList();

var map = new Dictionary<Type, InstructionParseUnit>();
map[typeof(SymbolOffset)] = InstructionParseUnit.SymbolOffset;
map[typeof(RegisterOffset)] = InstructionParseUnit.RegisterOffset;
map[typeof(X86Register)] = InstructionParseUnit.GeneralRegister32;
map[typeof(XmmRegister)] = InstructionParseUnit.XmmRegister;
map[typeof(int)] = InstructionParseUnit.Immediate;
map[typeof(string)] = InstructionParseUnit.Symbol;
map[typeof(SymbolOffset_Byte)] = InstructionParseUnit.SymbolOffset_Byte;
map[typeof(RegisterOffset_Byte)] = InstructionParseUnit.RegisterOffset_Byte;
map[typeof(X86ByteRegister)] = InstructionParseUnit.ByteRegister;



foreach (var method in methods)
{
    /*
        new(AssemblyInstruction.Mov, [InstructionParseUnit.SymbolOffset, InstructionParseUnit.GeneralRegister32], 
            (p) => { 
                var symbolOffset = p.ParseSymbolOffset(); 
                var generalRegister32 = p.ParseGeneralRegister32();
                return new Mov_SymbolOffset_Register(symbolOffset, generalRegister32);
            }),
     
     */
    Console.WriteLine($"\t\tnew(AssemblyInstruction.{method.Name}, [{string.Join(",", method.GetParameters().Select(x =>
    {
        if (!map.TryGetValue(x.ParameterType, out var instructionParseUnit)) return "AssemblyInstruction.Undefined";
        return $"InstructionParseUnit.{instructionParseUnit}";
    }))}],");
    Console.WriteLine("\t\t\t(p) => {");
    var pNames = new List<string>();
    foreach (var param in method.GetParameters())
    {
        if (!map.TryGetValue(param.ParameterType, out var instructionParseUnit))
        {
            Console.WriteLine("!");
            continue;
        }
        pNames.Add(param.Name ?? "!!!!!!!");
        Console.WriteLine($"\t\t\t\tvar {param.Name} = p.Parse{instructionParseUnit}();");
    }
    Console.WriteLine($"\t\t\t\treturn X86Instructions.{method.Name}({string.Join(",", pNames)});");

    Console.WriteLine("\t\t\t}),");
}

//eturn args.ResolveWithTryCatch(startupService, -1, ex =>
//
//   CliLogger.LogError(ex.InnerException?.Message ?? $"fatal error: {ex.Message}");
//);