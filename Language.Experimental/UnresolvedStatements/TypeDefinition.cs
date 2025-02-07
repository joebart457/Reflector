using Language.Experimental.Parser;
using Language.Experimental.Statements;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedStatements;

public class TypeDefinition : UnresolvedStatementBase
{
    public IToken TypeName { get; set; }
    public List<TypeDefinitionField> Fields { get; set; }
    public TypeDefinition(IToken typeName, List<TypeDefinitionField> fields) : base(typeName)
    {
        TypeName = typeName;
        Fields = fields;
    }

    public override StatementBase Resolve(TypeSymbolResolver typeSymbolResolver)
    {
        throw new NotImplementedException();
    }

    public void GatherSignature(TypeSymbolResolver typeSymbolResolver)
    {
        typeSymbolResolver.GatherSignature(this);
    }
}
