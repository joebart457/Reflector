using Language.Experimental.Compiler;
using Language.Experimental.Statements;

namespace Language.Experimental.TypedStatements;

public abstract class TypedStatement
{
    public StatementBase OriginalStatement { get; set; }

    protected TypedStatement(StatementBase originalStatement)
    {
        OriginalStatement = originalStatement;
    }

    public abstract void Compile(X86CompilationContext cc);
}