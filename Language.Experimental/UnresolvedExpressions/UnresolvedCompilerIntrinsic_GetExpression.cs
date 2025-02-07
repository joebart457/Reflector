using Language.Experimental.Expressions;
using Language.Experimental.Parser;
using System.Linq.Expressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedExpressions;

internal class UnresolvedCompilerIntrinsic_GetExpression : UnresolvedExpressionBase
{
    public TypeSymbol RetrievedType { get; set; }
    public UnresolvedExpressionBase ContextPointer { get; set; }
    public int MemberOffset { get; set; }
    public UnresolvedCompilerIntrinsic_GetExpression(IToken token, TypeSymbol retrievedType, UnresolvedExpressionBase contextPointer, int memberOffset) : base(token)
    {
        RetrievedType = retrievedType;
        ContextPointer = contextPointer;
        MemberOffset = memberOffset;
    }

    public override ExpressionBase Resolve(TypeSymbolResolver typeSymbolResolver)
    {
        return typeSymbolResolver.Resolve(this);
    }

    public override UnresolvedExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new UnresolvedCompilerIntrinsic_GetExpression(Token, RetrievedType.ReplaceGenericTypeParameter(genericToConcreteTypeMap), ContextPointer.ReplaceGenericTypeSymbols(genericToConcreteTypeMap), MemberOffset);
    }
}