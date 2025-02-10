using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.TypedStatements;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Statements;

public class TypeDefinition : StatementBase
{
    public IToken TypeName { get; set; }
    public List<TypeDefinitionField> Fields { get; set; }
    public TypeDefinition(IToken typeName, List<TypeDefinitionField> fields) : base(typeName)
    {
        TypeName = typeName;
        Fields = fields;
    }

    public override void GatherSignature(TypeResolver typeResolver)
    {
        typeResolver.GatherSignature(this);
    }

    public override TypedStatement Resolve(TypeResolver typeResolver)
    {
        throw new NotImplementedException();
    }
}
