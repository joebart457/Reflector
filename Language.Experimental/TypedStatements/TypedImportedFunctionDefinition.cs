using Language.Experimental.Compiler;
using Language.Experimental.Constants;
using Language.Experimental.Models;
using Language.Experimental.Statements;
using System.Runtime.InteropServices;
using TokenizerCore.Interfaces;

namespace Language.Experimental.TypedStatements;

public class TypedImportedFunctionDefinition: TypedStatement
{
    public IToken FunctionName { get; set; }
    public TypeInfo ReturnType { get; set; }
    public List<TypedParameter> Parameters { get; set; }
    public CallingConvention CallingConvention { get; set; }
    public IToken LibraryAlias { get; set; }
    public IToken FunctionSymbol { get; set; }
    public TypedImportedFunctionDefinition(IToken functionName, TypeInfo returnType, List<TypedParameter> parameters, CallingConvention callingConvention, IToken libraryAlias, IToken functionSymbol)
    {
        FunctionName = functionName;
        ReturnType = returnType;
        Parameters = parameters;
        CallingConvention = callingConvention;
        LibraryAlias = libraryAlias;
        FunctionSymbol = functionSymbol;
    }


    public string GetDecoratedFunctionIdentifier()
    {
        if (CallingConvention == CallingConvention.Cdecl) return $"_{FunctionName.Lexeme}";
        if (CallingConvention == CallingConvention.StdCall) return $"_{FunctionName.Lexeme}@{Parameters.Count * 4}";
        throw new NotImplementedException();
    }

    public override void Compile(X86CompilationContext cc)
    {
        cc.AddImportedFunction(this);
    }

    public FunctionPtrTypeInfo GetFunctionPointerType()
    {
        var intrinsicType = IntrinsicType.StdCall_Function_Ptr_External;
        if (CallingConvention == CallingConvention.Cdecl) intrinsicType = IntrinsicType.Cdecl_Function_Ptr_External;
        var typeArguments = Parameters.Select(x => x.TypeInfo).ToList();
        typeArguments.Add(ReturnType);
        return new FunctionPtrTypeInfo(intrinsicType, typeArguments);
    }
}