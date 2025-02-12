using Language.Experimental.Compiler;
using Language.Experimental.Statements;
using TokenizerCore.Interfaces;

namespace Language.Experimental.TypedStatements;

public class TypedImportLibraryDefinition : TypedStatement
{
    public IToken LibraryAlias { get; set; }
    public IToken LibraryPath { get; set; }
    public TypedImportLibraryDefinition(StatementBase originalStatement, IToken libraryAlias, IToken libraryPath): base(originalStatement)
    {
        LibraryAlias = libraryAlias;
        LibraryPath = libraryPath;
    }

    public override void Compile(X86CompilationContext cc)
    {
        cc.AddImportLibrary(this);
    }
}