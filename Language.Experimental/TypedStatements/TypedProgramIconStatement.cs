using Language.Experimental.Compiler;
using Language.Experimental.Statements;
using TokenizerCore.Interfaces;

namespace Language.Experimental.TypedStatements;

public class TypedProgramIconStatement : TypedStatement
{
    public IToken IconFilePath { get; set; }
    public TypedProgramIconStatement(StatementBase originalStatement, IToken iconFilePath) : base(originalStatement)
    {
        IconFilePath = iconFilePath;
    }

    public override void Compile(X86CompilationContext cc)
    {
        cc.SetProgramIcon(IconFilePath);
    }
}