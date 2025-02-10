using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.Parser;
using Language.Experimental.TypedStatements;
using ParserLite.Exceptions;
using TokenizerCore.Interfaces;
using TokenizerCore.Model;

namespace Language.Experimental.Statements;

public class GenericTypeDefinition : StatementBase
{
    public IToken TypeName { get; set; }
    public List<GenericTypeSymbol> GenericTypeParameters { get; set; }
    public List<TypeDefinitionField> Fields { get; set; }
    public GenericTypeDefinition(IToken typeName, List<GenericTypeSymbol> genericTypeParameters, List<TypeDefinitionField> fields) : base(typeName)
    {
        TypeName = typeName;
        GenericTypeParameters = genericTypeParameters;
        Fields = fields;
    }

    public TypeDefinition ToConcreteTypeDefinition(List<TypeSymbol> typeArguments)
    {
        var unresolvedTypeParameter = typeArguments.Find(x => x.IsGenericTypeSymbol || x.ContainsGenericTypeSymbol);
        if (unresolvedTypeParameter != null)
            throw new ParsingException(TypeName, $"invalid generic type arguments: unresolved generic type parameter {unresolvedTypeParameter}");
        if (typeArguments.Count != GenericTypeParameters.Count)
            throw new ParsingException(TypeName, $"expected {GenericTypeParameters.Count} type arguments but got {typeArguments.Count}");

        var genericToConcreteTypeMap = new Dictionary<GenericTypeSymbol, TypeSymbol>();
        for (int i = 0; i < GenericTypeParameters.Count; i++)
        {
            if (genericToConcreteTypeMap.ContainsKey(GenericTypeParameters[i]))
                throw new ParsingException(GenericTypeParameters[i].TypeName, $"redefinition of generic type parameter {GenericTypeParameters[i].TypeName.Lexeme}");
            genericToConcreteTypeMap[GenericTypeParameters[i]] = typeArguments[i];
        }

        var newFields = Fields.Select(x => new TypeDefinitionField(x.TypeSymbol, x.Name)).ToList();
        foreach (var field in newFields)
        {
            field.TypeSymbol = field.TypeSymbol.ReplaceGenericTypeParameter(genericToConcreteTypeMap);
        }

        // Check if there are still any unresolved parameters
        unresolvedTypeParameter = newFields.Select(x => x.TypeSymbol).FirstOrDefault(x => x.IsGenericTypeSymbol || x.ContainsGenericTypeSymbol);
        if (unresolvedTypeParameter != null)
            throw new ParsingException(TypeName, $"invalid generic type arguments: unresolved generic type parameter {unresolvedTypeParameter}");

        var instantiatedTypeName = $"{TypeName.Lexeme}!{string.Join('_', typeArguments.Select(x => x.GetFlattenedName()))}";
        var instantiatedTypeNameToken = new Token(TypeName.Type, instantiatedTypeName, TypeName.Start, TypeName.End);

        return new TypeDefinition(instantiatedTypeNameToken, newFields);
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
