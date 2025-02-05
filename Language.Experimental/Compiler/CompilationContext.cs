﻿using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Extensions;
using Language.Experimental.Statements;
using Language.Experimental.TypedStatements;
using ParserLite.Exceptions;
using System.Text;
using TokenizerCore.Interfaces;

namespace Language.Experimental.Compiler;


public class X86CompilationContext
{
    public CompilationOptions CompilationOptions { get; private set; }

    public X86CompilationContext(CompilationOptions compilationOptions)
    {
        CompilationOptions = compilationOptions;
    }

    private X86Function? _currentFunctionTarget;
    private int _stringUniqueIndex = 0;
    private int _floatUniqueIndex = 0;
    private int _integerUniqueIndex = 0;
    private int _byteUniqueIndex = 0;
    private int _pointerUniqueIndex = 0;
    private int _unitializedDataUniqueIndex = 0;

    private int _loopUniqueIndex = 0;

    private Stack<(string continueLabel, string breakLabel)> _loopLabels = new Stack<(string continueLabel, string breakLabel)>();
    private List<StringData> _stringData = new();
    private List<SinglePrecisionFloatingPointData> _floatingPointData = new();
    private List<IntegerData> _integerData = new();
    private List<ByteData> _byteData = new();
    private List<PointerData> _pointerData = new();
    private List<UnitializedData> _unitializedData = new();
    public List<StringData> StaticStringData => _stringData;
    public List<SinglePrecisionFloatingPointData> StaticFloatingPointData => _floatingPointData;
    public List<IntegerData> StaticIntegerData => _integerData;
    public List<ByteData> StaticByteData => _byteData;
    public List<PointerData> StaticPointerData => _pointerData;
    public List<UnitializedData> StaticUnitializedData => _unitializedData;
    public IconData? ProgramIcon { get; set; }
    public int SizeOfPtr => 4;

    public List<X86Function> FunctionData { get; private set; } = new();
    public List<ImportLibrary> ImportLibraries { get; private set; } = new();
    public List<(string functionIdentifier, string exportedSymbol)> ExportedFunctions => FunctionData.Where(x => x.IsExported)
        .OrderBy(x => x.ExportedSymbol.Lexeme, StringComparer.Ordinal) // Must be exported in ordinal order
        .Select(x => (x.OriginalDeclaration.GetDecoratedFunctionIdentifier(), x.ExportedSymbol.Lexeme))
        .ToList();
    public void AddImportedFunction(TypedImportedFunctionDefinition importedFunction)
    {
        var foundLibrary = ImportLibraries.Find(x => x.LibraryAlias.Lexeme == importedFunction.LibraryAlias.Lexeme);
        if (foundLibrary == null) throw new Exception($"import library with alias {importedFunction.LibraryAlias} is not defined");
        foundLibrary.AddImportedFunction(importedFunction.FunctionName, importedFunction.FunctionSymbol);
    }

    public void AddImportLibrary(TypedImportLibraryDefinition library)
    {
        var foundLibrary = ImportLibraries.Find(x => x.LibraryAlias.Lexeme == library.LibraryAlias.Lexeme);
        if (foundLibrary != null) throw new Exception($"import library with alias {library.LibraryAlias} is already defined");
        ImportLibraries.Add(new(library.LibraryPath, library.LibraryAlias));
    }


    public void AddInstruction(X86Instruction x86Instruction)
    {
        if (_currentFunctionTarget == null) throw new InvalidOperationException();
        _currentFunctionTarget.AddInstruction(x86Instruction);
    }

    public string AddStringData(string value, string? symbol = null)
    {
        var label = !string.IsNullOrWhiteSpace(symbol) ? Decorate(symbol) : CreateUniqueStringLabel();
        _stringData.Add(new(label, value));
        return label;
    }

    public string AddSinglePrecisionFloatingPointData(float value, string? symbol = null)
    {
        var label = !string.IsNullOrWhiteSpace(symbol) ? Decorate(symbol) : CreateUniqueFloatingPointLabel();
        _floatingPointData.Add(new(label, value));
        return label;
    }

    public string AddIntegerData(int value, string? symbol = null)
    {
        var label = !string.IsNullOrWhiteSpace(symbol) ? Decorate(symbol) : CreateUniqueIntegerLabel();
        _integerData.Add(new(label, value));
        return label;
    }

    public string AddByteData(byte value, string? symbol = null)
    {
        var label = !string.IsNullOrWhiteSpace(symbol) ? Decorate(symbol) : CreateUniqueByteLabel();
        _byteData.Add(new(label, value));
        return label;
    }

    public string AddPointerData(int value, string? symbol = null)
    {
        var label = !string.IsNullOrWhiteSpace(symbol) ? Decorate(symbol) : CreateUniquePointerLabel();
        _pointerData.Add(new(label, value));
        return label;
    }

    public string AddUninitializedData(int bytesToReserve, string? symbol = null)
    {
        var label = !string.IsNullOrWhiteSpace(symbol) ? Decorate(symbol) : CreateUniquePointerLabel();
        _unitializedData.Add(new(label, bytesToReserve));
        return label;
    }

    public RegisterOffset GetIdentifierOffset(IToken identifier)
    {
        var foundParameterIndex = CurrentFunction.Parameters.FindIndex(x => x.Name.Lexeme == identifier.Lexeme);
        if (foundParameterIndex != -1) return new RegisterOffset(X86Register.ebp, 8 + (foundParameterIndex * 4));
        var foundLocalVariableIndex = CurrentFunction.LocalVariables.FindIndex(x => x.Identifier.Lexeme == identifier.Lexeme);
        if (foundLocalVariableIndex != -1) return new RegisterOffset(X86Register.ebp, -4 - CurrentFunction.LocalVariables.Take(foundLocalVariableIndex + 1).Sum(x => x.TypeInfo.StackSize()));
        throw new Exception($"local variable {identifier} does not exist");
    }

    public RegisterOffset GetIdentifierOffset(IToken identifier, out bool isParameterOffset)
    {
        isParameterOffset = true;
        var foundParameterIndex = CurrentFunction.Parameters.FindIndex(x => x.Name.Lexeme == identifier.Lexeme);
        if (foundParameterIndex != -1) return new RegisterOffset(X86Register.ebp, 8 + (foundParameterIndex * 4));
        isParameterOffset = false;
        var foundLocalVariableIndex = CurrentFunction.LocalVariables.FindIndex(x => x.Identifier.Lexeme == identifier.Lexeme);
        if (foundLocalVariableIndex != -1) return new RegisterOffset(X86Register.ebp, -4 - CurrentFunction.LocalVariables.Take(foundLocalVariableIndex + 1).Sum(x => x.TypeInfo.StackSize()));
        throw new Exception($"local variable {identifier} does not exist");
    }

    public SymbolOffset GetGlobalOffset(IToken identifier)
    {
        return Offset.CreateSymbolOffset(Decorate(identifier.Lexeme), 0);
    }

    public void EnterFunction(TypedFunctionDefinition functionDefinition)
    {
        if (_currentFunctionTarget != null) throw new InvalidOperationException();
        _currentFunctionTarget = new X86Function(functionDefinition);
    }

    public void ExitFunction()
    {
        if (_currentFunctionTarget == null) throw new InvalidOperationException();
        FunctionData.Add(_currentFunctionTarget);
        _currentFunctionTarget = null;
    }

    public X86Function CurrentFunction => _currentFunctionTarget ?? throw new InvalidOperationException();

    private string CreateUniqueStringLabel()
    {
        return $"!str_{_stringUniqueIndex++}";
    }

    private string CreateUniqueFloatingPointLabel()
    {
        return $"!flt_{_floatUniqueIndex++}";
    }

    private string CreateUniqueIntegerLabel()
    {
        return $"!int_{_integerUniqueIndex++}";
    }
    private string CreateUniqueByteLabel()
    {
        return $"!byt_{_byteUniqueIndex++}";
    }
    private string CreateUniquePointerLabel()
    {
        return $"!ptr_{_pointerUniqueIndex++}";
    }

    private string CreateUniqueUnitializedDataLabel()
    {
        return $"!uninit_{_unitializedDataUniqueIndex++}";
    }
    private string CreateLoopLabel()
    {
        return $"!loop_{_loopUniqueIndex++}";
    }

    public string CreateLabel()
    {
        return $"!label_{_stringUniqueIndex++}";
    }

    private string Decorate(string symbol)
    {
        return $"!gbl_{symbol}";
    }

    public string EnterLoop(string continueLabel)
    {
        var breakLabel = CreateLoopLabel();
        _loopLabels.Push((continueLabel, breakLabel));
        return breakLabel;
    }

    public void ExitLoop()
    {
        _loopLabels.Pop();
    }


    public string GetLoopBreakLabel()
    {
        if (_loopLabels.Count == 0) throw new InvalidOperationException();
        return _loopLabels.Peek().breakLabel;
    }

    public string GetLoopContinueLabel()
    {
        if (_loopLabels.Count == 0) throw new InvalidOperationException();
        return _loopLabels.Peek().continueLabel;
    }

    public void SetProgramIcon(IToken iconFilePath)
    {
        if (ProgramIcon != null) throw new ParsingException(iconFilePath, $"program icon must only be defined once");
        ProgramIcon = new IconData(iconFilePath.Lexeme);
    }

    #region DataDefinitions
    public class ImportLibrary
    {
        public class ImportedFunction
        {
            public IToken Symbol { get; set; }
            public IToken FunctionIdentifier { get; set; }
            public ImportedFunction(IToken symbol, IToken functionIdentifier)
            {
                Symbol = symbol;
                FunctionIdentifier = functionIdentifier;
            }
        }
        public IToken LibraryPath { get; set; }
        public IToken LibraryAlias { get; set; }
        public List<ImportedFunction> ImportedFunctions { get; set; } = new();
        public ImportLibrary(IToken libraryPath, IToken libraryAlias)
        {
            LibraryPath = libraryPath;
            LibraryAlias = libraryAlias;
        }

        public void AddImportedFunction(IToken functionIdentifier, IToken symbol)
        {
            if (ImportedFunctions.Any(x => x.FunctionIdentifier.Lexeme == functionIdentifier.Lexeme))
                throw new Exception($"symbol with name {functionIdentifier} is already imported");
            ImportedFunctions.Add(new(symbol, functionIdentifier));
        }
    }

    public class StringData
    {
        public string Label { get; set; }
        public string Value { get; set; }
        public StringData(string label, string value)
        {
            Label = label;
            Value = value;
        }

        public string Emit(int indentLevel, CompilationOptions options)
        {
            var sourceComment = "";
            if (options.SourceComments)
            {
                sourceComment = $" ; \"{Value.ReplaceLineEndings("\\r\\n")}\"";
            }
            return $"{Label} db {EscapeString(Value)},0{sourceComment}".Indent(indentLevel);
        }

        private static string EscapeString(string value)
        {
            if (value.Length == 0) return "0";
            var bytes = Encoding.UTF8.GetBytes(value);
            var strBytes = BitConverter.ToString(bytes);
            return $"0x{strBytes.Replace("-", ",0x")}";
        }
    }

    public class SinglePrecisionFloatingPointData
    {
        public string Label { get; set; }
        public float Value { get; set; }
        public SinglePrecisionFloatingPointData(string label, float value)
        {
            Label = label;
            Value = value;
        }

        public string Emit(int indentLevel, CompilationOptions options)
        {
            var sourceComment = "";
            if (options.SourceComments)
            {
                sourceComment = $" ;    {Value}";
            }
            return $"{Label} db {ToBytes(Value)}{sourceComment}".Indent(indentLevel);
        }

        private static string ToBytes(float value)
        {
            var bytes = BitConverter.GetBytes(value);
            var strBytes = BitConverter.ToString(bytes);
            return $"0x{strBytes.Replace("-", ",0x")}";
        }
    }

    public class IntegerData
    {
        public string Label { get; set; }
        public int Value { get; set; }
        public IntegerData(string label, int value)
        {
            Label = label;
            Value = value;
        }

        public string Emit(int indentLevel, CompilationOptions options)
        {
            return $"{Label} dd {Value}".Indent(indentLevel);
        }
    }

    public class PointerData
    {
        public string Label { get; set; }
        public int Value { get; set; }
        public PointerData(string label, int value)
        {
            Label = label;
            Value = value;
        }

        public string Emit(int indentLevel, CompilationOptions options)
        {
            return $"{Label} dd {Value}".Indent(indentLevel);
        }
    }

    public class ByteData
    {
        public string Label { get; set; }
        public byte Value { get; set; }
        public ByteData(string label, byte value)
        {
            Label = label;
            Value = value;
        }

        public string Emit(int indentLevel, CompilationOptions options)
        {
            return $"{Label} db {EscapeByte(Value)}".Indent(indentLevel);
        }

        private static string EscapeByte(byte value)
        {
            var strBytes = BitConverter.ToString([value]);
            return $"0x{strBytes.Replace("-", ",0x")}";
        }
    }

    public class UnitializedData
    {
        public string Label { get; set; }
        public int BytesToReserve { get; set; }
        public UnitializedData(string label, int bytesToReserve)
        {
            Label = label;
            BytesToReserve = bytesToReserve;
        }

        public string Emit(int indentLevel, CompilationOptions options)
        {
            return $"{Label} rb {BytesToReserve}".Indent(indentLevel);
        }
    }

    public class IconData
    {
        public string FilePath { get; set; }

        public IconData(string filePath)
        {
            FilePath = filePath;
        }
    }


    #endregion

}