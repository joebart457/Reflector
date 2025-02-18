using Language.Experimental.Interfaces;
using Language.Experimental.Parser;
using Language.Experimental.TypedExpressions;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Expressions;

public class LocalVariableExpression : ExpressionBase
{
    public TypeSymbol TypeSymbol { get; set; }
    public IToken Identifier { get; set; }
    public ExpressionBase? Initializer { get; set; }
    public LocalVariableExpression(IToken token, TypeSymbol typeSymbol, IToken identifier, ExpressionBase? initializer) : base(token)
    {
        TypeSymbol = typeSymbol;
        Identifier = identifier;
        Initializer = initializer;
    }

    public override TypedExpression Resolve(ITypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }

    public override ExpressionBase ReplaceGenericTypeSymbols(Dictionary<GenericTypeSymbol, TypeSymbol> genericToConcreteTypeMap)
    {
        return new LocalVariableExpression(Token, TypeSymbol.ReplaceGenericTypeParameter(genericToConcreteTypeMap), Identifier, Initializer?.ReplaceGenericTypeSymbols(genericToConcreteTypeMap)).CopyStartAndEndTokens(this);
    }

    public override bool TryGetContainingExpression(int line, int column, out ExpressionBase? containingExpression)
    {
        if (Initializer?.TryGetContainingExpression(line, column, out containingExpression) == true) return true;
        return base.TryGetContainingExpression(line, column, out containingExpression);
    }
}