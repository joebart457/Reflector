using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.Parser;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;


public class GenericFunctionReferenceExpression : ExpressionBase
{
    public IToken Identifier { get; set; }
    public List<TypeSymbol> TypeArguments { get; set; }
    public GenericFunctionReferenceExpression(IToken identifier, List<TypeSymbol> typeArguments) : base(identifier)
    {
        Identifier = identifier;
        TypeArguments = typeArguments;
    }

    public override ExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new GenericFunctionReferenceExpression(Identifier, TypeArguments.Select(x => x.ReplaceGenericTypeParameter(genericToConcreteTypeMap)).ToList());
    }

    public override TypedExpression Resolve(TypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }
}