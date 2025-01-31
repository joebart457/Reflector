using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Language.Runtime.Exceptions;

public class ReturnException : System.Exception
{
    public object? Value { get; private set; }

    public ReturnException(object? value)
    {
        Value = value;
    }
}