using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.Models;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

internal class CompilerIntrinsic_GetExpression : ExpressionBase
{
    public TypeInfo RetrievedType{ get; set; }
    public ExpressionBase ContextPointer { get; set; }
    public int MemberOffset { get; set; }
    public CompilerIntrinsic_GetExpression(IToken token, TypeInfo retrievedType, ExpressionBase contextPointer, int memberOffset) : base(token)
    {
        RetrievedType = retrievedType;
        ContextPointer = contextPointer;
        MemberOffset = memberOffset;
    }

    public override TypedExpression Resolve(TypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }
}