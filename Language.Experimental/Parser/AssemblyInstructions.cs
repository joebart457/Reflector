﻿
using TokenizerCore.Model;

namespace Language.Experimental.Parser;

// The following is in the .Parser namespace because it is not actually used in assembly code generation
// this is just used by the parser to parse inline assembly instructions
public enum AssemblyInstruction
{
    Cdq,
    Push,
    Lea,
    Mov,
    Movsx,
    Sub,
    Add,
    And,
    Or,
    Xor,
    Pop,
    Neg,
    Not,
    Inc,
    Dec,
    IDiv,
    IMul,
    Jmp,
    JmpGt,
    JmpGte,
    JmpLt,
    JmpLte,
    JmpEq,
    JmpNeq,
    Jz,
    Jnz,
    Js,
    Jns,
    Ja,
    Jae,
    Jb,
    Jbe,
    Test,
    Cmp,
    Call,
    Label,
    Ret,
    Fstp,
    Fld,
    Movss,
    Comiss,
    Ucomiss,
    Addss,
    Subss,
    Mulss,
    Divss,
    Cvtsi2ss,
    Cvtss2si
}

public static class AssemblyInstructionTokenizerRuleGenerator
{
    public static List<TokenizerRule> AddAsssemblyInstructionParsingRules(this List<TokenizerRule> rules)
    {
        rules.AddRange(GetAsssemblyInstructionParsingRules);
        return rules;
    }
    public static List<TokenizerRule> GetAsssemblyInstructionParsingRules => Enum.GetNames<AssemblyInstruction>().Select(x => new TokenizerRule(TokenTypes.AssemblyInstruction, $"_{x.ToLower()}")).ToList();
}