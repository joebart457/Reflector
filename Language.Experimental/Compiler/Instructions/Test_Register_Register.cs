﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Language.Experimental.Compiler.Instructions
{
    public class Test_Register_Register : X86Instruction
    {
        public X86Register Operand1 { get; set; }
        public X86Register Operand2 { get; set; }
        public Test_Register_Register(X86Register operand1, X86Register operand2)
        {
            Operand1 = operand1;
            Operand2 = operand2;
        }

        public override string Emit()
        {
            return $"test {Operand1}, {Operand2}";
        }
    }

    public class Test_Register_Offset : X86Instruction
    {
        public X86Register Operand1 { get; set; }
        public RegisterOffset Operand2 { get; set; }
        public Test_Register_Offset(X86Register operand1, RegisterOffset operand2)
        {
            Operand1 = operand1;
            Operand2 = operand2;
        }

        public override string Emit()
        {
            return $"test {Operand1}, {Operand2}";
        }
    }
}
