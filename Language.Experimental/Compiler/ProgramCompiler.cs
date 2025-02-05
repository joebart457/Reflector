using Language.Experimental.Compiler.CodeGenerator.Fasm;
using Language.Experimental.Compiler.Optimizer;
using Language.Experimental.Parser;
using Language.Experimental.TypedStatements;

namespace Language.Experimental.Compiler;

public class X86ProgramCompiler
{
    private readonly ProgramParser _parser = new();
    private readonly TypeResolver.TypeResolver _resolver = new();
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
        var resolverResult = _resolver.Resolve(parserResult);
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