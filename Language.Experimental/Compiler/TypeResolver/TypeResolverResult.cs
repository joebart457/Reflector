using Language.Experimental.TypedStatements;

namespace Language.Experimental.Compiler.TypeResolver;

public class TypeResolverResult
{
    public List<TypedImportLibraryDefinition> ImportLibraries { get; set; } = new();
    public List<TypedImportedFunctionDefinition> ImportedFunctions { get; set; } = new();
    public List<TypedFunctionDefinition> Functions { get; set; } = new();
    public TypeResolverResult(List<TypedImportLibraryDefinition> importLibraries, List<TypedImportedFunctionDefinition> importedFunctions, List<TypedFunctionDefinition> functions)
    {
        ImportLibraries = importLibraries;
        ImportedFunctions = importedFunctions;
        Functions = functions;
    }

    public TypeResolverResult()
    {

    }
}