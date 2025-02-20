using Language.Experimental.Statements;

namespace Language.Experimental.Parser;

public class ParsingResult
{
    public string? SourceFilePath { get; set; }
    public List<FunctionDefinition> FunctionDefinitions { get; set; } = new();
    public List<ImportedFunctionDefinition> ImportedFunctionDefinitions { get; set; } = new();
    public List<ImportLibraryDefinition> ImportLibraryDefinitions { get; set; } = new();
    public List<GenericTypeDefinition> GenericTypeDefinitions { get; set; } = new();
    public List<GenericFunctionDefinition> GenericFunctionDefinitions { get; set; } = new();
    public List<TypeDefinition> TypeDefinitions { get; set; } = new();
    public ProgramIconStatement? ProgramIconStatement { get; set; }

    public ParsingResult()
    {

    }
}