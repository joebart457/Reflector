using Language.Experimental.UnresolvedStatements;

namespace Language.Experimental.Parser;


public class UnresolvedParsingResult
{
    public List<GenericTypeDefinition> GenericTypeDefinitions { get; set; } = new();
    public List<GenericFunctionDefinition> GenericFunctionDefinitions { get; set; } = new();
    public List<TypeDefinition> TypeDefinitions { get; set; } = new();
    public List<UnresolvedFunctionDefinition> FunctionDefinitions { get; set; } = new();
    public List<UnresolvedImportLibraryDefinition> ImportLibraryDefinitions { get; set; } = new();
    public List<UnresolvedImportedFunctionDefinition> ImportedFunctionDefinitions { get; set; } = new();
    public UnresolvedParsingResult(List<GenericTypeDefinition> genericTypeDefinitions, List<GenericFunctionDefinition> genericFunctionDefinitions, List<TypeDefinition> typeDefinitions, 
        List<UnresolvedFunctionDefinition> functionDefinitions, List<UnresolvedImportLibraryDefinition> importLibraryDefinitions, List<UnresolvedImportedFunctionDefinition> importedFunctionDefinitions)
    {
        GenericTypeDefinitions = genericTypeDefinitions;
        GenericFunctionDefinitions = genericFunctionDefinitions;
        TypeDefinitions = typeDefinitions;
        FunctionDefinitions = functionDefinitions;
        ImportLibraryDefinitions = importLibraryDefinitions;
        ImportedFunctionDefinitions = importedFunctionDefinitions;
    }

    public UnresolvedParsingResult()
    {

    }

}