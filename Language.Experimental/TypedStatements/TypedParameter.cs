using Language.Experimental.Models;
using TokenizerCore.Interfaces;

namespace Language.Experimental.TypedStatements;

public class TypedParameter
{
    public IToken Name { get; set; }
    public TypeInfo TypeInfo { get; set; }
    public TypedParameter(IToken name, TypeInfo typeInfo)
    {
        Name = name;
        TypeInfo = typeInfo;
    }
}