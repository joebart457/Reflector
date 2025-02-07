using Language.Experimental.Expressions;
using Language.Experimental.Parser;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedExpressions;

public class UnresolvedLocalVariableExpression : UnresolvedExpressionBase
{
    public TypeSymbol TypeSymbol { get; set; }
    public IToken Identifier { get; set; }
    public UnresolvedExpressionBase? Initializer { get; set; }
    public UnresolvedLocalVariableExpression(IToken token, TypeSymbol typeSymbol, IToken identifier, UnresolvedExpressionBase? initializer) : base(token)
    {
        TypeSymbol = typeSymbol;
        Identifier = identifier;
        Initializer = initializer;
    }

    public override ExpressionBase Resolve(TypeSymbolResolver typeSymbolResolver)
    {
        return typeSymbolResolver.Resolve(this);
    }

    public override UnresolvedExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new UnresolvedLocalVariableExpression(Token, TypeSymbol.ReplaceGenericTypeParameter(genericToConcreteTypeMap), Identifier, Initializer?.ReplaceGenericTypeSymbols(genericToConcreteTypeMap));
    }
}