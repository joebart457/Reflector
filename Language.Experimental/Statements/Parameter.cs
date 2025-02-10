using Language.Experimental.Constants;
using Language.Experimental.Parser;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Statements;

public class Parameter
{
    public IToken Name { get; set; }
    public TypeSymbol TypeSymbol { get; set; }
    public Parameter(IToken name, TypeSymbol typeSymbol)
    {
        Name = name;
        TypeSymbol = typeSymbol;
    }
}