using Language.Experimental.Constants;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Statements;


public class FunctionDefinition : StatementBase
{
    public IToken FunctionName { get; set; }
    public IntrinsicType ReturnType { get; set; }
    public List<Parameter> Parameters { get; set; }
    public List<StatementBase> BodyStatements { get; set; }

    public FunctionDefinition(IToken functionName, IntrinsicType returnType, List<Parameter> parameters, List<StatementBase> bodyStatements) : base(functionName)
    {
        FunctionName = functionName;
        ReturnType = returnType;
        Parameters = parameters;
        BodyStatements = bodyStatements;
    }


    public class Parameter
    {
        public IToken Name { get; set; }
        public IntrinsicType Type { get; set; }
        public Parameter(IToken name, IntrinsicType type)
        {
            Name = name;
            Type = type;
        }
    }
}