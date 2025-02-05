using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Language.Experimental.Compiler.Instructions
{
    public class Not_Offset : X86Instruction
    {
        public RegisterOffset Operand { get; set; }

        public Not_Offset(RegisterOffset operand)
        {
            Operand = operand;
        }

        public override string Emit()
        {
            return $"not {Operand}";
        }
    }
}
