using Language.Experimental.Interfaces;
using Language.Experimental.TypedStatements;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Statements;

public abstract class StatementBase
{
    public IToken Token { get; set; }
    private IToken? _startToken = null;
    private IToken? _endToken = null;
    public IToken StartToken { get => _startToken ?? throw new NullReferenceException(nameof(StartToken)); set => _startToken = value; }
    public IToken EndToken { get => _endToken ?? throw new NullReferenceException(nameof(EndToken)); set => _endToken = value; }
    public StatementBase(IToken token)
    {
        Token = token;
    }

    public abstract void GatherSignature(ITypeResolver typeResolver);
    public abstract TypedStatement Resolve(ITypeResolver typeResolver);
}