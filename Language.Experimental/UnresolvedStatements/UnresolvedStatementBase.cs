using Language.Experimental.Parser;
using Language.Experimental.Statements;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedStatements;
public abstract class UnresolvedStatementBase
{
    public IToken Token { get; set; }

    public UnresolvedStatementBase(IToken token)
    {
        Token = token;
    }
    public abstract StatementBase Resolve(TypeSymbolResolver typeSymbolResolver);
}