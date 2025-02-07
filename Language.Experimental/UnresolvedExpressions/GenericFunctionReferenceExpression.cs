using Language.Experimental.Expressions;
using Language.Experimental.Parser;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedExpressions;

public class GenericFunctionReferenceExpression : UnresolvedExpressionBase
{
    public IToken Identifier { get; set; }
    public List<TypeSymbol> TypeArguments { get; set; }
    public GenericFunctionReferenceExpression(IToken identifier, List<TypeSymbol> typeArguments) : base(identifier)
    {
        Identifier = identifier;
        TypeArguments = typeArguments;
    }

    public override ExpressionBase Resolve(TypeSymbolResolver typeSymbolResolver)
    {
        return typeSymbolResolver.Resolve(this);
    }

    public override UnresolvedExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new GenericFunctionReferenceExpression(Identifier, TypeArguments.Select(x => x.ReplaceGenericTypeParameter(genericToConcreteTypeMap)).ToList());
    }
}