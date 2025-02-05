
using TokenizerCore.Interfaces;

namespace Language.Experimental.Statements;

public class StatementBase
{
    public IToken Token { get; set; }

    public StatementBase(IToken token)
    {
        Token = token;
    }
}