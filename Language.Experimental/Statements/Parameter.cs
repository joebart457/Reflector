using Language.Experimental.Constants;
using Language.Experimental.Models;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Statements;

public class Parameter
{
    public IToken Name { get; set; }
    public TypeInfo TypeInfo { get; set; }
    public Parameter(IToken name, TypeInfo typeInfo)
    {
        Name = name;
        TypeInfo = typeInfo;
    }
}