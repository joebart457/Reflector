using Language.Parser.Expressions;

namespace Language.Parser.Interfaces;

public interface IInterpreter
{
    object? Evaluate(ExpressionBase expression);
    object? Evaluate(CallExpression callExpression);
    object? Evaluate(GetExpression getExpression);
    object? Evaluate(IdentifierExpression identifierExpression);
    object? Evaluate(LiteralExpression literalExpression);
}