using Language.Experimental.Compiler.TypeResolver;
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

    public override TypedExpression Resolve(TypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }

    public override ExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new GetExpression(Token, Instance.ReplaceGenericTypeSymbols(genericToConcreteTypeMap), TargetField, ShortCircuitOnNull);
    }
}