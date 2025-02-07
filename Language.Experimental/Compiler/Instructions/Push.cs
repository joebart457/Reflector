

namespace Language.Experimental.Compiler.Instructions
{
    public class Push_Register : X86Instruction
    {
        public X86Register Register { get; set; }
        public Push_Register(X86Register register)
        {
            Register = register;
        }

        public override string Emit()
        {
            return $"push {Register}";
        }
    }

    public class Push_Offset : X86Instruction
    {
        public RegisterOffset Offset { get; set; }
        public Push_Offset(RegisterOffset offset)
        {
            Offset = offset;
        }

        public override string Emit()
        {
            return $"push {Offset}";
        }
    }

    public class Push_SymbolOffset : X86Instruction
    {
        public SymbolOffset Offset { get; set; }
        public Push_SymbolOffset(SymbolOffset offset)
        {
            Offset = offset;
        }

        public override string Emit()
        {
            return $"push {Offset}";
        }
    }

    public class Push_Address : X86Instruction
    {
        public string Address { get; set; }
        public Push_Address(string address)
        {
            Address = address;
        }

        public override string Emit()
        {
            return $"push {Address}";
        }
    }

    public class Push_Immediate<Ty> : X86Instruction
    {
        public Ty Immediate { get; set; }

        public Push_Immediate(Ty immediate)
        {
            Immediate = immediate;
        }

        public override string Emit()
        {
            return $"push {Immediate}";
        }
    }
}
