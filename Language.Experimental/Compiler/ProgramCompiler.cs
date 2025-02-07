using Language.Experimental.Compiler.CodeGenerator.Fasm;
using Language.Experimental.Compiler.Optimizer;
using Language.Experimental.Parser;
using Language.Experimental.TypedStatements;

namespace Language.Experimental.Compiler;

public class X86ProgramCompiler
{
    private readonly ProgramParser _parser = new();
    private readonly TypeSymbolResolver _typeSymbolResolver = new();
    private readonly TypeResolver.TypeResolver _typeResolver = new();
    private readonly X86AssemblyOptimizer _optimizer = new();
    public string? EmitBinary(CompilationOptions compilationOptions)
    {
        var result = Compile(compilationOptions);
        return X86CodeGenerator.Generate(result);
    }

    public CompilationResult Compile(CompilationOptions options)
    {
        var unresolvedParserResult = _parser.ParseFile(options.InputPath, out var errors);
        if (errors.Any()) throw new AggregateException(errors);
        var parserResult = _typeSymbolResolver.Resolve(unresolvedParserResult);
        var resolverResult = _typeResolver.Resolve(parserResult);
        return Compile(resolverResult.ToList(), options);
    }

    public CompilationResult Compile(List<TypedStatement> resolverResult, CompilationOptions options)
    {
        var context = new X86CompilationContext(options);
        resolverResult.ForEach(x => x.Compile(context));
        var result = new CompilationResult(context);
        if (options.EnableOptimizations)
            _optimizer.Optimize(result);
        return result;
    }
}