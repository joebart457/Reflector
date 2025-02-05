using Language.Experimental.Expressions;
using Language.Experimental.Models;
using TokenizerCore.Interfaces;

namespace Language.Experimental.TypedExpressions;

public class TypedGetExpression : TypedExpression
{
    public TypedExpression Instance { get; private set; }
    public IToken TargetField { get; private set; }
    public bool ShortCircuitOnNull { get; private set; }    
    public TypedGetExpression(TypeInfo typeInfo, ExpressionBase originalExpression, TypedExpression instance, IToken targetField,  bool shortCircuitOnNull): base(typeInfo, originalExpression)
    {
        Instance = instance;
        TargetField = targetField;
        ShortCircuitOnNull = shortCircuitOnNull;
    }
}