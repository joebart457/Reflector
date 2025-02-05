using Language.Experimental.Models;

namespace Language.Experimental.TypedExpressions;

internal class TypedCompilerIntrinsic_GetExpression : TypedExpression
{
    public TypedIdentifierExpression ContextIdentifier { get; set; }
    public int MemberOffset { get; set; }
    public TypedCompilerIntrinsic_GetExpression(TypeInfo typeInfo, ExpressionBase originalExpression, TypedIdentifierExpression contextIdentifier, int memberOffset) : base(typeInfo, originalExpression)
    {
        ContextIdentifier = contextIdentifier;
        MemberOffset = memberOffset;
    }
}