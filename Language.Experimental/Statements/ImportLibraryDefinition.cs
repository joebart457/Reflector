using Language.Experimental.Interfaces;
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

    public override void GatherSignature(ITypeResolver typeResolver)
    {
        typeResolver.GatherSignature(this);
    }

    public override TypedStatement Resolve(ITypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }
}