using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Statements;
using Language.Experimental.Expressions;
using ParserLite;
using ParserLite.Exceptions;
using System.Runtime.InteropServices;
using TokenizerCore;
using TokenizerCore.Interfaces;
using TokenizerCore.Models.Constants;

namespace Language.Experimental.Parser;

public enum InstructionParseUnit
{
    GeneralRegister32,
    XmmRegister,
    RegisterOffset,
    SymbolOffset,
    Symbol,
    Immediate,
    ByteRegister,
    RegisterOffset_Byte,
    SymbolOffset_Byte,
}
public class AssemblyInstructionParsingRule
{
    public AssemblyInstruction AssemblyInstruction { get; set; }
    public List<InstructionParseUnit> ParseUnits { get; set; }
    public Func<ProgramParser, X86Instruction> InstructionParsingFunction { get; set; }

    public AssemblyInstructionParsingRule(AssemblyInstruction assemblyInstruction, List<InstructionParseUnit> parseUnits, Func<ProgramParser, X86Instruction> instructionParsingFunction)
    {
        AssemblyInstruction = assemblyInstruction;
        InstructionParsingFunction = instructionParsingFunction;
        ParseUnits = parseUnits;
    }

    public bool CanParse(ProgramParser programParser)
    {
        int tokenOffset = 0;
        if (!programParser.CanParse(AssemblyInstruction, ref tokenOffset)) return false;
        foreach(var parseUnit in ParseUnits)
        {
            if (!programParser.CanParse(parseUnit, ref tokenOffset)) return false;
        }
        return true;
    }

    public X86Instruction Parse(ProgramParser programParser)
    {
        return InstructionParsingFunction(programParser);
    }
}

public class ProgramParser : TokenParser
{
    private Tokenizer _tokenizer;
    private Dictionary<string, GenericTypeSymbol> _validScopedGenericTypeParameters = new();
    private List<AssemblyInstructionParsingRule> _assemblyParsingRules = new()
    {
                        new(AssemblyInstruction.Cdq, [],
                        (p) => {
                                return X86Instructions.Cdq();
                        }),
                new(AssemblyInstruction.Push, [InstructionParseUnit.GeneralRegister32],
                        (p) => {
                                var register = p.ParseGeneralRegister32();
                                return X86Instructions.Push(register);
                        }),
                new(AssemblyInstruction.Push, [InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var offset = p.ParseRegisterOffset();
                                return X86Instructions.Push(offset);
                        }),
                new(AssemblyInstruction.Push, [InstructionParseUnit.Symbol],
                        (p) => {
                                var address = p.ParseSymbol();
                                return X86Instructions.Push(address);
                        }),
                new(AssemblyInstruction.Push, [InstructionParseUnit.Immediate],
                        (p) => {
                                var immediateValue = p.ParseImmediate();
                                return X86Instructions.Push(immediateValue);
                        }),
                new(AssemblyInstruction.Push, [InstructionParseUnit.SymbolOffset],
                        (p) => {
                                var offset = p.ParseSymbolOffset();
                                return X86Instructions.Push(offset);
                        }),
                new(AssemblyInstruction.Lea, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var source = p.ParseRegisterOffset();
                                return X86Instructions.Lea(destination,source);
                        }),
                new(AssemblyInstruction.Lea, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.SymbolOffset],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var source = p.ParseSymbolOffset();
                                return X86Instructions.Lea(destination,source);
                        }),
                new(AssemblyInstruction.Mov, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var source = p.ParseRegisterOffset();
                                return X86Instructions.Mov(destination,source);
                        }),
                new(AssemblyInstruction.Mov, [InstructionParseUnit.RegisterOffset,InstructionParseUnit.GeneralRegister32],
                        (p) => {
                                var destination = p.ParseRegisterOffset();
                                var source = p.ParseGeneralRegister32();
                                return X86Instructions.Mov(destination,source);
                        }),
                new(AssemblyInstruction.Mov, [InstructionParseUnit.RegisterOffset,InstructionParseUnit.Immediate],
                        (p) => {
                                var destination = p.ParseRegisterOffset();
                                var immediate = p.ParseImmediate();
                                return X86Instructions.Mov(destination,immediate);
                        }),
                new(AssemblyInstruction.Mov, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.GeneralRegister32],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var source = p.ParseGeneralRegister32();
                                return X86Instructions.Mov(destination,source);
                        }),
                new(AssemblyInstruction.Mov, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.Immediate],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var immediate = p.ParseImmediate();
                                return X86Instructions.Mov(destination,immediate);
                        }),
                new(AssemblyInstruction.Mov, [InstructionParseUnit.SymbolOffset,InstructionParseUnit.GeneralRegister32],
                        (p) => {
                                var destination = p.ParseSymbolOffset();
                                var source = p.ParseGeneralRegister32();
                                return X86Instructions.Mov(destination,source);
                        }),
                new(AssemblyInstruction.Mov, [InstructionParseUnit.SymbolOffset,InstructionParseUnit.ByteRegister],
                        (p) => {
                                var destination = p.ParseSymbolOffset();
                                var source = p.ParseByteRegister();
                                return X86Instructions.Mov(destination,source);
                        }),
                new(AssemblyInstruction.Mov, [InstructionParseUnit.SymbolOffset,InstructionParseUnit.Immediate],
                        (p) => {
                                var destination = p.ParseSymbolOffset();
                                var immediateValue = p.ParseImmediate();
                                return X86Instructions.Mov(destination,immediateValue);
                        }),
                new(AssemblyInstruction.Mov, [InstructionParseUnit.SymbolOffset_Byte,InstructionParseUnit.ByteRegister],
                        (p) => {
                                var destination = p.ParseSymbolOffset_Byte();
                                var source = p.ParseByteRegister();
                                return X86Instructions.Mov(destination,source);
                        }),
                new(AssemblyInstruction.Mov, [InstructionParseUnit.RegisterOffset_Byte,InstructionParseUnit.ByteRegister],
                        (p) => {
                                var destination = p.ParseRegisterOffset_Byte();
                                var source = p.ParseByteRegister();
                                return X86Instructions.Mov(destination,source);
                        }),
                new(AssemblyInstruction.Mov, [InstructionParseUnit.RegisterOffset,InstructionParseUnit.ByteRegister],
                        (p) => {
                                var destination = p.ParseRegisterOffset();
                                var source = p.ParseByteRegister();
                                return X86Instructions.Mov(destination,source);
                        }),
                new(AssemblyInstruction.Movsx, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.RegisterOffset_Byte],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var source = p.ParseRegisterOffset_Byte();
                                return X86Instructions.Movsx(destination,source);
                        }),
                new(AssemblyInstruction.Movsx, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.SymbolOffset_Byte],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var source = p.ParseSymbolOffset_Byte();
                                return X86Instructions.Movsx(destination,source);
                        }),
                new(AssemblyInstruction.Sub, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.Immediate],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var valueToSubtract = p.ParseImmediate();
                                return X86Instructions.Sub(destination,valueToSubtract);
                        }),
                new(AssemblyInstruction.Sub, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.GeneralRegister32],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var source = p.ParseGeneralRegister32();
                                return X86Instructions.Sub(destination,source);
                        }),
                new(AssemblyInstruction.Add, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.Immediate],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var value = p.ParseImmediate();
                                return X86Instructions.Add(destination,value);
                        }),
                new(AssemblyInstruction.Add, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.GeneralRegister32],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var source = p.ParseGeneralRegister32();
                                return X86Instructions.Add(destination,source);
                        }),
                new(AssemblyInstruction.And, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.GeneralRegister32],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var source = p.ParseGeneralRegister32();
                                return X86Instructions.And(destination,source);
                        }),
                new(AssemblyInstruction.Or, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.GeneralRegister32],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var source = p.ParseGeneralRegister32();
                                return X86Instructions.Or(destination,source);
                        }),
                new(AssemblyInstruction.Xor, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.GeneralRegister32],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var source = p.ParseGeneralRegister32();
                                return X86Instructions.Xor(destination,source);
                        }),
                new(AssemblyInstruction.Pop, [InstructionParseUnit.GeneralRegister32],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                return X86Instructions.Pop(destination);
                        }),
                new(AssemblyInstruction.Neg, [InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var destination = p.ParseRegisterOffset();
                                return X86Instructions.Neg(destination);
                        }),
                new(AssemblyInstruction.Not, [InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var destination = p.ParseRegisterOffset();
                                return X86Instructions.Not(destination);
                        }),
                new(AssemblyInstruction.Inc, [InstructionParseUnit.GeneralRegister32],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                return X86Instructions.Inc(destination);
                        }),
                new(AssemblyInstruction.Dec, [InstructionParseUnit.GeneralRegister32],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                return X86Instructions.Dec(destination);
                        }),
                new(AssemblyInstruction.Inc, [InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var destination = p.ParseRegisterOffset();
                                return X86Instructions.Inc(destination);
                        }),
                new(AssemblyInstruction.Dec, [InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var destination = p.ParseRegisterOffset();
                                return X86Instructions.Dec(destination);
                        }),
                new(AssemblyInstruction.IDiv, [InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var divisor = p.ParseRegisterOffset();
                                return X86Instructions.IDiv(divisor);
                        }),
                new(AssemblyInstruction.IMul, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.GeneralRegister32],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var source = p.ParseGeneralRegister32();
                                return X86Instructions.IMul(destination,source);
                        }),
                new(AssemblyInstruction.IMul, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.Immediate],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var immediate = p.ParseImmediate();
                                return X86Instructions.IMul(destination,immediate);
                        }),
                new(AssemblyInstruction.Add, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var source = p.ParseRegisterOffset();
                                return X86Instructions.Add(destination,source);
                        }),
                new(AssemblyInstruction.Jmp, [InstructionParseUnit.Symbol],
                        (p) => {
                                var label = p.ParseSymbol();
                                return X86Instructions.Jmp(label);
                        }),
                new(AssemblyInstruction.JmpGt, [InstructionParseUnit.Symbol],
                        (p) => {
                                var label = p.ParseSymbol();
                                return X86Instructions.JmpGt(label);
                        }),
                new(AssemblyInstruction.JmpGte, [InstructionParseUnit.Symbol],
                        (p) => {
                                var label = p.ParseSymbol();
                                return X86Instructions.JmpGte(label);
                        }),
                new(AssemblyInstruction.JmpLt, [InstructionParseUnit.Symbol],
                        (p) => {
                                var label = p.ParseSymbol();
                                return X86Instructions.JmpLt(label);
                        }),
                new(AssemblyInstruction.JmpLte, [InstructionParseUnit.Symbol],
                        (p) => {
                                var label = p.ParseSymbol();
                                return X86Instructions.JmpLte(label);
                        }),
                new(AssemblyInstruction.JmpEq, [InstructionParseUnit.Symbol],
                        (p) => {
                                var label = p.ParseSymbol();
                                return X86Instructions.JmpEq(label);
                        }),
                new(AssemblyInstruction.JmpNeq, [InstructionParseUnit.Symbol],
                        (p) => {
                                var label = p.ParseSymbol();
                                return X86Instructions.JmpNeq(label);
                        }),
                new(AssemblyInstruction.Jz, [InstructionParseUnit.Symbol],
                        (p) => {
                                var label = p.ParseSymbol();
                                return X86Instructions.Jz(label);
                        }),
                new(AssemblyInstruction.Jnz, [InstructionParseUnit.Symbol],
                        (p) => {
                                var label = p.ParseSymbol();
                                return X86Instructions.Jnz(label);
                        }),
                new(AssemblyInstruction.Js, [InstructionParseUnit.Symbol],
                        (p) => {
                                var label = p.ParseSymbol();
                                return X86Instructions.Js(label);
                        }),
                new(AssemblyInstruction.Jns, [InstructionParseUnit.Symbol],
                        (p) => {
                                var label = p.ParseSymbol();
                                return X86Instructions.Jns(label);
                        }),
                new(AssemblyInstruction.Ja, [InstructionParseUnit.Symbol],
                        (p) => {
                                var label = p.ParseSymbol();
                                return X86Instructions.Ja(label);
                        }),
                new(AssemblyInstruction.Jae, [InstructionParseUnit.Symbol],
                        (p) => {
                                var label = p.ParseSymbol();
                                return X86Instructions.Jae(label);
                        }),
                new(AssemblyInstruction.Jb, [InstructionParseUnit.Symbol],
                        (p) => {
                                var label = p.ParseSymbol();
                                return X86Instructions.Jb(label);
                        }),
                new(AssemblyInstruction.Jbe, [InstructionParseUnit.Symbol],
                        (p) => {
                                var label = p.ParseSymbol();
                                return X86Instructions.Jbe(label);
                        }),
                new(AssemblyInstruction.Test, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.GeneralRegister32],
                        (p) => {
                                var operand1 = p.ParseGeneralRegister32();
                                var operand2 = p.ParseGeneralRegister32();
                                return X86Instructions.Test(operand1,operand2);
                        }),
                new(AssemblyInstruction.Test, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var operand1 = p.ParseGeneralRegister32();
                                var operand2 = p.ParseRegisterOffset();
                                return X86Instructions.Test(operand1,operand2);
                        }),
                new(AssemblyInstruction.Cmp, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.GeneralRegister32],
                        (p) => {
                                var operand1 = p.ParseGeneralRegister32();
                                var operand2 = p.ParseGeneralRegister32();
                                return X86Instructions.Cmp(operand1,operand2);
                        }),
                new(AssemblyInstruction.Cmp, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.Immediate],
                        (p) => {
                                var operand1 = p.ParseGeneralRegister32();
                                var operand2 = p.ParseImmediate();
                                return X86Instructions.Cmp(operand1,operand2);
                        }),
                new(AssemblyInstruction.Cmp, [InstructionParseUnit.ByteRegister,InstructionParseUnit.ByteRegister],
                        (p) => {
                                var operand1 = p.ParseByteRegister();
                                var operand2 = p.ParseByteRegister();
                                return X86Instructions.Cmp(operand1,operand2);
                        }),
                new(AssemblyInstruction.Call, [InstructionParseUnit.Symbol],
                        (p) => {
                                var callee = p.ParseSymbol();
                                return X86Instructions.Call(callee, false);
                        }),
                new(AssemblyInstruction.Call, [InstructionParseUnit.SymbolOffset],
                        (p) => {
                                var callee = p.ParseSymbolOffset();
                                return X86Instructions.Call(callee.Symbol, true);
                        }),
                new(AssemblyInstruction.Call, [InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var callee = p.ParseRegisterOffset();
                                return X86Instructions.Call(callee);
                        }),
                new(AssemblyInstruction.Call, [InstructionParseUnit.GeneralRegister32],
                        (p) => {
                                var callee = p.ParseGeneralRegister32();
                                return X86Instructions.Call(callee);
                        }),
                new(AssemblyInstruction.Label, [InstructionParseUnit.Symbol],
                        (p) => {
                                var text = p.ParseSymbol();
                                return X86Instructions.Label(text);
                        }),
                new(AssemblyInstruction.Ret, [],
                        (p) => {
                                return X86Instructions.Ret();
                        }),
                new(AssemblyInstruction.Ret, [InstructionParseUnit.Immediate],
                        (p) => {
                                var immediate = p.ParseImmediate();
                                return X86Instructions.Ret(immediate);
                        }),
                new(AssemblyInstruction.Fstp, [InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var destination = p.ParseRegisterOffset();
                                return X86Instructions.Fstp(destination);
                        }),
                new(AssemblyInstruction.Fld, [InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var source = p.ParseRegisterOffset();
                                return X86Instructions.Fld(source);
                        }),
                new(AssemblyInstruction.Movss, [InstructionParseUnit.RegisterOffset,InstructionParseUnit.XmmRegister],
                        (p) => {
                                var destination = p.ParseRegisterOffset();
                                var source = p.ParseXmmRegister();
                                return X86Instructions.Movss(destination,source);
                        }),
                new(AssemblyInstruction.Movss, [InstructionParseUnit.XmmRegister,InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var destination = p.ParseXmmRegister();
                                var source = p.ParseRegisterOffset();
                                return X86Instructions.Movss(destination,source);
                        }),
                new(AssemblyInstruction.Movss, [InstructionParseUnit.XmmRegister,InstructionParseUnit.XmmRegister],
                        (p) => {
                                var destination = p.ParseXmmRegister();
                                var source = p.ParseXmmRegister();
                                return X86Instructions.Movss(destination,source);
                        }),
                new(AssemblyInstruction.Comiss, [InstructionParseUnit.XmmRegister,InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var destination = p.ParseXmmRegister();
                                var source = p.ParseRegisterOffset();
                                return X86Instructions.Comiss(destination,source);
                        }),
                new(AssemblyInstruction.Comiss, [InstructionParseUnit.XmmRegister,InstructionParseUnit.XmmRegister],
                        (p) => {
                                var destination = p.ParseXmmRegister();
                                var source = p.ParseXmmRegister();
                                return X86Instructions.Comiss(destination,source);
                        }),
                new(AssemblyInstruction.Ucomiss, [InstructionParseUnit.XmmRegister,InstructionParseUnit.XmmRegister],
                        (p) => {
                                var destination = p.ParseXmmRegister();
                                var source = p.ParseXmmRegister();
                                return X86Instructions.Ucomiss(destination,source);
                        }),
                new(AssemblyInstruction.Addss, [InstructionParseUnit.XmmRegister,InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var destination = p.ParseXmmRegister();
                                var source = p.ParseRegisterOffset();
                                return X86Instructions.Addss(destination,source);
                        }),
                new(AssemblyInstruction.Subss, [InstructionParseUnit.XmmRegister,InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var destination = p.ParseXmmRegister();
                                var source = p.ParseRegisterOffset();
                                return X86Instructions.Subss(destination,source);
                        }),
                new(AssemblyInstruction.Mulss, [InstructionParseUnit.XmmRegister,InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var destination = p.ParseXmmRegister();
                                var source = p.ParseRegisterOffset();
                                return X86Instructions.Mulss(destination,source);
                        }),
                new(AssemblyInstruction.Divss, [InstructionParseUnit.XmmRegister,InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var destination = p.ParseXmmRegister();
                                var source = p.ParseRegisterOffset();
                                return X86Instructions.Divss(destination,source);
                        }),
                new(AssemblyInstruction.Cvtsi2ss, [InstructionParseUnit.XmmRegister,InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var destination = p.ParseXmmRegister();
                                var source = p.ParseRegisterOffset();
                                return X86Instructions.Cvtsi2ss(destination,source);
                        }),
                new(AssemblyInstruction.Cvtss2si, [InstructionParseUnit.GeneralRegister32,InstructionParseUnit.RegisterOffset],
                        (p) => {
                                var destination = p.ParseGeneralRegister32();
                                var source = p.ParseRegisterOffset();
                                return X86Instructions.Cvtss2si(destination,source);
                        }),
    };
    public ProgramParser(Tokenizer tokenizer)
    {
        _tokenizer = tokenizer;
        OverrideCurrentOnNull = true;
    }

    public ProgramParser()
    {
        _tokenizer = Tokenizers.Default;
        OverrideCurrentOnNull = true;
    }
    public ParsingResult ParseFile(string path, out List<ParsingException> errors)
    {
        return ParseText(File.ReadAllText(path), out errors);
    }

    public ParsingResult ParseText(string text, out List<ParsingException> errors)
    {
        var tokenizer = Tokenizers.Default;
        errors = new List<ParsingException>();
        var result = new ParsingResult();

        var tokens = tokenizer.Tokenize(text, false)
            .Where(token => token.Type != BuiltinTokenTypes.EndOfFile)
            .ToList();

        Initialize(tokens);
        while (!AtEnd())
        {
            try
            {
                _validScopedGenericTypeParameters = new();
                var next = ParseNext();
                if (next == null) break;
                if (next is TypeDefinition typeDefinition) result.TypeDefinitions.Add(typeDefinition);
                else if (next is GenericTypeDefinition genericTypeDefinition) result.GenericTypeDefinitions.Add(genericTypeDefinition);
                else if (next is FunctionDefinition functionDefinition) result.FunctionDefinitions.Add(functionDefinition);
                else if (next is GenericFunctionDefinition genericFunctionDefinition) result.GenericFunctionDefinitions.Add(genericFunctionDefinition);
                else if (next is ImportedFunctionDefinition importedFunctionDefinition) result.ImportedFunctionDefinitions.Add(importedFunctionDefinition);
                else if (next is ImportLibraryDefinition importLibraryDefinition) result.ImportLibraryDefinitions.Add(importLibraryDefinition);
                else throw new ParsingException(next.Token, $"unsupported statement type {next.GetType().Name}");
            }
            catch (ParsingException e)
            {
                errors.Add(e);
                SeekToNextParsableUnit();
            }
        }
        return result;
    }

    private void SeekToNextParsableUnit()
    {
        while (!AtEnd())
        {
            Advance();
            if (Match(TokenTypes.LParen)) break;
        }
    }

    private StatementBase? ParseNext()
    {
        var startToken = Current();
        var statement = ParseStatement();
        if (statement == null) return null;
        var endToken = Previous();
        statement.StartToken = startToken;
        statement.EndToken = endToken;
        return statement;
    }

    private StatementBase ParseTypeDefinition()
    {
        var name = Consume(BuiltinTokenTypes.Word, "expect type name");
        List<GenericTypeSymbol> genericTypeParameters = new();
        if (AdvanceIfMatch(TokenTypes.LBracket))
        {
            do
            {
                genericTypeParameters.Add(ParseGenericTypeSymbol());
            } while (AdvanceIfMatch(TokenTypes.Comma));
            Consume(TokenTypes.RBracket, "expect enclosing ] after generic type parameter list");
        }
        var fields = new List<TypeDefinitionField>();
        do
        {
            Consume(TokenTypes.LParen, "expect field definition");
            Consume(TokenTypes.Field, "expect field definition. IE (field x int)");
            var fieldName = Consume(BuiltinTokenTypes.Word, "expect field name");
            var typeInfo = ParseTypeSymbol();
            
            Consume(TokenTypes.RParen, "expect enclosing ) in field definition");
            fields.Add(new(typeInfo, fieldName));
        } while (!AtEnd() && !Match(TokenTypes.RParen));
        Consume(TokenTypes.RParen, "expect enclosing ) in type definition");
        if (genericTypeParameters.Any()) return new GenericTypeDefinition(name, genericTypeParameters, fields);
        return new TypeDefinition(name, fields);
    }

    public TypeSymbol ParseTypeSymbol()
    {
        IToken typeName;
        if (AdvanceIfMatch(TokenTypes.IntrinsicType)) typeName = Previous();
        else typeName = Consume(BuiltinTokenTypes.Word, "expect type annotation");
        if (_validScopedGenericTypeParameters.TryGetValue(typeName.Lexeme, out var genericTypeSymbol)) return genericTypeSymbol;
        List<TypeSymbol> typeArguments = new();
        if (AdvanceIfMatch(TokenTypes.LBracket))
        {
            do
            {
                var typeArgument = ParseTypeSymbol();
                typeArguments.Add(typeArgument);
            } while (AdvanceIfMatch(TokenTypes.Comma));
            Consume(TokenTypes.RBracket, "expect enclosing ] after type arguments");
        }
        
        return new TypeSymbol(typeName, typeArguments);
    }

    private GenericTypeSymbol ParseGenericTypeSymbol()
    {
        Consume(TokenTypes.Gen, "expect generic symbol");
        var typeSymbol = Consume(BuiltinTokenTypes.Word, "expect type annotation");
        if (_validScopedGenericTypeParameters.ContainsKey(typeSymbol.Lexeme))
            throw new ParsingException(typeSymbol, $"redefintion of generic parameter with name {typeSymbol.Lexeme}");
        var genericTypeSymbol = new GenericTypeSymbol(typeSymbol);
        _validScopedGenericTypeParameters[typeSymbol.Lexeme] = genericTypeSymbol;
        return genericTypeSymbol;
    }

    public StatementBase? ParseStatement()
    {
        if (AtEnd()) return null;
        Consume(TokenTypes.LParen, "expect all statements to begin with (");
        if (AdvanceIfMatch(TokenTypes.DefineFunction)) return ParseFunctionDefinition();
        if (AdvanceIfMatch(TokenTypes.Import)) return ParseImportedFunctionDefinition();
        if (AdvanceIfMatch(TokenTypes.Library)) return ParseImportLibraryDefinition();
        if (AdvanceIfMatch(TokenTypes.Type)) return ParseTypeDefinition();
        throw new ParsingException(Current(), $"unexpected token {Current()}");
    }

    public StatementBase ParseFunctionDefinition(bool isLambda = false)
    {
        /*
         * (defn main:int (param argc int) (param argv ptr[string]
         *  //...
         * )
         * 
         *  (defn add[gen T]:T (param x gen T) (param y gen T)
         *  //...
         *  )
         */
        IToken name;
        var genericTypeParameters = new List<GenericTypeSymbol>();
        if (!isLambda)
        {
            name = Consume(BuiltinTokenTypes.Word, "expect function name");
            if (AdvanceIfMatch(TokenTypes.LBracket))
            {
                do
                {
                    genericTypeParameters.Add(ParseGenericTypeSymbol());
                } while (AdvanceIfMatch(TokenTypes.Comma));
                Consume(TokenTypes.RBracket, "expect enclosing ] after generic type parameter list");
            }
        }
        else name = Previous();
        Consume(TokenTypes.Colon, "expect functionName:returnType");
        var returnType = ParseTypeSymbol();
        var parameters = new List<Parameter>();
        if (Match(TokenTypes.LParen) && PeekMatch(1, TokenTypes.Param))
        {
            do
            {
                Consume(TokenTypes.LParen, "expect parameter definition");
                Consume(TokenTypes.Param, "expect parameter definition");
                var parameterName = Consume(BuiltinTokenTypes.Word, "expect parameter name");
                var parameterType = ParseTypeSymbol();
                Consume(TokenTypes.RParen, "expect enclosing ) in parameter definition");
                parameters.Add(new(parameterName, parameterType));
            } while(!AtEnd() && Match(TokenTypes.LParen) && PeekMatch(1, TokenTypes.Param));
        }
        var body = new List<ExpressionBase>();
        if (!AdvanceIfMatch(TokenTypes.RParen))
        {
            do
            {
                body.Add(ParseExpression());
            } while (!AtEnd() && !Match(TokenTypes.RParen));
            Consume(TokenTypes.RParen, "expect enclosing ) in function body");
        }
        if (genericTypeParameters.Any()) return new GenericFunctionDefinition(name, genericTypeParameters, returnType, parameters, body);
        return new FunctionDefinition(name, returnType, parameters, body);
    }

    public FunctionDefinition ParseLambdaFunctionDefinition()
    {
        /*
         * (defn int (param argc int) (param argv ptr[string])
         *  //...
         * )
         * 
         */
        IToken name = Previous();   
        var returnType = ParseTypeSymbol();
        var parameters = new List<Parameter>();
        if (Match(TokenTypes.LParen) && PeekMatch(1, TokenTypes.Param))
        {
            do
            {
                Consume(TokenTypes.LParen, "expect parameter definition");
                Consume(TokenTypes.Param, "expect parameter definition");
                var parameterName = Consume(BuiltinTokenTypes.Word, "expect parameter name");
                var parameterType = ParseTypeSymbol();
                Consume(TokenTypes.RParen, "expect enclosing ) in parameter definition");
                parameters.Add(new(parameterName, parameterType));
            } while (!AtEnd() && Match(TokenTypes.LParen) && PeekMatch(1, TokenTypes.Param));
        }
        var body = new List<ExpressionBase>();
        if (!AdvanceIfMatch(TokenTypes.RParen))
        {
            do
            {
                body.Add(ParseExpression());
            } while (!AtEnd() && !Match(TokenTypes.RParen));
            Consume(TokenTypes.RParen, "expect enclosing ) in function body");
        }
        return new FunctionDefinition(name, returnType, parameters, body);
    }



    public ImportedFunctionDefinition ParseImportedFunctionDefinition()
    {
        /*
         * (import mscvrt cdecl (symbol `_printf`) 
         *          printf:void (param string s))
         * 
         * 
         */
        var libraryAlias = Consume(BuiltinTokenTypes.Word, "expect import library alias");
        var callingConvention = ParseCallingConvention();
        var functionName = Consume(BuiltinTokenTypes.Word, "expect function name");
        Consume(TokenTypes.Colon, "expect functionName:returnType");
        var returnType = ParseTypeSymbol();
        IToken importSymbol = functionName;
        if (Match(TokenTypes.LParen) && PeekMatch(1, TokenTypes.Symbol))
        {
            Advance();
            Advance();
            importSymbol = Consume(BuiltinTokenTypes.Word, "expect import symbol");
            Consume(TokenTypes.RParen, "expect enclosing ) in symbol annotation");
        }
        var parameters = new List<Parameter>();
        if (Match(TokenTypes.LParen) && PeekMatch(1, TokenTypes.Param))
        {
            do
            {
                Consume(TokenTypes.LParen, "expect parameter definition");
                Consume(TokenTypes.Param, "expect parameter definition");
                var parameterName = Consume(BuiltinTokenTypes.Word, "expect parameter name");
                var parameterType = ParseTypeSymbol();
                Consume(TokenTypes.RParen, "expect enclosing ) in parameter definition");
                parameters.Add(new(parameterName, parameterType));
            } while (!AtEnd() && Match(TokenTypes.LParen) && PeekMatch(1, TokenTypes.Param));
        }
        Consume(TokenTypes.RParen, "expect enclosing ) after imported function definition");
        return new ImportedFunctionDefinition(functionName, returnType, parameters, callingConvention, libraryAlias, importSymbol);
    }

    public ImportLibraryDefinition ParseImportLibraryDefinition()
    {
        /*
         * (library mscvrt `msvcrt.dll`)
         * 
         * 
         */
        var libraryAlias = Consume(BuiltinTokenTypes.Word, "expect import library alias");
        var libraryPath = Consume(BuiltinTokenTypes.Word, "expect path to dll");
        Consume(TokenTypes.RParen, "expect enclosing ) after import library definition");    

        return new ImportLibraryDefinition(libraryAlias, libraryPath);
    }

    private CallingConvention ParseCallingConvention()
    {
        if (!AdvanceIfMatch(TokenTypes.CallingConvention))
            throw new ParsingException(Current(), "expect calling convention");
        if (!Enum.TryParse<CallingConvention>(Previous().Lexeme, true, out var callingConvention))
            throw new ParsingException(Previous(), $"unsupported calling convention {Previous().Lexeme}");
        return callingConvention;
    }

    public ExpressionBase ParseExpression()
    {
        var startToken = Current();       
        ExpressionBase expression;
        if (AdvanceIfMatch(TokenTypes.LBracket)) expression = ParseCast();
        else expression = ParseCall();
        var endToken = Previous();
        expression.StartToken = startToken;
        expression.EndToken = endToken;
        return expression;
    }

    private ExpressionBase ParseCast()
    {
        var token = Previous();
        var typeInfo = ParseTypeSymbol();
        Consume(TokenTypes.RBracket, "expect enclosing ] in cast");
        var expression = ParseExpression();
        return new CastExpression(token, typeInfo, expression);
    }
    private ExpressionBase ParseCall()
    {
        if (AdvanceIfMatch(TokenTypes.LParen))
        {
            if (AdvanceIfMatch(TokenTypes.CompilerIntrinsicGet)) return ParseCompilerIntrinsicGet();
            if (AdvanceIfMatch(TokenTypes.CompilerIntrinsicSet)) return ParseCompilerIntrinsicSet();
            if (AdvanceIfMatch(TokenTypes.Return)) return ParseReturn();
            if (AdvanceIfMatch(TokenTypes.DefineFunction)) return ParseLambdaFunction();
            if (AdvanceIfMatch(TokenTypes.Local)) return ParseLocalVariable();
            if (AdvanceIfMatch(TokenTypes.Set)) return ParseSet();
            if (Match(TokenTypes.AssemblyInstruction)) return ParseAssemblyInstruction();
            var token = Previous();
            if (AdvanceIfMatch(TokenTypes.RParen))
                throw new ParsingException(Previous(), "empty call encountered");
            var callTarget = ParseExpression();
            var arguments = new List<ExpressionBase>();
            if (!AdvanceIfMatch(TokenTypes.RParen))
            {
                do
                {
                    arguments.Add(ParseExpression());
                } while (!AtEnd() && !Match(TokenTypes.RParen));
                Consume(TokenTypes.RParen, "expect enclosing ) after call");
            }
            return new CallExpression(token, callTarget, arguments);
        }
        else return ParseGet();

    }

    private ExpressionBase ParseLocalVariable()
    {
        var token = Consume(BuiltinTokenTypes.Word, "expect local variable identifier");
        var type = ParseTypeSymbol();
        ExpressionBase? initializer = null;
        if (!AdvanceIfMatch(TokenTypes.RParen))
        {
            initializer = ParseExpression();
            Consume(TokenTypes.RParen, "expect enclosing ) in local variable definition");
        }

        return new LocalVariableExpression(token, type, token, initializer);
    }

    private ExpressionBase ParseCompilerIntrinsicGet()
    {
        var token = Previous(); 
        Consume(TokenTypes.Colon, "expect _ci_get:returnType");
        var returnType = ParseTypeSymbol();
        var contextPointer = ParseExpression();
        int offset = int.Parse(Consume(BuiltinTokenTypes.Integer, "expect integer offset").Lexeme);
        Consume(TokenTypes.RParen, "expect enclosing ) after call to _ci_get");
        return new CompilerIntrinsic_GetExpression(token, returnType, contextPointer, offset);
    }

    private ExpressionBase ParseCompilerIntrinsicSet()
    {
        var token = Previous();
        var contextPointer = ParseExpression();
        int offset = int.Parse(Consume(BuiltinTokenTypes.Integer, "expect integer offset to memory location").Lexeme);
        var valueToAssign = ParseExpression();
        Consume(TokenTypes.RParen, "expect enclosing ) after call to _ci_set");
        return new CompilerIntrinsic_SetExpression(token, contextPointer, offset, valueToAssign);
    }

    private ExpressionBase ParseReturn()
    {
        var token = Previous();
        ExpressionBase? returnValue = null;
        if (!AdvanceIfMatch(TokenTypes.RParen))
        {
            returnValue = ParseExpression();
            Consume(TokenTypes.RParen, "expect enclosing ) after return statement");
        }
        return new ReturnExpression(token, returnValue);
    }

    private ExpressionBase ParseLambdaFunction()
    {
        var startToken = Current();
        var functionDefinition = ParseLambdaFunctionDefinition();
        var endToken = Previous();
        functionDefinition.StartToken = startToken;
        functionDefinition.EndToken = endToken;
        return new LambdaExpression(startToken, functionDefinition);
    }

    private ExpressionBase ParseSet()
    {
        var assignmentTarget = ParseExpression();
        var valueToAssign = ParseExpression();
        Consume(TokenTypes.RParen, "expect enclosing ) after set statement");
        return new SetExpression(Previous(), assignmentTarget, valueToAssign);
    }

    private ExpressionBase ParseGet()
    {
        var start = Current();
        var expr = ParsePrimary();
        var end = Previous();
        expr.StartToken = start;
        expr.EndToken = end;
        if (expr is IdentifierExpression identifierExpression && AdvanceIfMatch(TokenTypes.LBracket))
        {
            var typeArguments = new List<TypeSymbol>();
            if (!AdvanceIfMatch(TokenTypes.RBracket))
            {
                do
                {
                    typeArguments.Add(ParseTypeSymbol());
                } while (AdvanceIfMatch(TokenTypes.Comma));
                Consume(TokenTypes.RBracket, "expect enclosing ] after type arguments list");
            }
            // return here, no chaining allowed
            return new GenericFunctionReferenceExpression(identifierExpression.Token, typeArguments);
        }

        if (expr is IdentifierExpression || expr is GetExpression)
        {
            while (Match(TokenTypes.Dot) || Match(TokenTypes.NullDot))
            {
                if (AdvanceIfMatch(TokenTypes.Dot))
                {
                    var targetField = Consume(BuiltinTokenTypes.Word, "expect member name after '.'");
                    expr = new GetExpression(Previous(), expr, targetField, false);
                    expr.StartToken = start;
                    expr.EndToken = targetField;
                }
                else
                {
                    Advance();
                    var targetField = Consume(BuiltinTokenTypes.Word, "expect member name after '?.'");
                    expr = new GetExpression(Previous(), expr, targetField, true);
                    expr.StartToken = start;
                    expr.EndToken = targetField;
                }
            }
        }

        return expr;
    }

    private ExpressionBase ParsePrimary()
    {
        if (AdvanceIfMatch(BuiltinTokenTypes.Word)) return new IdentifierExpression(Previous());
        return ParseLiteral();
    }

    public LiteralExpression ParseLiteral()
    {
        if (AdvanceIfMatch(BuiltinTokenTypes.Integer))
        {
            return new LiteralExpression(Previous(), int.Parse(Previous().Lexeme));
        }
        if (AdvanceIfMatch(BuiltinTokenTypes.Float))
        {
            return new LiteralExpression(Previous(), float.Parse(Previous().Lexeme));
        }
        if (AdvanceIfMatch(BuiltinTokenTypes.String))
        {
            return new LiteralExpression(Previous(), Previous().Lexeme);
        }
        if (AdvanceIfMatch(TokenTypes.True))
        {
            return new LiteralExpression(Previous(), 1);
        }
        if (AdvanceIfMatch(TokenTypes.False))
        {
            return new LiteralExpression(Previous(), 0);
        }
        if (AdvanceIfMatch(TokenTypes.Null))
        {
            return new LiteralExpression(Previous(), null);
        }
        throw new ParsingException(Current(), $"encountered unexpected token {Current()}");
    }

    private ExpressionBase ParseAssemblyInstruction()
    {
        foreach (var assemblyParsingRule in _assemblyParsingRules)
        {
            if (assemblyParsingRule.CanParse(this))
            {
                var token = Previous();
                var instruction = assemblyParsingRule.Parse(this);
                Consume(TokenTypes.RParen, "expect enclosing ) after assembly instruction");
                return new InlineAssemblyExpression(token, instruction);
            }
        }

        throw new ParsingException(Previous(), "expect inline assembly instruction");
    }

    public bool CanParse(AssemblyInstruction instruction, ref int tokenOffset)
    {
        if (Match(TokenTypes.AssemblyInstruction))
        {
            if (!Enum.TryParse<AssemblyInstruction>(Current().Lexeme, true, out var assemblyInstruction))
                return false;
            tokenOffset += 1;
            return true;
        }
        return false;
    }

    public bool CanParse(InstructionParseUnit unit, ref int tokenOffset)
    {
        if (unit == InstructionParseUnit.Symbol) return PeekMatch(tokenOffset, BuiltinTokenTypes.Word);
        if (unit == InstructionParseUnit.Immediate) return PeekMatch(tokenOffset, BuiltinTokenTypes.Integer);
        if (unit == InstructionParseUnit.GeneralRegister32) return PeekMatch(tokenOffset, TokenTypes.GeneralRegister32);
        if (unit == InstructionParseUnit.XmmRegister) return PeekMatch(tokenOffset, TokenTypes.XmmRegister);
        if (unit == InstructionParseUnit.RegisterOffset) return CanParseRegisterOffset(ref tokenOffset);
        if (unit == InstructionParseUnit.SymbolOffset) return CanParseSymbolOffset(ref tokenOffset);
        if (unit == InstructionParseUnit.ByteRegister) return PeekMatch(tokenOffset, TokenTypes.ByteRegister);
        if (unit == InstructionParseUnit.SymbolOffset_Byte) return CanParseSymbolOffset_Byte(ref tokenOffset);
        if (unit == InstructionParseUnit.RegisterOffset_Byte) return CanParseRegisterOffset_Byte(ref tokenOffset);
        return false;
    }

    private bool CanParseRegisterOffset(ref int tokenOffset)
    {
        if (PeekMatch(tokenOffset, TokenTypes.RBracket) && PeekMatch(tokenOffset + 1, TokenTypes.GeneralRegister32) && (PeekMatch(tokenOffset + 2, TokenTypes.Plus) || PeekMatch(tokenOffset + 2, TokenTypes.Minus)) && PeekMatch(tokenOffset + 3, BuiltinTokenTypes.Integer) && PeekMatch(tokenOffset + 4, TokenTypes.RBracket))
        {
            tokenOffset += 5;
            return true;
        }
        if (PeekMatch(tokenOffset, TokenTypes.RBracket) && PeekMatch(tokenOffset + 1, TokenTypes.GeneralRegister32) && PeekMatch(tokenOffset + 2, TokenTypes.RBracket))
        {
            tokenOffset += 3;
            return true;
        }
        return false;
    }

    private bool CanParseRegisterOffset_Byte(ref int tokenOffset)
    {
        if (PeekMatch(tokenOffset, TokenTypes.Byte) && PeekMatch(tokenOffset + 1, TokenTypes.RBracket) && PeekMatch(tokenOffset + 2, TokenTypes.GeneralRegister32) && (PeekMatch(tokenOffset + 3, TokenTypes.Plus) || PeekMatch(tokenOffset + 3, TokenTypes.Minus)) && PeekMatch(tokenOffset + 4, BuiltinTokenTypes.Integer) && PeekMatch(tokenOffset + 5, TokenTypes.RBracket))
        {
            tokenOffset += 6;
            return true;
        }
        if (PeekMatch(tokenOffset, TokenTypes.Byte) && PeekMatch(tokenOffset + 1, TokenTypes.RBracket) && PeekMatch(tokenOffset + 2, TokenTypes.GeneralRegister32) && PeekMatch(tokenOffset + 3, TokenTypes.RBracket))
        {
            tokenOffset += 4;
            return true;
        }
        return false;
    }

    private bool CanParseSymbolOffset(ref int tokenOffset)
    {
        if (PeekMatch(tokenOffset, TokenTypes.RBracket) && PeekMatch(tokenOffset + 1, BuiltinTokenTypes.Word) && (PeekMatch(tokenOffset + 2, TokenTypes.Plus) || PeekMatch(tokenOffset + 2, TokenTypes.Minus)) && PeekMatch(tokenOffset + 3, BuiltinTokenTypes.Integer) && PeekMatch(tokenOffset + 4, TokenTypes.RBracket))
        {
            tokenOffset += 5;
            return true;
        }
        if (PeekMatch(tokenOffset, TokenTypes.RBracket) && PeekMatch(tokenOffset + 1, BuiltinTokenTypes.Word) && PeekMatch(tokenOffset + 2, TokenTypes.RBracket))
        {
            tokenOffset += 3;
            return true;
        }
        return false;
    }

    private bool CanParseSymbolOffset_Byte(ref int tokenOffset)
    {
        if (PeekMatch(tokenOffset, TokenTypes.Byte) && PeekMatch(tokenOffset + 1, TokenTypes.RBracket) && PeekMatch(tokenOffset + 2, BuiltinTokenTypes.Word) && (PeekMatch(tokenOffset + 3, TokenTypes.Plus) || PeekMatch(tokenOffset + 3, TokenTypes.Minus)) && PeekMatch(tokenOffset + 4, BuiltinTokenTypes.Integer) && PeekMatch(tokenOffset + 5, TokenTypes.RBracket))
        {
            tokenOffset += 6;
            return true;
        }
        if (PeekMatch(tokenOffset, TokenTypes.Byte) && PeekMatch(tokenOffset + 1, TokenTypes.RBracket) && PeekMatch(tokenOffset + 2, BuiltinTokenTypes.Word) && PeekMatch(tokenOffset + 3, TokenTypes.RBracket))
        {
            tokenOffset += 4;
            return true;
        }
        return false;
    }

    private RegisterOffset ParseRegisterOffset()
    {
        Consume(TokenTypes.LBracket, "expect register offset");
        var register = ParseGeneralRegister32();
        if (AdvanceIfMatch(TokenTypes.Plus))
        {
            Consume(BuiltinTokenTypes.Integer, "expect integer offset");
            var offset = int.Parse(Previous().Lexeme);
            Consume(TokenTypes.LBracket, "expect enclosing ] in register offset");
            return new RegisterOffset(register, offset);
        }
        else if (AdvanceIfMatch(TokenTypes.Minus))
        {
            Consume(BuiltinTokenTypes.Integer, "expect integer offset");
            var offset = int.Parse(Previous().Lexeme);
            Consume(TokenTypes.LBracket, "expect enclosing ] in register offset");
            return new RegisterOffset(register, -offset);
        }
        else if (AdvanceIfMatch(TokenTypes.RBracket)) return new RegisterOffset(register, 0);
        else throw new ParsingException(Previous(), "expect register offset");
    }

    private RegisterOffset_Byte ParseRegisterOffset_Byte()
    {
        Consume(TokenTypes.Byte, "expect byte offset");
        Consume(TokenTypes.LBracket, "expect register offset");
        var register = ParseGeneralRegister32();
        if (AdvanceIfMatch(TokenTypes.Plus))
        {
            Consume(BuiltinTokenTypes.Integer, "expect integer offset");
            var offset = int.Parse(Previous().Lexeme);
            Consume(TokenTypes.LBracket, "expect enclosing ] in register offset");
            return new RegisterOffset_Byte(register, offset);
        }
        else if (AdvanceIfMatch(TokenTypes.Minus))
        {
            Consume(BuiltinTokenTypes.Integer, "expect integer offset");
            var offset = int.Parse(Previous().Lexeme);
            Consume(TokenTypes.LBracket, "expect enclosing ] in register offset");
            return new RegisterOffset_Byte(register, -offset);
        }
        else if (AdvanceIfMatch(TokenTypes.RBracket)) return new RegisterOffset_Byte(register, 0);
        else throw new ParsingException(Previous(), "expect register offset");
    }

    private SymbolOffset ParseSymbolOffset()
    {
        Consume(TokenTypes.LBracket, "expect symbol offset");
        var symbol = Consume(BuiltinTokenTypes.Word, "expect symbol offset").Lexeme;
        if (AdvanceIfMatch(TokenTypes.Plus))
        {
            Consume(BuiltinTokenTypes.Integer, "expect integer offset");
            var offset = int.Parse(Previous().Lexeme);
            Consume(TokenTypes.LBracket, "expect enclosing ] in symbol offset");
            return new SymbolOffset(symbol, offset);
        }
        else if (AdvanceIfMatch(TokenTypes.Minus))
        {
            Consume(BuiltinTokenTypes.Integer, "expect integer offset");
            var offset = int.Parse(Previous().Lexeme);
            Consume(TokenTypes.LBracket, "expect enclosing ] in symbol offset");
            return new SymbolOffset(symbol, -offset);
        }
        else if (AdvanceIfMatch(TokenTypes.RBracket)) return new SymbolOffset(symbol, 0);
        else throw new ParsingException(Previous(), "expect symbol offset");
    }

    private SymbolOffset ParseSymbolOffset_Byte()
    {
        Consume(TokenTypes.Byte, "expect byte offset");
        Consume(TokenTypes.LBracket, "expect symbol offset");
        var symbol = Consume(BuiltinTokenTypes.Word, "expect symbol offset").Lexeme;
        if (AdvanceIfMatch(TokenTypes.Plus))
        {
            Consume(BuiltinTokenTypes.Integer, "expect integer offset");
            var offset = int.Parse(Previous().Lexeme);
            Consume(TokenTypes.LBracket, "expect enclosing ] in symbol offset");
            return new SymbolOffset(symbol, offset);
        }
        else if (AdvanceIfMatch(TokenTypes.Minus))
        {
            Consume(BuiltinTokenTypes.Integer, "expect integer offset");
            var offset = int.Parse(Previous().Lexeme);
            Consume(TokenTypes.LBracket, "expect enclosing ] in symbol offset");
            return new SymbolOffset(symbol, -offset);
        }
        else if (AdvanceIfMatch(TokenTypes.RBracket)) return new SymbolOffset(symbol, 0);
        else throw new ParsingException(Previous(), "expect symbol offset");
    }

    private X86Register ParseGeneralRegister32()
    {
        if (!AdvanceIfMatch(TokenTypes.GeneralRegister32))
            throw new ParsingException(Previous(), $"expect general register");
        if (!Enum.TryParse<X86Register>(Previous().Lexeme, true, out var register))
            throw new ParsingException(Previous(), $"unsupported general register {Previous().Lexeme}");
        return register;
    }

    private XmmRegister ParseXmmRegister()
    {
        if (!AdvanceIfMatch(TokenTypes.XmmRegister))
            throw new ParsingException(Previous(), $"expect xmm register");
        if (!Enum.TryParse<XmmRegister>(Previous().Lexeme, true, out var register))
            throw new ParsingException(Previous(), $"unsupported xmm register {Previous().Lexeme}");
        return register;
    }

    private X86ByteRegister ParseByteRegister()
    {
        if (!AdvanceIfMatch(TokenTypes.ByteRegister))
            throw new ParsingException(Previous(), $"expect xmm register");
        if (!Enum.TryParse<X86ByteRegister>(Previous().Lexeme, true, out var register))
            throw new ParsingException(Previous(), $"unsupported byte register {Previous().Lexeme}");
        return register;
    }

    public AssemblyInstruction Consume(AssemblyInstruction instruction)
    {
        Consume(TokenTypes.AssemblyInstruction, $"expect {instruction} instruction");
        if (!Enum.TryParse<AssemblyInstruction>(Previous().Lexeme, true, out var assemblyInstruction))
            throw new ParsingException(Previous(), $"unsupported assembly instruction {Previous().Lexeme}");
        if (assemblyInstruction != instruction)
            throw new ParsingException(Previous(), $"expected {instruction} instruction but got {assemblyInstruction}");
        return assemblyInstruction;
    }

    public int ParseImmediate()
    {
        Consume(BuiltinTokenTypes.Integer, "expect integer offset");
        return int.Parse(Previous().Lexeme);
    }

    public string ParseSymbol()
    {
        return Consume(BuiltinTokenTypes.Word, "expect symbol").Lexeme;
    }
}