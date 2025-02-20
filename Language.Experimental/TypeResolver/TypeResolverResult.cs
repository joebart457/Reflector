using Language.Experimental.TypedStatements;

namespace Language.Experimental.TypeResolver;

public class TypeResolverResult
{
    public List<TypedImportLibraryDefinition> ImportLibraries { get; set; } = new();
    public List<TypedImportedFunctionDefinition> ImportedFunctions { get; set; } = new();
    public List<TypedFunctionDefinition> Functions { get; set; } = new();
    public TypedProgramIconStatement? ProgramIcon { get; set; }

    public TypeResolverResult()
    {

    }
}