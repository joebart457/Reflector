using Language.Experimental.Models;
using System.Runtime.InteropServices;
using TokenizerCore.Interfaces;

namespace Language.Experimental.TypedStatements;

public interface ITypedFunctionInfo
{
    public IToken FunctionName { get; }
    public IToken FunctionSymbol { get; }
    public TypeInfo ReturnType { get; }
    public List<TypedParameter> Parameters { get; }
    public CallingConvention CallingConvention { get; }
    public bool IsImported { get; }
    public bool IsExported { get; }
    public IToken ExportedSymbol { get; }
    public string GetDecoratedFunctionIdentifier();
}