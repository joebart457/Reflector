using Language.Experimental.Compiler;
using Language.Experimental.Constants;
using Language.Experimental.Models;
using System.Runtime.InteropServices;
using TokenizerCore.Interfaces;

namespace Language.Experimental.TypedStatements;

public class TypedImportedFunctionDefinition: TypedStatement, ITypedFunctionInfo
{
    public IToken FunctionName { get; set; }
    public TypeInfo ReturnType { get; set; }
    public List<TypedParameter> Parameters { get; set; }
    public CallingConvention CallingConvention { get; set; }
    public IToken LibraryAlias { get; set; }
    public IToken FunctionSymbol { get; set; }
    public bool IsImported => true;
    public bool IsExported => false;
    public IToken ExportedSymbol => throw new InvalidOperationException($"function {FunctionName.Lexeme} cannot be exported: export forwarding not supported");

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
        var intrinsicType = IntrinsicType.Func;
        if (CallingConvention == CallingConvention.Cdecl) intrinsicType = IntrinsicType.CFunc;
        var typeArguments = Parameters.Select(x => x.TypeInfo).ToList();
        typeArguments.Add(ReturnType);
        return new FunctionPtrTypeInfo(intrinsicType, typeArguments);
    }
}