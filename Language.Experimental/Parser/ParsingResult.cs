using Language.Experimental.Statements;

namespace Language.Experimental.Parser;

public class ParsingResult
{
    public List<FunctionDefinition> FunctionDefinitions { get; set; } = new();
    public List<ImportedFunctionDefinition> ImportedFunctionDefinitions { get; set; } = new();
    public List<ImportLibraryDefinition> ImportLibraryDefinitions { get; set; } = new();
    public List<GenericTypeDefinition> GenericTypeDefinitions { get; set; } = new();
    public List<GenericFunctionDefinition> GenericFunctionDefinitions { get; set; } = new();
    public List<TypeDefinition> TypeDefinitions { get; set; } = new();
    public ParsingResult(List<FunctionDefinition> functionDefinitions, List<ImportedFunctionDefinition> importedFunctionDefinitions, List<ImportLibraryDefinition> importLibraryDefinitions, 
        List<GenericTypeDefinition> genericTypeDefinitions, List<GenericFunctionDefinition> genericFunctionDefinitions, List<TypeDefinition> typeDefinitions)
    {
        FunctionDefinitions = functionDefinitions;
        ImportedFunctionDefinitions = importedFunctionDefinitions;
        ImportLibraryDefinitions = importLibraryDefinitions;
        GenericTypeDefinitions = genericTypeDefinitions;
        GenericFunctionDefinitions = genericFunctionDefinitions;
        TypeDefinitions = typeDefinitions;
    }
    public ParsingResult()
    {

    }
}