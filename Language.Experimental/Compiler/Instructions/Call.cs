using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
