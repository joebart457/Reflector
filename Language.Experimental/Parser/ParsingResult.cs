using Language.Experimental.Statements;

namespace Language.Experimental.Parser;


public class ParsingResult
{
    public List<FunctionDefinition> FunctionDefinitions { get; set; } = new();
    public List<ImportedFunctionDefinition> ImportedFunctionDefinitions { get; set; } = new();
    public List<ImportLibraryDefinition> ImportLibraryDefinitions { get; set; } = new();
    public ParsingResult(List<FunctionDefinition> functionDefinitions, List<ImportedFunctionDefinition> importedFunctionDefinitions, List<ImportLibraryDefinition> importLibraryDefinitions)
    {
        FunctionDefinitions = functionDefinitions;
        ImportedFunctionDefinitions = importedFunctionDefinitions;
        ImportLibraryDefinitions = importLibraryDefinitions;
    }
    public ParsingResult()
    {

    }
}