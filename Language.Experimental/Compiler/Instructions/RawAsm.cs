
namespace Language.Experimental.Compiler.Instructions;

public class RawAsm : X86Instruction
{
    public string Assembly { get; set; }

    public RawAsm(string assembly)
    {
        Assembly = assembly;
    }

    public override string Emit()
    {
        return Assembly;
    }
}