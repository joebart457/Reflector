using Language.Experimental.Parser;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Statements;

public class TypeDefinitionField
{
    public TypeSymbol TypeSymbol { get; set; }
    public IToken Name { get; set; }
    public TypeDefinitionField(TypeSymbol typeSymbol, IToken name)
    {
        TypeSymbol = typeSymbol;
        Name = name;
    }

}