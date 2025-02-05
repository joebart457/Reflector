using Language.Experimental.Compiler.TypeResolver;
using Language.Experimental.TypedStatements;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Statements;
public class ImportLibraryDefinition : StatementBase
{
    public IToken LibraryAlias { get; set; }
    public IToken LibraryPath { get; set; }
    public ImportLibraryDefinition(IToken libraryAlias, IToken libraryPath) : base(libraryAlias)
    {
        LibraryAlias = libraryAlias;
        LibraryPath = libraryPath;
    }

    public override void GatherSignature(TypeResolver typeResolver)
    {
        typeResolver.GatherSignature(this);
    }

    public override TypedStatement Resolve(TypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }
}