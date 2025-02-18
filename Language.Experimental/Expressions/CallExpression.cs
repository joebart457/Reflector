using Language.Experimental.Interfaces;
using Language.Experimental.Parser;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public class CallExpression : ExpressionBase
{
    public ExpressionBase CallTarget { get; private set; }
    public List<ExpressionBase> Arguments { get; private set; }
    public CallExpression(IToken token, ExpressionBase callTarget, List<ExpressionBase> arguments) : base(token)
    {
        CallTarget = callTarget;
        Arguments = arguments;
    }

    public override TypedExpression Resolve(ITypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }

    public override ExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new CallExpression(Token, CallTarget.ReplaceGenericTypeSymbols(genericToConcreteTypeMap), Arguments.Select(x => x.ReplaceGenericTypeSymbols(genericToConcreteTypeMap)).ToList()).CopyStartAndEndTokens(this);
    }

    public override bool TryGetContainingExpression(int line, int column, out ExpressionBase? containingExpression)
    {
        if (CallTarget.TryGetContainingExpression(line, column, out containingExpression)) return true;
        foreach(var argument in Arguments)
        {
            if (argument.TryGetContainingExpression(line, column, out containingExpression)) return true;
        }
        return base.TryGetContainingExpression(line, column, out containingExpression);
    }
}