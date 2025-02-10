using Language.Experimental.Compiler.CodeGenerator.Fasm;
using Language.Experimental.Compiler.Optimizer;
using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.Parser;

namespace Language.Experimental.Compiler;

public class X86ProgramCompiler
{
    private readonly ProgramParser _parser = new();
    private readonly TypeResolver.TypeResolver _typeResolver = new();
    private readonly X86AssemblyOptimizer _optimizer = new();
    public string? EmitBinary(CompilationOptions compilationOptions)
    {
        var result = Compile(compilationOptions);
        return X86CodeGenerator.Generate(result);
    }

    public CompilationResult Compile(CompilationOptions options)
    {
        var parserResult = _parser.ParseFile(options.InputPath, out var errors);
        if (errors.Any()) throw new AggregateException(errors);
        var resolverResult = _typeResolver.Resolve(parserResult);
        return Compile(resolverResult, options);
    }

    public CompilationResult Compile(TypeResolverResult resolverResult, CompilationOptions options)
    {
        var context = new X86CompilationContext(options);
        resolverResult.ImportLibraries.ForEach(x => x.Compile(context));
        resolverResult.ImportedFunctions.ForEach(x => x.Compile(context));
        resolverResult.Functions.ForEach(x => x.Compile(context));
        var result = new CompilationResult(context);
        if (options.EnableOptimizations)
            _optimizer.Optimize(result);
        return result;
    }
}