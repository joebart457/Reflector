using Language.Experimental.Parser;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedStatements;

public class UnresolvedParameter
{
    public IToken Name { get; set; }
    public TypeSymbol TypeSymbol { get; set; }
    public UnresolvedParameter(IToken name, TypeSymbol typeSymbol)
    {
        Name = name;
        TypeSymbol = typeSymbol;
    }
}