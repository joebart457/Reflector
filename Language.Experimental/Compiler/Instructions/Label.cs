using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Language.Experimental.Compiler.Instructions
{
    public class Label : X86Instruction
    {
        public string Text { get; set; }

        public Label(string text)
        {
            Text = text;
        }

        public override string Emit()
        {
            return $"{Text}:";
        }
    }
}
