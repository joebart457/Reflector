using Language.Experimental.Interfaces;
using Language.Experimental.TypedStatements;
using TokenizerCore.Interfaces;


namespace Language.Experimental.Statements;

public class ProgramIconStatement : StatementBase
{
    public IToken IconFilePath { get; set; }

    public ProgramIconStatement(IToken iconFilePath): base(iconFilePath)
    {
        IconFilePath = iconFilePath;
    }

    public override void GatherSignature(ITypeResolver typeResolver)
    {
        throw new NotImplementedException();
    }

    public override TypedStatement Resolve(ITypeResolver typeResolver)
    {
        return typeResolver.Resolve(this);
    }
}