using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Language.Experimental.Compiler.Instructions
{
    public class Cdq : X86Instruction
    {
        public override string Emit()
        {
            return $"cdq";
        }
    }
}
