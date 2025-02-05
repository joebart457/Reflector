
using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.TypedStatements;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Statements;

public abstract class StatementBase
{
    public IToken Token { get; set; }

    public StatementBase(IToken token)
    {
        Token = token;
    }

    public abstract void GatherSignature(TypeResolver typeResolver);
    public abstract TypedStatement Resolve(TypeResolver typeResolver);
}