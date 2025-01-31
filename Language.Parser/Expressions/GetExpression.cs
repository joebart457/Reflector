using Language.Parser.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TokenizerCore.Interfaces;

namespace Language.Parser.Expressions;

public class GetExpression : ExpressionBase
{
    public ExpressionBase Instance { get; private set; }
    public IToken TargetField { get; private set; }
    public bool ShortCircuitOnNull { get; private set; }    
    public GetExpression(IToken token, ExpressionBase instance, IToken targetField,  bool shortCircuitOnNull): base(token)
    {
        Instance = instance;
        TargetField = targetField;
        ShortCircuitOnNull = shortCircuitOnNull;
    }

    public override object? Evaluate(IInterpreter interpreter)
    {
        return interpreter.Evaluate(this);
    }
}