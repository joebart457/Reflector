﻿using Language.Experimental.Interfaces;
using Language.Experimental.Parser;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public class GetExpression : ExpressionBase
{
    public ExpressionBase Instance { get; private set; }
    public IToken TargetField { get; private set; }
    public bool ShortCircuitOnNull { get; private set; }    
    public GetExpression(IToken token, ExpressionBase instance, IToken targetField,  bool shortCircuitOnNull): base(token)
    {
        Instance = instance;
        TargetField = targetField;
        ShortCircuitOnNull = shortCircuitOnNull;
    }

    public override TypedExpression Resolve(ITypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }

    public override ExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new GetExpression(Token, Instance.ReplaceGenericTypeSymbols(genericToConcreteTypeMap), TargetField, ShortCircuitOnNull).CopyStartAndEndTokens(this);
    }

    public override bool TryGetContainingExpression(int line, int column, out ExpressionBase? containingExpression)
    {
        if (Instance.TryGetContainingExpression(line, column, out containingExpression)) return true;
        return base.TryGetContainingExpression(line, column, out containingExpression);
    }
}