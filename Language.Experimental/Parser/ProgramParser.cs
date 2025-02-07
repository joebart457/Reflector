using Language.Experimental.Compiler.Instructions;
using Language.Experimental.Constants;
using Language.Experimental.Expressions;
using Language.Experimental.Models;
using Language.Experimental.Statements;
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
    private Dictionary<string, StructTypeInfo> _userDefinedTypes = new();
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
    }

    public ProgramParser()
    {
        _tokenizer = Tokenizers.Default;
    }
    public List<StatementBase> ParseFile(string path, out List<ParsingException> errors)
    {
        return ParseText(File.ReadAllText(path), out errors);
    }

    public List<StatementBase> ParseText(string text, out List<ParsingException> errors)
    {
        var tokenizer = Tokenizers.Default;
        errors = new List<ParsingException>();
        _userDefinedTypes = new();
        var statements = new List<StatementBase>();

        var tokens = tokenizer.Tokenize(text, false)
            .Where(token => token.Type != BuiltinTokenTypes.EndOfFile)
            .ToList();

        Initialize(tokens);
        while (!AtEnd())
        {
            try
            {
                var next = ParseNext();
                if (next == null) break;
                else statements.Add(next);
            }
            catch (ParsingException e)
            {
                errors.Add(e);
                SeekToNextParsableUnit();
            }
        }
        return statements;
    }

    private void SeekToNextParsableUnit()
    {
        while (!AtEnd())
        {
            Advance();
            if (Match(TokenTypes.LParen)) break;
        }
    }

    private void SkipOverEnclosingParenthesis()
    {
        int lParenCount = 1;
        while (!AtEnd())
        {
            Advance();
            if (Match(TokenTypes.LParen)) lParenCount++;
            if (Match(TokenTypes.RParen))
            {
                lParenCount--;
                if (lParenCount == 0)
                {
                    Advance();
                    break;
                }
            }
        }
    }

    public void ParseUserTypes()
    {
        GatherTypeSignatures();
        ParseTypeDefinitions();
        SeekBeginning();
    }

    private StatementBase? ParseNext()
    {
        return ParseStatement();
    }

    private void GatherTypeSignatures()
    {
        SeekBeginning();
        while (!AtEnd())
        {
            if (AdvanceIfMatch(TokenTypes.LParen))
            {
                if (AdvanceIfMatch(TokenTypes.Type)) GatherTypeSignature();
                else SkipOverEnclosingParenthesis();
            }
            else throw new ParsingException(Current(), "expect top-level statement");
        }      
    }

    private void ParseTypeDefinitions()
    {
        SeekBeginning();
        while (!AtEnd())
        {
            if (AdvanceIfMatch(TokenTypes.LParen))
            {
                if (AdvanceIfMatch(TokenTypes.Type)) ParseTypeDefinition();
                else SkipOverEnclosingParenthesis();
            }
            else throw new ParsingException(Current(), "expect top-level statement");
        }
    }

    private void GatherTypeSignature()
    {
        var name = Consume(BuiltinTokenTypes.Word, "expect type name");
        if (_userDefinedTypes.ContainsKey(name.Lexeme))
            throw new ParsingException(name, $"redefinition of type {name.Lexeme}");
        SkipOverEnclosingParenthesis();
    }

    private void ParseTypeDefinition()
    {
        var name = Consume(BuiltinTokenTypes.Word, "expect type name");
        if (!_userDefinedTypes.TryGetValue(name.Lexeme, out var foundType))
            throw new ParsingException(name, $"type has not been defined properly {name.Lexeme}");
        
        do
        {
            Consume(TokenTypes.LParen, "expect field definition");
            Consume(TokenTypes.Field, "expect field definition. IE (field x int)");
            var fieldName = Consume(BuiltinTokenTypes.Word, "expect field name");
            var typeInfo = ParseTypeInfo();
            if (typeInfo.Is(IntrinsicType.Void) || typeInfo.IsStructType)
                throw new ParsingException(fieldName, $"invalid field type {typeInfo}");
            Consume(TokenTypes.RParen, "expect enclosing ) in field definition");
            foundType.Fields.Add(new StructFieldInfo(typeInfo, fieldName));
        } while (!AtEnd() && !Match(TokenTypes.RParen));
        Consume(TokenTypes.RParen, "expect enclosing ) in type definition");
        foundType.ValidateFields();
    }

    public TypeInfo ParseTypeInfo()
    {
        if (!AdvanceIfMatch(TokenTypes.IntrinsicType))
        {
            var typeName = Consume(BuiltinTokenTypes.Word, "expect type annotation");
            if (_userDefinedTypes.TryGetValue(typeName.Lexeme, out var userType)) return userType;
            else throw new ParsingException(Current(), "expect builtin or user type annotation");
        }
        if (!Enum.TryParse<IntrinsicType>(Previous().Lexeme, true, out var type))
            throw new ParsingException(Previous(), $"unsupported type annotation {Previous().Lexeme}");
        if (RequiresTypeArgument(type))
        {
            Consume(TokenTypes.LBracket, $"expect type argument for type {type}");
            if (SupportsMultipleTypeArguments(type))
            {
                List<TypeInfo> typeArguments = new();
                do
                {
                    var typeArgument = ParseTypeInfo();
                    typeArguments.Add(typeArgument);
                } while (AdvanceIfMatch(TokenTypes.Comma));
                Consume(TokenTypes.RBracket, "expect enclosing ] after type arguments");
                return new FunctionPtrTypeInfo(type, typeArguments);

            }else
            {
                var typeArgument = ParseTypeInfo();
                Consume(TokenTypes.RBracket, "expect enclosing ] after type argument");
                return new TypeInfo(type, typeArgument);
            }

        }
        return new TypeInfo(type, null);
    }

    private bool RequiresTypeArgument(IntrinsicType type)
    {
        return type == IntrinsicType.Ptr
            || type == IntrinsicType.StdCall_Function_Ptr
            || type == IntrinsicType.StdCall_Function_Ptr_Internal
            || type == IntrinsicType.StdCall_Function_Ptr_External
            || type == IntrinsicType.Cdecl_Function_Ptr
            || type == IntrinsicType.Cdecl_Function_Ptr_Internal
            || type == IntrinsicType.Cdecl_Function_Ptr_External;
    }
    private bool SupportsMultipleTypeArguments(IntrinsicType type)
    {
        return type == IntrinsicType.StdCall_Function_Ptr
            || type == IntrinsicType.StdCall_Function_Ptr_Internal
            || type == IntrinsicType.StdCall_Function_Ptr_External
            || type == IntrinsicType.Cdecl_Function_Ptr
            || type == IntrinsicType.Cdecl_Function_Ptr_Internal
            || type == IntrinsicType.Cdecl_Function_Ptr_External;
    }

    public StatementBase? ParseStatement()
    {
        if (AtEnd()) return null;
        Consume(TokenTypes.LParen, "expect all statements to begin with (");
        if (AdvanceIfMatch(TokenTypes.DefineFunction)) return ParseFunctionDefinition();
        if (AdvanceIfMatch(TokenTypes.Import)) return ParseImportedFunctionDefinition();
        if (AdvanceIfMatch(TokenTypes.Library)) return ParseImportLibraryDefinition();
        if (AdvanceIfMatch(TokenTypes.Type))
        {
            // Types have already been parsed so skip parsing them a second time
            SkipOverEnclosingParenthesis();
            return ParseStatement();
        }
        throw new ParsingException(Current(), $"unexpected token {Current()}");
    }

    public FunctionDefinition ParseFunctionDefinition(bool isLambda = false)
    {
        /*
         * (defn main:int (params (param argc int) (param argv ptr[string]))
         * 
         * 
         */
        IToken name;
        if (!isLambda) name = Consume(BuiltinTokenTypes.Word, "expect function name");
        else name = Previous();
        Consume(TokenTypes.Colon, "expect functionName:returnType");
        var returnType = ParseTypeInfo();
        Consume(TokenTypes.LParen, "expect parameter list");
        Consume(TokenTypes.Params, "expect paramter list. IE (params (param argc int) (param argv ptr<string>))");
        var parameters = new List<Parameter>();
        if (!AdvanceIfMatch(TokenTypes.RParen))
        {
            do
            {
                Consume(TokenTypes.LParen, "expect parameter definition");
                Consume(TokenTypes.Param, "expect parameter definition");
                var parameterName = Consume(BuiltinTokenTypes.Word, "expect parameter name");
                var parameterType = ParseTypeInfo();
                Consume(TokenTypes.RParen, "expect enclosing ) in parameter definition");
                parameters.Add(new Parameter(parameterName, parameterType));
            } while(!AtEnd() && !Match(TokenTypes.RParen));
            Consume(TokenTypes.RParen, "expect enclosing ) in parameter list");
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
         *          printf:void (params (param string s)))
         * 
         * 
         */
        var libraryAlias = Consume(BuiltinTokenTypes.Word, "expect import library alias");
        var callingConvention = ParseCallingConvention();
        var functionName = Consume(BuiltinTokenTypes.Word, "expect function name");
        Consume(TokenTypes.Colon, "expect functionName:returnType");
        var returnType = ParseTypeInfo();
        IToken importSymbol = functionName;
        if (Match(TokenTypes.LParen) && PeekMatch(1, TokenTypes.Symbol))
        {
            Advance();
            Advance();
            importSymbol = Consume(BuiltinTokenTypes.Word, "expect import symbol");
            Consume(TokenTypes.RParen, "expect enclosing ) in symbol annotation");
        }
        Consume(TokenTypes.LParen, "expect parameter list");
        Consume(TokenTypes.Params, "expect paramter list. IE (params (param argc int) (param argv ptr<string>))");
        var parameters = new List<Parameter>();
        if (!AdvanceIfMatch(TokenTypes.RParen))
        {
            do
            {
                Consume(TokenTypes.LParen, "expect parameter definition");
                Consume(TokenTypes.Param, "expect parameter definition");
                var parameterName = Consume(BuiltinTokenTypes.Word, "expect parameter name");
                var parameterType = ParseTypeInfo();
                Consume(TokenTypes.RParen, "expect enclosing ) in parameter definition");
                parameters.Add(new Parameter(parameterName, parameterType));
            } while (!AtEnd() && !Match(TokenTypes.RParen));
            Consume(TokenTypes.RParen, "expect enclosing ) in parameter list");
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
        if (AdvanceIfMatch(TokenTypes.LBracket)) return ParseCast();
        return ParseCall();
    }

    private ExpressionBase ParseCast()
    {
        var token = Previous();
        var typeInfo = ParseTypeInfo();
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

    private ExpressionBase ParseCompilerIntrinsicGet()
    {
        var token = Previous(); 
        Consume(TokenTypes.Colon, "expect _ci_get:returnType");
        var returnType = ParseTypeInfo();
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
        return new LambdaExpression(Previous(), ParseFunctionDefinition(true));
    }

    private ExpressionBase ParseSet()
    {
        var assignmentTarget = ParseExpression();
        var valueToAssign = ParseExpression();
        return new SetExpression(Previous(), assignmentTarget, valueToAssign);
    }

    private ExpressionBase ParseAssemblyInstruction()
    {
        foreach(var assemblyParsingRule in _assemblyParsingRules)
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

    private bool TryParseGeneralRegister32(X86Register register)
    {
        if (AdvanceIfMatch(TokenTypes.GeneralRegister32))
        {
            if (!Enum.TryParse<X86Register>(Previous().Lexeme, true, out register))
                throw new ParsingException(Previous(), $"unsupported general register {Previous().Lexeme}");
            return true;
        }
        return false;
    }

    private bool TryParseXmmRegister(XmmRegister register)
    {
        if (AdvanceIfMatch(TokenTypes.GeneralRegister32))
        {
            if (!Enum.TryParse<XmmRegister>(Previous().Lexeme, true, out register))
                throw new ParsingException(Previous(), $"unsupported xmm register {Previous().Lexeme}");
            return true;
        }
        return false;
    }

    private ExpressionBase ParseGet()
    {

        var expr = ParsePrimary();
        if (expr is IdentifierExpression identifierExpression)
        {
            while (Match(TokenTypes.Dot) || Match(TokenTypes.NullDot))
            {
                if (AdvanceIfMatch(TokenTypes.Dot))
                {
                    var targetField = Consume(BuiltinTokenTypes.Word, "expect member name after '.'");
                    expr = new GetExpression(Previous(), expr, targetField, false);
                }
                else
                {
                    Advance();
                    var targetField = Consume(BuiltinTokenTypes.Word, "expect member name after '?.'");
                    expr = new GetExpression(Previous(), expr, targetField, true);
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
        throw new ParsingException(Current(), $"encountered unexpected token {Current()}");
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