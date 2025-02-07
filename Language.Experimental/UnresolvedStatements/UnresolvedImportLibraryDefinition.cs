using Language.Experimental.Parser;
using Language.Experimental.Statements;
using TokenizerCore.Interfaces;

namespace Language.Experimental.UnresolvedStatements;
public class UnresolvedImportLibraryDefinition : UnresolvedStatementBase
{
    public IToken LibraryAlias { get; set; }
    public IToken LibraryPath { get; set; }
    public UnresolvedImportLibraryDefinition(IToken libraryAlias, IToken libraryPath) : base(libraryAlias)
    {
        LibraryAlias = libraryAlias;
        LibraryPath = libraryPath;
    }

    public override StatementBase Resolve(TypeSymbolResolver typeSymbolResolver)
    {
        return typeSymbolResolver.Resolve(this);
    }
}