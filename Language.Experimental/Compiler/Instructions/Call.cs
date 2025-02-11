
namespace Language.Experimental.Compiler.Instructions
{
    public class Call : X86Instruction
    {
        public string Callee { get; set; }
        public bool IsIndirect { get; set; }
        public Call(string callee, bool isIndirect)
        {
            Callee = callee;
            IsIndirect = isIndirect;
        }

        public override string Emit()
        {
            if (IsIndirect) return $"call dword [{Callee}]";
            return $"call {Callee}";
        }
    }

    public class Call_RegisterOffset : X86Instruction
    {
        public RegisterOffset Callee { get; set; }
        public Call_RegisterOffset(RegisterOffset callee)
        {
            Callee = callee;
        }

        public override string Emit()
        {
            return $"call {Callee}";
        }
    }

    public class Call_Register : X86Instruction
    {
        public X86Register Callee { get; set; }
        public Call_Register(X86Register callee)
        {
            Callee = callee;
        }

        public override string Emit()
        {
            return $"call {Callee}";
        }
    }
}
