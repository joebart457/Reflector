﻿using Language.Experimental.Interfaces;
using Language.Experimental.Parser;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public class CompilerIntrinsic_GetExpression : ExpressionBase
{
    public TypeSymbol RetrievedType { get; set; }
    public ExpressionBase ContextPointer { get; set; }
    public int MemberOffset { get; set; }
    public CompilerIntrinsic_GetExpression(IToken token, TypeSymbol retrievedType, ExpressionBase contextPointer, int memberOffset) : base(token)
    {
        RetrievedType = retrievedType;
        ContextPointer = contextPointer;
        MemberOffset = memberOffset;
    }

    public override TypedExpression Resolve(ITypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }
    public override ExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new CompilerIntrinsic_GetExpression(Token, RetrievedType.ReplaceGenericTypeParameter(genericToConcreteTypeMap), ContextPointer.ReplaceGenericTypeSymbols(genericToConcreteTypeMap), MemberOffset).CopyStartAndEndTokens(this);
    }

    public override bool TryGetContainingExpression(int line, int column, out ExpressionBase? containingExpression)
    {
        if (ContextPointer.TryGetContainingExpression(line, column, out containingExpression)) return true;
        return base.TryGetContainingExpression(line, column, out containingExpression);
    }
}