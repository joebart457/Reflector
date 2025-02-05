using Language.Experimental.Compiler;
using TokenizerCore.Interfaces;

namespace Language.Experimental.TypedStatements;

public class TypedImportLibraryDefinition : TypedStatement
{
    public IToken LibraryAlias { get; set; }
    public IToken LibraryPath { get; set; }
    public TypedImportLibraryDefinition(IToken libraryAlias, IToken libraryPath)
    {
        LibraryAlias = libraryAlias;
        LibraryPath = libraryPath;
    }

    public override void Compile(X86CompilationContext cc)
    {
        cc.AddImportLibrary(this);
    }
}