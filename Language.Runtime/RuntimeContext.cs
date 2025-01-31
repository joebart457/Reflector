using Language.Parser.Expressions;
using Language.Parser.Interfaces;
using Language.Runtime.BuiltinTypes;
using Language.Runtime.CustomAttributes;
using Language.Runtime.Exceptions;
using Language.Runtime.Extensions;
using Language.Runtime.Interfaces;
using TokenizerCore.Model;


namespace Language.Runtime;



public class RuntimeContext: IInterpreter
{
    private Environment<object?> _environment;

    public RuntimeContext(Environment<object?> environment)
    {
        _environment = environment;
    }

    public Environment<object?> GetCurrentEnvironment()
    {
        return _environment;
    }

    public object? Evaluate(IEnumerable<ExpressionBase> program)
    {
        foreach(var expression in program)
        {
           expression.Evaluate(this);
        }
        return null;
    }

    public object? Evaluate(ExpressionBase expression)
    {
        return expression.Evaluate(this);
    }
    public object? Evaluate(CallExpression callExpression)
    {
        var potentialCallTarget = callExpression.CallTarget.Evaluate(this);
        if (potentialCallTarget == null) 
            throw new RuntimeException(callExpression.Token, "call target was null");
        if (potentialCallTarget is ICallable callable)
        {
            var args = callExpression.Arguments.Select(x => x.Evaluate(this)).ToList();
            return callable.Invoke(this, args);
        }
        throw new RuntimeException(callExpression.Token, $"invalid call to non callable type {potentialCallTarget.GetType().UndecoratedName()}");
    }

    public object? Evaluate(GetExpression getExpression)
    {
        var potentialContainer = getExpression.Instance.Evaluate(this);
        if (potentialContainer == null)
        {
            if (getExpression.ShortCircuitOnNull) return null;
            throw new RuntimeException(getExpression.Token, "left hand side of member access was null");
        }
        if (potentialContainer is Environment<object?> container)
        {
            return container.Get(getExpression.Token);
        }
        throw new RuntimeException(getExpression.Token, $"invalid member access to non-container type {potentialContainer.GetType().UndecoratedName()}");
    }

    public object? Evaluate(IdentifierExpression identifierExpression)
    {
         return _environment.Get(identifierExpression.Token);
    }

    public object? Evaluate(LiteralExpression literalExpression)
    {
        return literalExpression.Value;
    }

}

public static class Runtime
{
    public static void DefineFunction([RuntimeInserted] RuntimeContext context, IdentifierExpression identifier, List<Parameter> parameterList, params ExpressionBase[] statements)
    {
        var newCallable = new UserCallable(context.GetCurrentEnvironment(), identifier.Token, parameterList, statements.ToList());
        context.GetCurrentEnvironment().Define(identifier.Token, newCallable);
    }

    public static UserCallable DefineAnonymousFunction([RuntimeInserted] RuntimeContext context, List<Parameter> parameterList, params ExpressionBase[] statements)
    {
        var newCallable = new UserCallable(context.GetCurrentEnvironment(), new Token("_anonymous_", "_anonymous_", -1, -1), parameterList, statements.ToList());
        return newCallable;
    }

    public static Parameter Param(IdentifierExpression identifier, Type type) => new Parameter(identifier.Token, type);
    public static List<Parameter> Params(params Parameter[] parameters) => parameters.ToList();
    public static void Return(object value) => throw new ReturnException(value);
    public static object? If([RuntimeInserted] RuntimeContext context, bool condition, ICallable thenDo, ICallable elseDo)
    {
        if (condition) return thenDo.Invoke(context, new());
        else return elseDo.Invoke(context, new());
    }

    public static bool Equal(object? value1, object? value2)
    {
        if (value1 == null) return value2 == null;
        return value1.Equals(value2);
    }
}
