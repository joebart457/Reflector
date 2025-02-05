using Language.Experimental.Compiler;

namespace Language.Experimental.TypedStatements;

public abstract class TypedStatement
{
    public abstract void Compile(X86CompilationContext cc);
}