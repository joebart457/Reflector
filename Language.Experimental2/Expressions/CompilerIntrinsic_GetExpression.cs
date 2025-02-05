using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

internal class CompilerIntrinsic_GetExpression : ExpressionBase
{
    public IdentifierExpression ContextIdentifier { get; set; }
    public int MemberOffset { get; set; }
    public CompilerIntrinsic_GetExpression(IToken token, IdentifierExpression contextIdentifier, int memberOffset) : base(token)
    {
        ContextIdentifier = contextIdentifier;
        MemberOffset = memberOffset;
    }
}