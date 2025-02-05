﻿using Language.Experimental.Compiler.Instructions;

namespace Language.Experimental.Compiler.Optimizer;

internal class X86AssemblyOptimizer
{
    private class RegisterOffsetOrImmediate
    {
        private int? _immediateValue;
        public int ImmediateValue => _immediateValue ?? throw new ArgumentNullException(nameof(ImmediateValue));
        private RegisterOffset? _registerOffset;
        public RegisterOffset RegisterOffset => _registerOffset ?? throw new ArgumentNullException(nameof(RegisterOffset));
        public RegisterOffsetOrImmediate(int? immediateValue, RegisterOffset? registerOffset)
        {
            _immediateValue = immediateValue;
            _registerOffset = registerOffset;
        }

        public bool IsImmediate => _immediateValue != null;
        public bool IsRegisterOffset => _registerOffset != null;

        public override bool Equals(object? obj)
        {
            if (obj is RegisterOffsetOrImmediate offsetOrImmediate)
            {
                if (IsImmediate && offsetOrImmediate.IsImmediate) return ImmediateValue == offsetOrImmediate.ImmediateValue;
                if (IsRegisterOffset && offsetOrImmediate.IsRegisterOffset) return RegisterOffset!.Equals(offsetOrImmediate.RegisterOffset);
                return false;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return 1;
        }

        public static RegisterOffsetOrImmediate Create(int immediate) => new RegisterOffsetOrImmediate(immediate, null);
        public static RegisterOffsetOrImmediate Create(RegisterOffset registerOffset) => new RegisterOffsetOrImmediate(null, registerOffset);
    }

    private class MemoryLocationOrConstantValue
    {
        // represents equivalency to a memory offset or a known immediate value
        private IOffset? _memoryLocation;
        private object? _immediateValue;
        public IOffset MemoryLocation => _memoryLocation ?? throw new NullReferenceException(nameof(MemoryLocation));
        public object ImmediateValue => _immediateValue ?? throw new NullReferenceException(nameof(ImmediateValue));


        public bool IsImmediateValue => _immediateValue != null;
        public bool IsMemoryOffset => _memoryLocation != null;
        public bool IsRegisterOffset => _memoryLocation is RegisterOffset;
        public RegisterOffset RegisterOffset => _memoryLocation as RegisterOffset ?? throw new NullReferenceException(nameof(RegisterOffset));
        public MemoryLocationOrConstantValue(IOffset memoryLocation)
        {
            _memoryLocation = memoryLocation;
            _immediateValue = null;
        }
        public MemoryLocationOrConstantValue(object value)
        {
            _immediateValue = value;
            _memoryLocation = null;
        }

        public bool TryGetValue<Ty>(out Ty? value)
        {
            value = default;
            if (!IsImmediateValue) return false;
            if (ImmediateValue is Ty tyVal)
            {
                value = tyVal;
                return true;
            }
            return false;
        }

        public static MemoryLocationOrConstantValue Create(IOffset offset) => new MemoryLocationOrConstantValue(offset);
        public static MemoryLocationOrConstantValue Create(int value) => new MemoryLocationOrConstantValue(value);
        public static MemoryLocationOrConstantValue Create(float value) => new MemoryLocationOrConstantValue(value);
        public static MemoryLocationOrConstantValue Create(byte value) => new MemoryLocationOrConstantValue(value);

    }

    private Dictionary<X86Register, MemoryLocationOrConstantValue> _registerValues = new();
    private Dictionary<XmmRegister, MemoryLocationOrConstantValue> _xmmRegisterValues = new();
    private Dictionary<IOffset, MemoryLocationOrConstantValue> _memoryMap = new();
    private Stack<MemoryLocationOrConstantValue> _fpuStack = new();
    private RegisterOffset TopOfStack => Offset.Create(X86Register.esp, 0);

    public CompilationResult Optimize(CompilationResult compilationResult)
    {
        for (int i = 0; i < compilationResult.CompilationOptions.OptimizationPasses; i++)
        {
            MakeOpimizationPass(compilationResult);
        }
        return compilationResult;
    }

    private void WipeRegister(X86Register register)
    {
        _registerValues.Remove(register);
    }

    private void WipeRegister(XmmRegister register)
    {
        _xmmRegisterValues.Remove(register);
    }

    private void InvalidateMemory(IOffset offset)
    {
        if (offset is SymbolOffset_Byte symbolOffset_Byte)
        {
            // still need to invalidate memory for byte offsets since it is part of larger chunk of tracked memory
            offset = Offset.CreateSymbolOffset(symbolOffset_Byte.Symbol, symbolOffset_Byte.Offset);
        }
        _memoryMap.Remove(offset);
        foreach (var key in _memoryMap.Keys)
        {
            if (_memoryMap[key].IsMemoryOffset && _memoryMap[key].MemoryLocation.Equals(offset))
            {
                _memoryMap.Remove(key);
            }
        }
        foreach (var key in _registerValues.Keys)
        {
            if (_registerValues[key].IsMemoryOffset && _registerValues[key].Equals(offset))
                _registerValues.Remove(key);
        }
        foreach (var key in _xmmRegisterValues.Keys)
        {
            if (_xmmRegisterValues[key].IsMemoryOffset && _xmmRegisterValues[key].Equals(offset))
                _xmmRegisterValues.Remove(key);
        }
    }




    private void PushStack(MemoryLocationOrConstantValue? memoryLocationOrConstantValue)
    {
        foreach (var key in _registerValues.Keys)
        {
            if (_registerValues[key].IsRegisterOffset && _registerValues[key].RegisterOffset.Register == X86Register.esp)
            {
                _registerValues[key] = MemoryLocationOrConstantValue.Create(Offset.Create(X86Register.esp, _registerValues[key].RegisterOffset.Offset + 4));
            }
        }
        foreach (var key in _memoryMap.Keys)
        {
            if (_memoryMap[key].IsRegisterOffset && _memoryMap[key].RegisterOffset.Register == X86Register.esp)
            {
                _memoryMap[key] = MemoryLocationOrConstantValue.Create(Offset.Create(X86Register.esp, _memoryMap[key].RegisterOffset.Offset + 4));
            }
        }
        if (memoryLocationOrConstantValue != null) _memoryMap[TopOfStack] = memoryLocationOrConstantValue;
        else _memoryMap.Remove(TopOfStack);
    }

    private void PopStack(X86Register register)
    {
        WipeRegister(register);
        _memoryMap.TryGetValue(TopOfStack, out var value);
        AdjustMemory(X86Register.esp, -4);
        if (value != null)
            SetRegister(register, value);
    }

    private void AdjustMemory(X86Register register, int offset)
    {
        if (register == X86Register.esp && offset < 0)
        {
            WipeRegister(X86Register.esp);
        }
        else
        {
            foreach (var key in _registerValues.Keys)
            {
                if (_registerValues[key].IsRegisterOffset && _registerValues[key].RegisterOffset.Register == register)
                {
                    _registerValues[key] = MemoryLocationOrConstantValue.Create(Offset.Create(register, _registerValues[key].RegisterOffset.Offset + offset));
                }
            }
            foreach (var key in _memoryMap.Keys)
            {
                if (_memoryMap[key].IsRegisterOffset && _memoryMap[key].RegisterOffset.Register == register)
                {
                    _memoryMap[key] = MemoryLocationOrConstantValue.Create(Offset.Create(register, _memoryMap[key].RegisterOffset.Offset + offset));
                }
            }
        }
    }

    private void SetMemory(IOffset offset, MemoryLocationOrConstantValue memoryLocationOrConstantValue)
    {
        _memoryMap[offset] = memoryLocationOrConstantValue;
    }

    private void SetRegister(X86Register register, MemoryLocationOrConstantValue registerOffsetOrImmediate)
    {
        _registerValues[register] = registerOffsetOrImmediate;
    }

    private void SetRegister(XmmRegister register, MemoryLocationOrConstantValue registerOffsetOrImmediate)
    {
        _xmmRegisterValues[register] = registerOffsetOrImmediate;
    }

    private void AddRegister(X86Register register, int valueToAdd)
    {
        if (_registerValues.TryGetValue(register, out var value) && value.TryGetValue(out int immediateValue))
        {
            SetRegister(register, MemoryLocationOrConstantValue.Create(immediateValue + valueToAdd));
        }
        else _registerValues.Remove(register);
        AdjustMemory(register, -valueToAdd);
    }

    private void SubtractRegister(X86Register register, int valueToSubtract)
    {
        if (_registerValues.TryGetValue(register, out var value) && value.TryGetValue(out int immediateValue))
        {
            SetRegister(register, MemoryLocationOrConstantValue.Create(immediateValue - valueToSubtract));
        }
        else _registerValues.Remove(register);
        AdjustMemory(register, valueToSubtract);
    }

    private X86Instruction TrackInstruction(X86Instruction instruction)
    {
        if (instruction is Cdq cdq)
        {
            WipeRegister(X86Register.eax);
            WipeRegister(X86Register.edx);
        }
        if (instruction is Push_Register push_Register)
        {
            if (_registerValues.TryGetValue(push_Register.Register, out var registerValue))
            {
                PushStack(registerValue);
            }
            else PushStack(null);
        }
        if (instruction is Push_Offset push_Offset)
        {
            PushStack(MemoryLocationOrConstantValue.Create(push_Offset.Offset));
        }
        if (instruction is Push_Address push_Address)
        {
            PushStack(null);
        }
        if (instruction is Push_Immediate<int> push_Immediate)
        {
            PushStack(MemoryLocationOrConstantValue.Create(push_Immediate.Immediate));
        }
        if (instruction is Lea_Register_Offset lea_Register_Offset)
        {
            WipeRegister(lea_Register_Offset.Destination);
        }
        if (instruction is Mov_Register_Offset mov_Register_Offset)
        {
            WipeRegister(mov_Register_Offset.Destination);
            _registerValues[mov_Register_Offset.Destination] = MemoryLocationOrConstantValue.Create(mov_Register_Offset.Source);
        }
        if (instruction is Mov_Offset_Register mov_Offset_Register)
        {
            InvalidateMemory(mov_Offset_Register.Destination);
            if (_registerValues.TryGetValue(mov_Offset_Register.Source, out var registerValue))
            {
                SetMemory(mov_Offset_Register.Destination, registerValue);
            }
        }
        if (instruction is Mov_Offset_Immediate mov_Offset_Immediate)
        {
            InvalidateMemory(mov_Offset_Immediate.Destination);
            SetMemory(mov_Offset_Immediate.Destination, MemoryLocationOrConstantValue.Create(mov_Offset_Immediate.Immediate));
        }
        if (instruction is Mov_Register_Register mov_Register_Register)
        {
            WipeRegister(mov_Register_Register.Destination);
            if (_registerValues.TryGetValue(mov_Register_Register.Source, out var registerValue))
            {
                SetRegister(mov_Register_Register.Destination, registerValue);
            }
        }
        if (instruction is Mov_Register_Immediate mov_Register_Immediate)
        {
            WipeRegister(mov_Register_Immediate.Destination);
            SetRegister(mov_Register_Immediate.Destination, MemoryLocationOrConstantValue.Create(mov_Register_Immediate.ImmediateValue));
        }
        if (instruction is Mov_Offset_Register__Byte mov_Offset_Register__Byte)
        {
            InvalidateMemory(mov_Offset_Register__Byte.Destination);
        }
        if (instruction is Movsx_Register_Offset movsx_Register_Offset)
        {
            WipeRegister(movsx_Register_Offset.Destination);
        }
        if (instruction is Sub_Register_Immediate sub_Register_Immediate)
        {
            SubtractRegister(sub_Register_Immediate.Destination, sub_Register_Immediate.ValueToSubtract);
        }
        if (instruction is Sub_Register_Register sub_Register_Register)
        {
            _registerValues.TryGetValue(sub_Register_Register.Source, out var sourceValue);
            if (sourceValue?.TryGetValue(out int value) == true)
            {
                SubtractRegister(sub_Register_Register.Destination, value);
            }
            else
            {
                WipeRegister(sub_Register_Register.Destination);
            }
        }
        if (instruction is Add_Register_Immediate add_Register_Immediate)
        {
            AddRegister(add_Register_Immediate.Destination, add_Register_Immediate.ValueToAdd);
        }
        if (instruction is Add_Register_Register add_Register_Register)
        {
            _registerValues.TryGetValue(add_Register_Register.Source, out var sourceValue);
            if (sourceValue?.TryGetValue(out int immediateValue) == true)
            {
                AddRegister(add_Register_Register.Destination, immediateValue);
            }
            else
            {
                WipeRegister(add_Register_Register.Destination);
            }
        }
        if (instruction is And_Register_Register and_Register_Register)
        {
            WipeRegister(and_Register_Register.Destination);
        }
        if (instruction is Or_Register_Register or_Register_Register)
        {
            WipeRegister(or_Register_Register.Destination);
        }
        if (instruction is Xor_Register_Register xor_Register_Register)
        {
            WipeRegister(xor_Register_Register.Destination);
            if (xor_Register_Register.Destination == xor_Register_Register.Source)
                SetRegister(xor_Register_Register.Destination, MemoryLocationOrConstantValue.Create(0));
        }
        if (instruction is Pop_Register pop_Register)
        {
            PopStack(pop_Register.Destination);
        }
        if (instruction is Neg_Offset neg_Offset)
        {
            if (_memoryMap.TryGetValue(neg_Offset.Operand, out var value) && value.TryGetValue(out int immediateValue))
            {
                InvalidateMemory(neg_Offset.Operand);
                SetMemory(neg_Offset.Operand, MemoryLocationOrConstantValue.Create(-immediateValue));
            }
            else if (_memoryMap.TryGetValue(neg_Offset.Operand, out var memoryValue) && memoryValue.TryGetValue(out float immediateFloatValue))
            {
                InvalidateMemory(neg_Offset.Operand);
                SetMemory(neg_Offset.Operand, MemoryLocationOrConstantValue.Create(-immediateFloatValue));
            }
            else InvalidateMemory(neg_Offset.Operand);

        }
        if (instruction is Not_Offset not_Offset)
        {
            InvalidateMemory(not_Offset.Operand);
        }
        if (instruction is IDiv_Offset idiv_Offset)
        {
            WipeRegister(X86Register.eax);
            WipeRegister(X86Register.edx);
        }
        if (instruction is IMul_Register_Register imul_Register_Register)
        {
            _registerValues.TryGetValue(imul_Register_Register.Destination, out var destinationValue);
            _registerValues.TryGetValue(imul_Register_Register.Source, out var sourceValue);
            WipeRegister(imul_Register_Register.Destination);
            if (sourceValue?.TryGetValue(out int immediateSourceValue) == true)
            {
                if (destinationValue?.TryGetValue(out int immediateDestinationValue) == true)
                    SetRegister(imul_Register_Register.Destination, MemoryLocationOrConstantValue.Create(immediateDestinationValue * immediateSourceValue));
            }
        }
        if (instruction is IMul_Register_Immediate imul_Register_Immediate)
        {
            WipeRegister(imul_Register_Immediate.Destination);
            if (_registerValues.TryGetValue(imul_Register_Immediate.Destination, out var value) && value.TryGetValue(out int immediateValue))
            {
                SetRegister(imul_Register_Immediate.Destination, MemoryLocationOrConstantValue.Create(immediateValue * imul_Register_Immediate.Immediate));
            }
        }
        if (instruction is Add_Register_Offset add_Register_Offset)
        {
            if (_registerValues.TryGetValue(add_Register_Offset.Destination, out var value) && value.TryGetValue(out int immediateValue))
            {
                AddRegister(add_Register_Offset.Destination, immediateValue);
            }
            else WipeRegister(add_Register_Offset.Destination);
        }
        if (instruction is Jmp jmp)
        {
            _memoryMap.Clear();
            _registerValues.Clear();
            _xmmRegisterValues.Clear();
            _fpuStack.Clear();
        }
        if (instruction is Test_Register_Register test_Register_Register)
        {

        }
        if (instruction is Test_Register_Offset test_Register_Offset)
        {

        }
        if (instruction is Cmp_Register_Register cmp_Register_Register)
        {

        }
        if (instruction is Cmp_Register_Immediate cmp_Register_Immediate)
        {

        }
        if (instruction is Cmp_Byte_Byte cmp_Byte_Byte)
        {

        }
        if (instruction is Call call)
        {
            WipeRegister(X86Register.eax);
            WipeRegister(X86Register.ebx);
            WipeRegister(X86Register.ecx);
            WipeRegister(X86Register.edx);
            WipeRegister(XmmRegister.xmm0);
            WipeRegister(XmmRegister.xmm1);
            _fpuStack.Clear();
        }
        if (instruction is Label label)
        {
            _memoryMap.Clear();
            _registerValues.Clear();
            _xmmRegisterValues.Clear();
        }
        if (instruction is Ret ret)
        {
            _memoryMap.Clear();
            _registerValues.Clear();
            _xmmRegisterValues.Clear();
        }
        if (instruction is Ret_Immediate ret_Immediate)
        {
            _memoryMap.Clear();
            _registerValues.Clear();
            _xmmRegisterValues.Clear();
        }
        if (instruction is Fstp_Offset fstp_Offset)
        {
            InvalidateMemory(fstp_Offset.Destination);
            if (_fpuStack.TryPop(out var trackedValue))
            {
                SetMemory(fstp_Offset.Destination, trackedValue);
            }
        }
        if (instruction is Fld_Offset fld_Offset)
        {
            _fpuStack.Push(MemoryLocationOrConstantValue.Create(fld_Offset.Source));
        }
        if (instruction is Movss_Offset_Register movss_Offset_Register)
        {
            if (_xmmRegisterValues.TryGetValue(movss_Offset_Register.Source, out var trackedValue))
            {
                InvalidateMemory(movss_Offset_Register.Destination);
                SetMemory(movss_Offset_Register.Destination, trackedValue);
            }
            else InvalidateMemory(movss_Offset_Register.Destination);
        }
        if (instruction is Movss_Register_Offset movss_Register_Offset)
        {
            WipeRegister(movss_Register_Offset.Destination);
            if (_memoryMap.TryGetValue(movss_Register_Offset.Source, out var trackedValue))
            {
                SetRegister(movss_Register_Offset.Destination, trackedValue);
            }
        }
        if (instruction is Movss_Register_Register movss_Register_Register)
        {
            WipeRegister(movss_Register_Register.Destination);
            if (_xmmRegisterValues.TryGetValue(movss_Register_Register.Source, out var trackedValue))
            {
                SetRegister(movss_Register_Register.Destination, trackedValue);
            }
        }
        if (instruction is Comiss_Register_Offset comiss_Register_Offset)
        {

        }
        if (instruction is Comiss_Register_Register comiss_Register_Register)
        {

        }
        if (instruction is Ucomiss_Register_Register ucomiss_Register_Register)
        {

        }
        if (instruction is Addss_Register_Offset addss_Register_Offset)
        {
            if (_xmmRegisterValues.TryGetValue(addss_Register_Offset.Destination, out var destinationValue) && destinationValue.TryGetValue(out float destinationFloatValue)
                && _memoryMap.TryGetValue(addss_Register_Offset.Source, out var sourceValue) && sourceValue.TryGetValue(out float sourceFloatValue))
            {
                WipeRegister(addss_Register_Offset.Destination);
                SetRegister(addss_Register_Offset.Destination, MemoryLocationOrConstantValue.Create(destinationFloatValue + sourceFloatValue));
            }
            else WipeRegister(addss_Register_Offset.Destination);
        }
        if (instruction is Subss_Register_Offset subss_Register_Offset)
        {
            if (_xmmRegisterValues.TryGetValue(subss_Register_Offset.Destination, out var destinationValue) && destinationValue.TryGetValue(out float destinationFloatValue)
                && _memoryMap.TryGetValue(subss_Register_Offset.Source, out var sourceValue) && sourceValue.TryGetValue(out float sourceFloatValue))
            {
                WipeRegister(subss_Register_Offset.Destination);
                SetRegister(subss_Register_Offset.Destination, MemoryLocationOrConstantValue.Create(destinationFloatValue - sourceFloatValue));
            }
            else WipeRegister(subss_Register_Offset.Destination);
        }
        if (instruction is Mulss_Register_Offset mulss_Register_Offset)
        {
            if (_xmmRegisterValues.TryGetValue(mulss_Register_Offset.Destination, out var destinationValue) && destinationValue.TryGetValue(out float destinationFloatValue)
                && _memoryMap.TryGetValue(mulss_Register_Offset.Source, out var sourceValue) && sourceValue.TryGetValue(out float sourceFloatValue))
            {
                WipeRegister(mulss_Register_Offset.Destination);
                SetRegister(mulss_Register_Offset.Destination, MemoryLocationOrConstantValue.Create(destinationFloatValue * sourceFloatValue));
            }
            else WipeRegister(mulss_Register_Offset.Destination);
        }
        if (instruction is Divss_Register_Offset divss_Register_Offset)
        {
            if (_xmmRegisterValues.TryGetValue(divss_Register_Offset.Destination, out var destinationValue) && destinationValue.TryGetValue(out float destinationFloatValue)
                && _memoryMap.TryGetValue(divss_Register_Offset.Source, out var sourceValue) && sourceValue.TryGetValue(out float sourceFloatValue) && sourceFloatValue != 0)
            {
                WipeRegister(divss_Register_Offset.Destination);
                SetRegister(divss_Register_Offset.Destination, MemoryLocationOrConstantValue.Create(destinationFloatValue / sourceFloatValue));
            }
            else WipeRegister(divss_Register_Offset.Destination);
        }
        if (instruction is Cvtsi2ss_Register_Offset cvtsi2Ss_Register_Offset)
        {
            WipeRegister(cvtsi2Ss_Register_Offset.Destination);
        }
        if (instruction is Cvtss2si_Register_Offset cvtss2Si_Register_Offset)
        {
            WipeRegister(cvtss2Si_Register_Offset.Destination);
        }
        if (instruction is Push_SymbolOffset push_SymbolOffset)
        {
            if (_memoryMap.TryGetValue(push_SymbolOffset.Offset, out var value))
                PushStack(value);
            else PushStack(null);
        }
        if (instruction is Lea_Register_SymbolOffset lea_Register_SymbolOffset)
        {
            WipeRegister(lea_Register_SymbolOffset.Destination);
        }
        if (instruction is Mov_SymbolOffset_Register mov_SymbolOffset_Register)
        {
            if (_registerValues.TryGetValue(mov_SymbolOffset_Register.Source, out var value))
                SetMemory(mov_SymbolOffset_Register.Destination, value);
        }
        if (instruction is Mov_SymbolOffset_Register__Byte mov_SymbolOffset_Register__Byte)
        {
            // Do not track byte values but need to invalidate the memory they are moved to
            InvalidateMemory(mov_SymbolOffset_Register__Byte.Destination);
        }
        if (instruction is Movsx_Register_SymbolOffset__Byte movsx_Register_SymbolOffset)
        {
            WipeRegister(movsx_Register_SymbolOffset.Destination);
        }
        if (instruction is Mov_SymbolOffset_Immediate mov_SymbolOffset_Immediate)
        {
            SetMemory(mov_SymbolOffset_Immediate.Destination, MemoryLocationOrConstantValue.Create(mov_SymbolOffset_Immediate.ImmediateValue));
        }
        if (instruction is Inc_Register inc_Register)
        {
            AddRegister(inc_Register.Destination, 1);
        }
        if (instruction is Dec_Register dec_Register)
        {
            SubtractRegister(dec_Register.Destination, 1);
        }
        if (instruction is Inc_Offset inc_Offset)
        {
            _memoryMap.TryGetValue(inc_Offset.Destination, out var value);
            InvalidateMemory(inc_Offset.Destination);
            if (value != null && value.TryGetValue(out int immediateValue)) SetMemory(inc_Offset.Destination, MemoryLocationOrConstantValue.Create(immediateValue + 1));

        }
        if (instruction is Dec_Offset dec_Offset)
        {
            _memoryMap.TryGetValue(dec_Offset.Destination, out var value);
            InvalidateMemory(dec_Offset.Destination);
            if (value != null && value.TryGetValue(out int immediateValue)) SetMemory(dec_Offset.Destination, MemoryLocationOrConstantValue.Create(immediateValue - 1));
        }
        if (instruction is Mov_SymbolOffset_Byte_Register__Byte mov_SymbolOffset_Byte_Register__Byte)
        {
            InvalidateMemory(mov_SymbolOffset_Byte_Register__Byte.Destination);
        }
        if (instruction is Mov_RegisterOffset_Byte_Register__Byte mov_RegisterOffset_Byte_Register__Byte)
        {
            InvalidateMemory(mov_RegisterOffset_Byte_Register__Byte.Destination);
        }
        return instruction;
    }

    private bool TryGetRegister(IOffset offset, out X86Register result)
    {
        // Tests if any register already has the corresponding offset value stored in it
        result = X86Register.eax;
        foreach (var key in _registerValues.Keys)
        {
            if (_registerValues[key].IsMemoryOffset && _registerValues[key].MemoryLocation.Equals(offset))
            {
                result = key;
                return true;
            }
        }
        return false;
    }

    private bool TryGetXmmRegister(IOffset offset, out XmmRegister result)
    {
        // Tests if any register already has the corresponding offset value stored in it
        result = XmmRegister.xmm0;
        foreach (var key in _xmmRegisterValues.Keys)
        {
            if (_xmmRegisterValues[key].IsMemoryOffset && _xmmRegisterValues[key].MemoryLocation.Equals(offset))
            {
                result = key;
                return true;
            }
        }
        return false;
    }


    private bool TryGetImmediate(IOffset offset, out int result)
    {
        result = 0;
        if (_memoryMap.TryGetValue(offset, out var value) && value.TryGetValue(out int immediateValue))
        {
            result = immediateValue;
            return true;
        }
        return false;
    }

    private bool TryGetImmediate(X86Register register, out int result)
    {
        result = 0;
        if (_registerValues.TryGetValue(register, out var value))
        {
            if (value.TryGetValue(out int immediateValue))
            {
                result = immediateValue;
                return true;
            }
            return TryGetImmediate(value.MemoryLocation, out result);
        }
        return false;
    }

    private bool TryGetImmediateFloat(IOffset offset, out float result)
    {
        result = 0;
        if (_memoryMap.TryGetValue(offset, out var value) && value.TryGetValue(out float immediateValue))
        {
            result = immediateValue;
            return true;
        }
        return false;
    }

    private bool TryGetImmediateFloat(XmmRegister register, out float result)
    {
        result = 0;
        if (_xmmRegisterValues.TryGetValue(register, out var value))
        {
            if (value.TryGetValue(out float immediateValue))
            {
                result = immediateValue;
                return true;
            }
            return TryGetImmediateFloat(value.MemoryLocation, out result);
        }
        return false;
    }

    private bool IsEquivalent(X86Register register1, X86Register register2)
    {
        if (register1 == register2) return true;
        _registerValues.TryGetValue(register1, out var reg1Value);
        _registerValues.TryGetValue(register2, out var reg2Value);
        return reg1Value?.Equals(reg2Value) == true;
    }

    private bool IsEquivalent(IOffset offset1, IOffset offset2)
    {
        if (offset1.Equals(offset2)) return true;
        _memoryMap.TryGetValue(offset1, out var offset1Value);
        _memoryMap.TryGetValue(offset2, out var offset2Value);
        if (offset1Value == null && offset2Value != null) return offset2Value.IsMemoryOffset && offset2Value.MemoryLocation.Equals(offset1);
        return offset1Value?.Equals(offset2Value) == true;
    }

    private bool IsEquivalent(X86Register register, IOffset offset)
    {
        _registerValues.TryGetValue(register, out var regValue);
        _memoryMap.TryGetValue(offset, out var offsetValue);
        if (regValue == null) return false;
        if (regValue.IsMemoryOffset && regValue.MemoryLocation.Equals(offset)) return true;
        return regValue.Equals(offsetValue);
    }

    private bool IsEquivalent(IOffset offset, X86Register register)
    {
        _registerValues.TryGetValue(register, out var regValue);
        _memoryMap.TryGetValue(offset, out var offsetValue);
        if (regValue == null) return false;
        if (regValue.IsMemoryOffset && regValue.MemoryLocation.Equals(offset)) return true;
        return regValue.Equals(offsetValue);
    }

    private bool IsEquivalent(XmmRegister register1, XmmRegister register2)
    {
        if (register1 == register2) return true;
        _xmmRegisterValues.TryGetValue(register1, out var reg1Value);
        _xmmRegisterValues.TryGetValue(register2, out var reg2Value);
        return reg1Value?.Equals(reg2Value) == true;
    }

    private bool IsEquivalent(XmmRegister register, IOffset offset)
    {
        _xmmRegisterValues.TryGetValue(register, out var regValue);
        _memoryMap.TryGetValue(offset, out var offsetValue);
        if (regValue == null) return false;
        if (regValue.IsMemoryOffset && regValue.MemoryLocation.Equals(offset)) return true;
        return regValue.Equals(offsetValue);
    }

    private bool IsEquivalent(IOffset offset, XmmRegister register)
    {
        _xmmRegisterValues.TryGetValue(register, out var regValue);
        _memoryMap.TryGetValue(offset, out var offsetValue);
        if (regValue == null) return false;
        if (regValue.IsMemoryOffset && regValue.MemoryLocation.Equals(offset)) return true;
        return regValue.Equals(offsetValue);
    }

    private bool IsEquivalent(IOffset offset, int immediateValue)
    {
        _memoryMap.TryGetValue(offset, out var offsetValue);
        if (offsetValue == null) return false;
        return offsetValue.TryGetValue(out int trackedValue) && trackedValue == immediateValue;
    }

    private bool IsEquivalent(X86Register register, int immediateValue)
    {
        _registerValues.TryGetValue(register, out var regValue);
        if (regValue == null) return false;
        return regValue.TryGetValue(out int registerValue) && registerValue == immediateValue;
    }

    private X86Instruction? Peek(List<X86Instruction> instructions, int index)
    {
        if (index < instructions.Count) return instructions[index];
        return null;
    }

    private CompilationResult MakeOpimizationPass(CompilationResult compilationResult)
    {
        foreach (var fn in compilationResult.FunctionData)
        {
            _registerValues.Clear();
            _memoryMap.Clear();

            var optimizedInstructions = new List<X86Instruction>();

            for (int i = 0; i < fn.Instructions.Count; i++)
            {
                var instruction = fn.Instructions[i];

                if (instruction is Cdq cdq)
                {

                }
                if (instruction is Push_Register push_Register)
                {
                    var espResetIndex = fn.Instructions.Skip(i).ToList().FindIndex(x => x is Add_Register_Immediate add_Register_Immediate1 && add_Register_Immediate1.Destination == X86Register.esp && add_Register_Immediate1.ValueToAdd == 4);
                    if (espResetIndex != -1 && !IsEspReferenced(fn.Instructions.Slice(i + 1, espResetIndex), 0))
                    {
                        fn.Instructions.Slice(i + 1, espResetIndex).ForEach(x => optimizedInstructions.Add(TrackInstruction(x)));
                        i = i + espResetIndex + 1;
                        continue;
                    }

                    if (Peek(fn.Instructions, i + 1) is Pop_Register pop_register && push_Register.Register == pop_register.Destination)
                    {
                        // test for
                        // push eax
                        // pop eax
                        // optimization:
                        // ...            (no instructions necessary)
                        i++;
                        continue;
                    }

                    if (TryGetImmediate(push_Register.Register, out var immediate))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Push(immediate)));
                        continue;
                    }
                    if (Peek(fn.Instructions, i + 1) is Add_Register_Immediate add_Register_Immediate1 && add_Register_Immediate1.Destination == X86Register.esp && add_Register_Immediate1.ValueToAdd == 4)
                    {
                        i++;
                        continue;
                    }

                    if (Peek(fn.Instructions, i + 1) is Mov_Register_Offset mov_Register_Offset1 && mov_Register_Offset1.Source.Equals(TopOfStack))
                    {
                        // test for
                        // push eax
                        // mov ebx, [esp]
                        // optimization:
                        // push eax
                        // mov ebx, eax
                        optimizedInstructions.Add(TrackInstruction(instruction));
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Mov(mov_Register_Offset1.Destination, push_Register.Register)));
                        i++;
                        continue;
                    }

                    if (Peek(fn.Instructions, i + 1) is Pop_Register pop_Register1)
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Mov(pop_Register1.Destination, push_Register.Register)));
                        i++;
                        continue;
                    }

                    if (Peek(fn.Instructions, i + 2) is Add_Register_Immediate add_Register_Immediate2 && add_Register_Immediate2.Destination == X86Register.esp && add_Register_Immediate2.ValueToAdd == 4)
                    {
                        var nextInstruction = Peek(fn.Instructions, i + 1);
                        if (nextInstruction != null
                            && !(nextInstruction is Call || nextInstruction is Label || nextInstruction is Jmp)
                            && !IsEspReferenced([nextInstruction], 0))
                        {
                            optimizedInstructions.Add(TrackInstruction(nextInstruction));
                            i = i + 2;
                            continue;
                        }
                    }
                    if (Peek(fn.Instructions, i + 2) is Pop_Register pop_Register2)
                    {
                        var nextInstruction = Peek(fn.Instructions, i + 1);
                        if (nextInstruction != null
                            && !(nextInstruction is Call || nextInstruction is Label || nextInstruction is Jmp)
                            && !IsEspReferenced([nextInstruction], 0)
                            && !IsReferenced([nextInstruction], 0, push_Register.Register))
                        {

                            // Test for
                            // push ebx
                            // mov eax, [ebp + 12]
                            // pop ebx
                            // optimization:
                            // mov eax, [ebp + 12]
                            if (push_Register.Register == pop_Register2.Destination) optimizedInstructions.Add(TrackInstruction(nextInstruction));
                            else
                            {
                                // Test for
                                // push ebx
                                // mov eax, [ebp + 12]
                                // pop edx
                                // optimization:
                                // mov eax, [ebp + 12]
                                // mov edx, ebx
                                optimizedInstructions.Add(TrackInstruction(nextInstruction));
                                optimizedInstructions.Add(TrackInstruction(X86Instructions.Mov(pop_Register2.Destination, push_Register.Register)));
                            }
                            i = i + 2;
                            continue;
                        }
                    }
                }
                if (instruction is Push_Offset push_Offset)
                {
                    var espResetIndex = fn.Instructions.Skip(i).ToList().FindIndex(x => x is Add_Register_Immediate add_Register_Immediate1 && add_Register_Immediate1.Destination == X86Register.esp && add_Register_Immediate1.ValueToAdd == 4);
                    if (espResetIndex != -1 && !IsEspReferenced(fn.Instructions.Slice(i + 1, espResetIndex), 0))
                    {
                        fn.Instructions.Slice(i + 1, espResetIndex).ForEach(x => optimizedInstructions.Add(TrackInstruction(x)));
                        i = i + espResetIndex + 1;
                        continue;
                    }
                    if (TryGetImmediate(push_Offset.Offset, out var immediate) && !IsReferenced(fn.Instructions, i + 1, push_Offset.Offset))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Push(immediate)));
                        continue;
                    }
                    if (Peek(fn.Instructions, i + 1) is Pop_Register pop_register)
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Mov(pop_register.Destination, push_Offset.Offset)));
                        i++;
                        continue;
                    }
                    if (Peek(fn.Instructions, i + 1) is Add_Register_Immediate add_Register_Immediate1 && add_Register_Immediate1.Destination == X86Register.esp && add_Register_Immediate1.ValueToAdd == 4)
                    {
                        i++;
                        continue;
                    }
                    if (Peek(fn.Instructions, i + 2) is Add_Register_Immediate add_Register_Immediate2 && add_Register_Immediate2.Destination == X86Register.esp && add_Register_Immediate2.ValueToAdd == 4)
                    {
                        var nextInstruction = Peek(fn.Instructions, i + 1);
                        if (nextInstruction != null
                            && !(nextInstruction is Call || nextInstruction is Label || nextInstruction is Jmp)
                            && !IsEspReferenced([nextInstruction], 0))
                        {
                            optimizedInstructions.Add(TrackInstruction(nextInstruction));
                            i = i + 2;
                            continue;
                        }
                    }
                    if (Peek(fn.Instructions, i + 2) is Pop_Register pop_Register1)
                    {
                        var nextInstruction = Peek(fn.Instructions, i + 1);
                        if (nextInstruction != null
                            && !(nextInstruction is Call || nextInstruction is Label || nextInstruction is Jmp)
                            && !IsEspReferenced([nextInstruction], 0)
                            && !IsReferenced([nextInstruction], 0, push_Offset.Offset)
                            && !DoesRegisterLoseIntegrity(nextInstruction, push_Offset.Offset.Register))
                        {

                            // Test for
                            // push [ebp-4]
                            // mov eax, [ebp + 12]
                            // pop ebx
                            // optimization:
                            // mov eax, [ebp + 12]
                            // mov ebx, [ebp - 4]
                            optimizedInstructions.Add(TrackInstruction(nextInstruction));
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Mov(pop_Register1.Destination, push_Offset.Offset)));
                            i = i + 2;
                            continue;
                        }
                    }
                }
                if (instruction is Push_Address push_Address)
                {
                    var espResetIndex = fn.Instructions.Skip(i).ToList().FindIndex(x => x is Add_Register_Immediate add_Register_Immediate1 && add_Register_Immediate1.Destination == X86Register.esp && add_Register_Immediate1.ValueToAdd == 4);
                    if (espResetIndex != -1 && !IsEspReferenced(fn.Instructions.Slice(i + 1, espResetIndex), 0))
                    {
                        fn.Instructions.Slice(i + 1, espResetIndex).ForEach(x => optimizedInstructions.Add(TrackInstruction(x)));
                        i = i + espResetIndex + 1;
                        continue;
                    }
                    if (Peek(fn.Instructions, i + 1) is Add_Register_Immediate add_Register_Immediate1 && add_Register_Immediate1.Destination == X86Register.esp && add_Register_Immediate1.ValueToAdd == 4)
                    {
                        i++;
                        continue;
                    }
                    if (Peek(fn.Instructions, i + 2) is Add_Register_Immediate add_Register_Immediate2 && add_Register_Immediate2.Destination == X86Register.esp && add_Register_Immediate2.ValueToAdd == 4)
                    {
                        var nextInstruction = Peek(fn.Instructions, i + 1);
                        if (nextInstruction != null
                            && !(nextInstruction is Call || nextInstruction is Label || nextInstruction is Jmp)
                            && !IsReferenced([nextInstruction], 0, X86Register.esp))
                        {
                            optimizedInstructions.Add(TrackInstruction(nextInstruction));
                            i = i + 2;
                            continue;
                        }
                    }
                }
                if (instruction is Push_Immediate<int> push_Immediate)
                {
                    var espResetIndex = fn.Instructions.Skip(i + 1).ToList().FindIndex(x => x is Add_Register_Immediate add_Register_Immediate1 && add_Register_Immediate1.Destination == X86Register.esp && add_Register_Immediate1.ValueToAdd == 4);
                    if (espResetIndex != -1 && !IsEspReferenced(fn.Instructions.Slice(i + 1, espResetIndex), 0))
                    {
                        fn.Instructions.Slice(i + 1, espResetIndex).ForEach(x => optimizedInstructions.Add(TrackInstruction(x)));
                        i = i + espResetIndex + 1;
                        continue;
                    }
                    if (Peek(fn.Instructions, i + 1) is Pop_Register pop_register)
                    {
                        // test for
                        // push 1
                        // pop eax
                        // optimization:
                        // mov eax, 1
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Mov(pop_register.Destination, push_Immediate.Immediate)));
                        i++;
                        continue;
                    }
                    if (Peek(fn.Instructions, i + 1) is Add_Register_Immediate add_Register_Immediate1 && add_Register_Immediate1.Destination == X86Register.esp && add_Register_Immediate1.ValueToAdd == 4)
                    {
                        i++;
                        continue;
                    }
                    if (Peek(fn.Instructions, i + 2) is Add_Register_Immediate add_Register_Immediate2 && add_Register_Immediate2.Destination == X86Register.esp && add_Register_Immediate2.ValueToAdd == 4)
                    {
                        var nextInstruction = Peek(fn.Instructions, i + 1);
                        if (nextInstruction != null
                            && !(nextInstruction is Call || nextInstruction is Label || nextInstruction is Jmp)
                            && !IsEspReferenced([nextInstruction], 0))
                        {
                            optimizedInstructions.Add(TrackInstruction(nextInstruction));
                            i = i + 2;
                            continue;
                        }
                    }
                    if (Peek(fn.Instructions, i + 2) is Pop_Register pop_Register2)
                    {
                        var nextInstruction = Peek(fn.Instructions, i + 1);
                        if (nextInstruction != null
                            && !(nextInstruction is Call || nextInstruction is Label || nextInstruction is Jmp)
                            && !IsEspReferenced([nextInstruction], 0))
                        {

                            // Test for
                            // push 98
                            // mov eax, [ebp + 12]
                            // pop edx
                            // optimization:
                            // mov eax, [ebp + 12]
                            // mov edx, ebx
                            optimizedInstructions.Add(TrackInstruction(nextInstruction));
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Mov(pop_Register2.Destination, push_Immediate.Immediate)));
                            i = i + 2;
                            continue;
                        }
                    }
                }
                if (instruction is Lea_Register_Offset lea_Register_Offset)
                {
                    if (!IsReferenced(fn.Instructions, i + 1, lea_Register_Offset.Destination)) continue;
                }
                if (instruction is Mov_Register_Offset mov_Register_Offset)
                {
                    if (IsEquivalent(mov_Register_Offset.Destination, mov_Register_Offset.Source)) continue;
                    if (!IsReferenced(fn.Instructions, i + 1, mov_Register_Offset.Destination)) continue;
                    if (TryGetImmediate(mov_Register_Offset.Source, out var immediate) && !IsReferenced(fn.Instructions, i + 1, mov_Register_Offset.Source))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Mov(mov_Register_Offset.Destination, immediate)));
                        continue;
                    }
                    else if (TryGetRegister(mov_Register_Offset.Source, out var register))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Mov(mov_Register_Offset.Destination, register)));
                        continue;
                    }
                }
                if (instruction is Mov_Offset_Register mov_Offset_Register)
                {
                    if (IsEquivalent(mov_Offset_Register.Destination, mov_Offset_Register.Source)) continue;
                    // We cannot skip any moves to offset because they may be local variables or offsets that may be passed or referenced down the call chain
                    //if (!IsReferenced(fn.Instructions, i + 1, mov_Offset_Register.Destination)) continue;
                    if (TryGetImmediate(mov_Offset_Register.Source, out var immediate))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Mov(mov_Offset_Register.Destination, immediate)));
                        continue;
                    }
                }
                if (instruction is Mov_Offset_Immediate mov_Offset_Immediate)
                {
                    if (IsEquivalent(mov_Offset_Immediate.Destination, mov_Offset_Immediate.Immediate)) continue;
                    // We cannot skip any moves to offset because they may be local variables or offsets that may be passed or referenced down the call chain
                    //if (!IsReferenced(fn.Instructions, i + 1, mov_Offset_Immediate.Destination)) continue;
                }
                if (instruction is Mov_Register_Register mov_Register_Register)
                {
                    if (IsEquivalent(mov_Register_Register.Destination, mov_Register_Register.Source)) continue;
                    if (!IsReferenced(fn.Instructions, i + 1, mov_Register_Register.Destination)) continue;
                    if (TryGetImmediate(mov_Register_Register.Source, out var immediate) && !IsReferenced(fn.Instructions, i + 1, mov_Register_Register.Source))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Mov(mov_Register_Register.Destination, immediate)));
                        continue;
                    }
                    if (Peek(fn.Instructions, i + 1) is Mov_Offset_Register mov_Offset_Register1 && mov_Offset_Register1.Source == mov_Register_Register.Destination)
                    {
                        if (!IsReferenced(fn.Instructions, i + 2, mov_Register_Register.Destination))
                        {
                            // test for
                            // mov eax, ebx
                            // mov [ebp-4], eax
                            // optimization:
                            // mov [ebp-4], ebx
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Mov(mov_Offset_Register1.Destination, mov_Register_Register.Source)));
                            i++;
                            continue;
                        }

                    }
                }
                if (instruction is Mov_Register_Immediate mov_Register_Immediate)
                {
                    if (mov_Register_Immediate.ImmediateValue == 0)
                    {
                        // test for 
                        // mov eax, 0
                        // optimization:
                        // xor eax, eax       (xor register, register faster than mov register, 0)
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Xor(mov_Register_Immediate.Destination, mov_Register_Immediate.Destination)));
                        continue;
                    }
                    if (IsEquivalent(mov_Register_Immediate.Destination, mov_Register_Immediate.ImmediateValue)) continue;
                    if (!IsReferenced(fn.Instructions, i + 1, mov_Register_Immediate.Destination)) continue;
                }
                if (instruction is Mov_Offset_Register__Byte mov_Offset_Register__Byte)
                {
                    // We cannot skip any moves to offset because they may be local variables or offsets that may be passed or referenced down the call chain
                    //if (!IsReferenced(fn.Instructions, i + 1, mov_Offset_Register__Byte.Destination)) continue;
                }
                if (instruction is Movsx_Register_Offset movsx_Register_Offset)
                {
                    // We cannot skip any moves to offset because they may be local variables or offsets that may be passed or referenced down the call chain
                    //if (!IsReferenced(fn.Instructions, i + 1, movsx_Register_Offset.Destination)) continue;
                }
                if (instruction is Sub_Register_Immediate sub_Register_Immediate)
                {
                    if (!IsReferenced(fn.Instructions, i + 1, sub_Register_Immediate.Destination)) continue;


                    if (TryGetImmediate(sub_Register_Immediate.Destination, out var immediateValue))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Mov(sub_Register_Immediate.Destination, immediateValue - sub_Register_Immediate.ValueToSubtract)));
                        continue;
                    }

                    if (Peek(fn.Instructions, i + 1) is Sub_Register_Immediate sub_Register_Immediate1 && sub_Register_Immediate.Destination == sub_Register_Immediate1.Destination)
                    {
                        var finalValue = sub_Register_Immediate.ValueToSubtract + sub_Register_Immediate1.ValueToSubtract;
                        if (finalValue > 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Sub(sub_Register_Immediate.Destination, finalValue)));
                        else if (finalValue < 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Add(sub_Register_Immediate.Destination, -finalValue)));
                        // else do nothing if the final value is 0
                        i++;
                        continue;
                    }

                    if (Peek(fn.Instructions, i + 1) is Add_Register_Immediate add_Register_Immediate1 && sub_Register_Immediate.Destination == add_Register_Immediate1.Destination)
                    {
                        var finalValue = add_Register_Immediate1.ValueToAdd - sub_Register_Immediate.ValueToSubtract;
                        if (finalValue > 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Add(sub_Register_Immediate.Destination, finalValue)));
                        else if (finalValue < 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Sub(sub_Register_Immediate.Destination, -finalValue)));
                        // else do nothing if the final value is 0
                        i++;
                        continue;
                    }

                    if (Peek(fn.Instructions, i + 1) is IMul_Register_Immediate imul_Register_Immediate1 && sub_Register_Immediate.Destination == imul_Register_Immediate1.Destination)
                    {
                        var finalValue = sub_Register_Immediate.ValueToSubtract * imul_Register_Immediate1.Immediate;
                        if (finalValue > 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Sub(sub_Register_Immediate.Destination, finalValue)));
                        else if (finalValue < 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Add(sub_Register_Immediate.Destination, -finalValue)));
                        // else do nothing if the final value is 0
                        i++;
                        continue;
                    }
                    if (sub_Register_Immediate.ValueToSubtract == 1)
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Dec(sub_Register_Immediate.Destination)));
                        continue;
                    }

                }
                if (instruction is Sub_Register_Register sub_Register_Register)
                {
                    if (!IsReferenced(fn.Instructions, i + 1, sub_Register_Register.Destination)) continue;

                    if (TryGetImmediate(sub_Register_Register.Source, out var valueToSubtract))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Sub(sub_Register_Register.Destination, valueToSubtract)));
                        continue;
                    }
                }
                if (instruction is Add_Register_Immediate add_Register_Immediate)
                {
                    if (!IsReferenced(fn.Instructions, i + 1, add_Register_Immediate.Destination)) continue;

                    if (TryGetImmediate(add_Register_Immediate.Destination, out var immediateValue))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Mov(add_Register_Immediate.Destination, immediateValue + add_Register_Immediate.ValueToAdd)));
                        continue;
                    }

                    if (Peek(fn.Instructions, i + 1) is Sub_Register_Immediate sub_Register_Immediate1 && add_Register_Immediate.Destination == sub_Register_Immediate1.Destination)
                    {
                        var finalValue = add_Register_Immediate.ValueToAdd - sub_Register_Immediate1.ValueToSubtract;
                        if (finalValue > 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Add(add_Register_Immediate.Destination, finalValue)));
                        else if (finalValue < 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Sub(add_Register_Immediate.Destination, -finalValue)));
                        // else do nothing if the final value is 0
                        i++;
                        continue;
                    }

                    if (Peek(fn.Instructions, i + 1) is Add_Register_Immediate add_Register_Immediate1 && add_Register_Immediate.Destination == add_Register_Immediate1.Destination)
                    {
                        var finalValue = add_Register_Immediate1.ValueToAdd + add_Register_Immediate.ValueToAdd;
                        if (finalValue > 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Add(add_Register_Immediate.Destination, finalValue)));
                        else if (finalValue < 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Sub(add_Register_Immediate.Destination, -finalValue)));
                        // else do nothing if the final value is 0
                        i++;
                        continue;
                    }

                    if (Peek(fn.Instructions, i + 1) is IMul_Register_Immediate imul_Register_Immediate1 && add_Register_Immediate.Destination == imul_Register_Immediate1.Destination)
                    {
                        var finalValue = add_Register_Immediate.ValueToAdd * imul_Register_Immediate1.Immediate;
                        if (finalValue > 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Add(add_Register_Immediate.Destination, finalValue)));
                        else if (finalValue < 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Sub(add_Register_Immediate.Destination, -finalValue)));
                        // else do nothing if the final value is 0
                        i++;
                        continue;
                    }
                    if (add_Register_Immediate.ValueToAdd == 1)
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Inc(add_Register_Immediate.Destination)));
                        continue;
                    }
                }
                if (instruction is Add_Register_Register add_Register_Register)
                {
                    if (!IsReferenced(fn.Instructions, i + 1, add_Register_Register.Destination)) continue;

                    if (TryGetImmediate(add_Register_Register.Source, out var valueToAdd))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Add(add_Register_Register.Destination, valueToAdd)));
                        continue;
                    }
                }
                if (instruction is And_Register_Register and_Register_Register)
                {
                    if (!IsReferenced(fn.Instructions, i + 1, and_Register_Register.Destination)) continue;

                    if (IsEquivalent(and_Register_Register.Destination, and_Register_Register.Source)) continue;
                }
                if (instruction is Or_Register_Register or_Register_Register)
                {
                    if (!IsReferenced(fn.Instructions, i + 1, or_Register_Register.Destination)) continue;

                    if (IsEquivalent(or_Register_Register.Destination, or_Register_Register.Source)) continue;
                }
                if (instruction is Xor_Register_Register xor_Register_Register)
                {
                    if (!IsReferenced(fn.Instructions, i + 1, xor_Register_Register.Destination)) continue;

                }
                if (instruction is Pop_Register pop_Register)
                {

                }
                if (instruction is Neg_Offset neg_Offset)
                {
                    if (!IsReferenced(fn.Instructions, i + 1, neg_Offset.Operand)) continue;

                    if (Peek(fn.Instructions, i + 1) is Neg_Offset neg_Offset1 && neg_Offset.Operand.Equals(neg_Offset1.Operand))
                    {
                        // test for 
                        // neg [esp]
                        // neg [esp]
                        // optimization:
                        // ...              (no instructions necessary)
                        i++;
                        continue;
                    }
                }
                if (instruction is Not_Offset not_Offset)
                {
                    if (!IsReferenced(fn.Instructions, i + 1, not_Offset.Operand)) continue;

                    if (Peek(fn.Instructions, i + 1) is Not_Offset not_Offset1 && not_Offset.Operand.Equals(not_Offset1.Operand))
                    {
                        // test for 
                        // not [esp]
                        // not [esp]
                        // optimization:
                        // ...              (no instructions necessary)
                        i++;
                        continue;
                    }
                }
                if (instruction is IDiv_Offset idiv_Offset)
                {

                }
                if (instruction is IMul_Register_Register imul_Register_Register)
                {
                    if (!IsReferenced(fn.Instructions, i + 1, imul_Register_Register.Destination)) continue;

                    if (TryGetImmediate(imul_Register_Register.Source, out var valueToMultiply))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.IMul(imul_Register_Register.Destination, valueToMultiply)));
                        continue;
                    }
                }
                if (instruction is IMul_Register_Immediate imul_Register_Immediate)
                {
                    if (!IsReferenced(fn.Instructions, i + 1, imul_Register_Immediate.Destination)) continue;
                    if (imul_Register_Immediate.Immediate == 1) continue;
                    if (TryGetImmediate(imul_Register_Immediate.Destination, out var immediateValue))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Mov(imul_Register_Immediate.Destination, immediateValue * imul_Register_Immediate.Immediate)));
                        continue;
                    }

                    if (Peek(fn.Instructions, i + 1) is Sub_Register_Immediate sub_Register_Immediate1 && imul_Register_Immediate.Destination == sub_Register_Immediate1.Destination)
                    {
                        var finalValue = imul_Register_Immediate.Immediate * sub_Register_Immediate1.ValueToSubtract;
                        if (finalValue > 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Add(imul_Register_Immediate.Destination, finalValue)));
                        else if (finalValue < 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Sub(imul_Register_Immediate.Destination, -finalValue)));
                        // else do nothing if the final value is 0
                        i++;
                        continue;
                    }

                    if (Peek(fn.Instructions, i + 1) is Add_Register_Immediate add_Register_Immediate1 && imul_Register_Immediate.Destination == add_Register_Immediate1.Destination)
                    {
                        var finalValue = add_Register_Immediate1.ValueToAdd * imul_Register_Immediate.Immediate;
                        if (finalValue > 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Add(imul_Register_Immediate.Destination, finalValue)));
                        else if (finalValue < 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Sub(imul_Register_Immediate.Destination, -finalValue)));
                        // else do nothing if the final value is 0
                        i++;
                        continue;
                    }

                    if (Peek(fn.Instructions, i + 1) is IMul_Register_Immediate imul_Register_Immediate1 && imul_Register_Immediate.Destination == imul_Register_Immediate1.Destination)
                    {
                        var finalValue = imul_Register_Immediate.Immediate * imul_Register_Immediate1.Immediate;
                        if (finalValue > 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Add(imul_Register_Immediate.Destination, finalValue)));
                        else if (finalValue < 0) optimizedInstructions.Add(TrackInstruction(X86Instructions.Sub(imul_Register_Immediate.Destination, -finalValue)));
                        // else do nothing if the final value is 0
                        i++;
                        continue;
                    }
                }
                if (instruction is Add_Register_Offset add_Register_Offset)
                {
                    if (!IsReferenced(fn.Instructions, i + 1, add_Register_Offset.Destination)) continue;

                    if (TryGetImmediate(add_Register_Offset.Source, out var valueToAdd))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Add(add_Register_Offset.Destination, valueToAdd)));
                        continue;
                    }
                }
                if (instruction is Jmp jmp)
                {
                    if (jmp.Emit().StartsWith("jmp")) // If it is an unconditional jump
                    {
                        if (Peek(fn.Instructions, i + 1) is Label label1 && label1.Text == jmp.Label)
                        {
                            // if we are jumping unconditionally to a label that immediately follows the jump, we do not need to jump
                            // but we cannot omit the label because it may be referenced elsewhere (or externally)
                            continue;
                        }
                        // otherwise search for the next label
                        var nextLabelIndex = fn.Instructions.Skip(i).ToList().FindIndex(x => x is Label);
                        if (nextLabelIndex != -1) // if we found a label
                        {
                            // skip to the next label since code up until that point is unreachable
                            i = nextLabelIndex + i - 1;
                            optimizedInstructions.Add(TrackInstruction(jmp));
                            continue;
                        }



                    }
                }
                if (instruction is Test_Register_Register test_Register_Register)
                {

                }
                if (instruction is Test_Register_Offset test_Register_Offset)
                {

                }
                if (instruction is Cmp_Register_Register cmp_Register_Register)
                {
                    if (TryGetImmediate(cmp_Register_Register.Operand2, out var immediate))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Cmp(cmp_Register_Register.Operand1, immediate)));
                        continue;
                    }
                }
                if (instruction is Cmp_Register_Immediate cmp_Register_Immediate)
                {
                    if (TryGetImmediate(cmp_Register_Immediate.Operand1, out var immediate))
                    {
                        if (Peek(fn.Instructions, i + 1) is JmpEq jmpEq && immediate == cmp_Register_Immediate.Operand2)
                        {
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Jmp(jmpEq.Label)));
                            i++;
                            continue;
                        }
                        if (Peek(fn.Instructions, i + 1) is JmpNeq jmpNeq && immediate != cmp_Register_Immediate.Operand2)
                        {
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Jmp(jmpNeq.Label)));
                            i++;
                            continue;
                        }
                        if (Peek(fn.Instructions, i + 1) is JmpGt jmpGt && immediate > cmp_Register_Immediate.Operand2)
                        {
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Jmp(jmpGt.Label)));
                            i++;
                            continue;
                        }
                        if (Peek(fn.Instructions, i + 1) is JmpGte jmpGte && immediate >= cmp_Register_Immediate.Operand2)
                        {
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Jmp(jmpGte.Label)));
                            i++;
                            continue;
                        }
                        if (Peek(fn.Instructions, i + 1) is JmpLt jmpLt && immediate < cmp_Register_Immediate.Operand2)
                        {
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Jmp(jmpLt.Label)));
                            i++;
                            continue;
                        }
                        if (Peek(fn.Instructions, i + 1) is JmpLte jmpLte && immediate <= cmp_Register_Immediate.Operand2)
                        {
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Jmp(jmpLte.Label)));
                            i++;
                            continue;
                        }
                        if (Peek(fn.Instructions, i + 1) is Jz jz && immediate == cmp_Register_Immediate.Operand2)
                        {
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Jmp(jz.Label)));
                            i++;
                            continue;
                        }
                        if (Peek(fn.Instructions, i + 1) is Jnz jnz && immediate != cmp_Register_Immediate.Operand2)
                        {
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Jmp(jnz.Label)));
                            i++;
                            continue;
                        }
                        if (Peek(fn.Instructions, i + 1) is Ja ja && immediate > cmp_Register_Immediate.Operand2)
                        {
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Jmp(ja.Label)));
                            i++;
                            continue;
                        }
                        if (Peek(fn.Instructions, i + 1) is Jae jae && immediate >= cmp_Register_Immediate.Operand2)
                        {
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Jmp(jae.Label)));
                            i++;
                            continue;
                        }
                        if (Peek(fn.Instructions, i + 1) is Jb jb && immediate < cmp_Register_Immediate.Operand2)
                        {
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Jmp(jb.Label)));
                            i++;
                            continue;
                        }
                        if (Peek(fn.Instructions, i + 1) is Jbe jbe && immediate <= cmp_Register_Immediate.Operand2)
                        {
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Jmp(jbe.Label)));
                            i++;
                            continue;
                        }

                    }
                }
                if (instruction is Cmp_Byte_Byte cmp_Byte_Byte)
                {

                }
                if (instruction is Call call)
                {

                }
                if (instruction is Label label)
                {

                }
                if (instruction is Ret ret)
                {

                }
                if (instruction is Ret_Immediate ret_Immediate)
                {

                }
                if (instruction is Fstp_Offset fstp_Offset)
                {

                }
                if (instruction is Fld_Offset fld_Offset)
                {

                }
                if (instruction is Movss_Offset_Register movss_Offset_Register)
                {

                }
                if (instruction is Movss_Register_Offset movss_Register_Offset)
                {
                    if (IsEquivalent(movss_Register_Offset.Destination, movss_Register_Offset.Source)) continue;
                    if (!IsReferenced(fn.Instructions, i + 1, movss_Register_Offset.Destination)) continue;
                    if (TryGetXmmRegister(movss_Register_Offset.Source, out var register))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Movss(movss_Register_Offset.Destination, register)));
                        continue;
                    }
                }
                if (instruction is Movss_Register_Register movss_Register_Register)
                {
                    if (movss_Register_Register.Destination == movss_Register_Register.Source) continue;
                }
                if (instruction is Comiss_Register_Offset comiss_Register_Offset)
                {

                }
                if (instruction is Comiss_Register_Register comiss_Register_Register)
                {

                }
                if (instruction is Ucomiss_Register_Register ucomiss_Register_Register)
                {

                }
                if (instruction is Addss_Register_Offset addss_Register_Offset)
                {
                    if (TryGetImmediateFloat(addss_Register_Offset.Source, out var sourceFloat))
                    {
                        // Test for the following:
                        // addss xmm0, [ebp-4] ;ebp-4 is 3.02
                        // subss xmm0, [ebp-8] ;ebp-8 is 3.02
                        // optimization:
                        // ...         (no instructions necessary)
                        if (Peek(fn.Instructions, i + 1) is Subss_Register_Offset subss_Register_Offset1
                            && subss_Register_Offset1.Destination == addss_Register_Offset.Destination
                            && TryGetImmediateFloat(subss_Register_Offset1.Source, out var valueToSubtract)
                            && valueToSubtract == sourceFloat)
                        {
                            continue;
                        }
                    }
                }
                if (instruction is Subss_Register_Offset subss_Register_Offset)
                {
                    if (TryGetImmediateFloat(subss_Register_Offset.Source, out var sourceFloat))
                    {
                        // Test for the following:
                        // subss xmm0, [ebp-4] ;ebp-4 is 3.02
                        // addss xmm0, [ebp-8] ;ebp-8 is 3.02
                        // optimization:
                        // ...         (no instructions necessary)
                        if (Peek(fn.Instructions, i + 1) is Addss_Register_Offset addss_Register_Offset1
                            && addss_Register_Offset1.Destination == subss_Register_Offset.Destination
                            && TryGetImmediateFloat(addss_Register_Offset1.Source, out var valueToAdd)
                            && valueToAdd == sourceFloat)
                        {
                            continue;
                        }
                    }
                }
                if (instruction is Mulss_Register_Offset mulss_Register_Offset)
                {

                }
                if (instruction is Divss_Register_Offset divss_Register_Offset)
                {

                }
                if (instruction is Cvtsi2ss_Register_Offset cvtsi2Ss_Register_Offset)
                {

                }
                if (instruction is Cvtss2si_Register_Offset cvtss2Si_Register_Offset)
                {

                }
                if (instruction is Push_SymbolOffset push_SymbolOffset)
                {
                    if (Peek(fn.Instructions, i + 1) is Add_Register_Immediate add_Register_Immediate1 && add_Register_Immediate1.Destination == X86Register.esp && add_Register_Immediate1.ValueToAdd == 4)
                    {
                        i++;
                        continue;
                    }

                    if (TryGetImmediate(push_SymbolOffset.Offset, out var immediate))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Push(immediate)));
                        continue;
                    }
                }
                if (instruction is Lea_Register_SymbolOffset lea_Register_SymbolOffset)
                {

                }
                if (instruction is Mov_SymbolOffset_Register mov_SymbolOffset_Register)
                {
                    if (IsEquivalent(mov_SymbolOffset_Register.Destination, mov_SymbolOffset_Register.Source)) continue;
                    if (TryGetImmediate(mov_SymbolOffset_Register.Source, out var immediate))
                    {
                        optimizedInstructions.Add(TrackInstruction(X86Instructions.Mov(mov_SymbolOffset_Register.Destination, immediate)));
                        continue;
                    }
                }
                if (instruction is Mov_SymbolOffset_Register__Byte mov_SymbolOffset_Register__Byte)
                {

                }
                if (instruction is Movsx_Register_SymbolOffset__Byte movsx_Register_SymbolOffset__Byte)
                {

                }
                if (instruction is Mov_SymbolOffset_Immediate mov_SymbolOffset_Immediate)
                {
                    if (IsEquivalent(mov_SymbolOffset_Immediate.Destination, mov_SymbolOffset_Immediate.ImmediateValue)) continue;
                }
                if (instruction is Inc_Register inc_Register)
                {
                    if (_registerValues.TryGetValue(inc_Register.Destination, out var value) && value.IsRegisterOffset)
                    {
                        if (Peek(fn.Instructions, i + 1) is Mov_Offset_Register mov_Offset_Register1
                                && mov_Offset_Register1.Source == inc_Register.Destination // if the registers only reference is moving it back into the offset it came from
                                && mov_Offset_Register1.Destination.Equals(value.RegisterOffset)
                                && !IsReferenced(fn.Instructions, i + 2, inc_Register.Destination))
                        {
                            // Test for 			
                            // mov ebx, dword [ebp - 12]
                            // inc ebx
                            // mov dword[ebp - 12], ebx
                            // optimization
                            // inc [ebp -12]
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Inc(value.RegisterOffset)));
                            continue;
                        }

                    }
                }
                if (instruction is Dec_Register dec_Register)
                {
                    if (_registerValues.TryGetValue(dec_Register.Destination, out var value) && value.IsRegisterOffset)
                    {
                        if (Peek(fn.Instructions, i + 1) is Mov_Offset_Register mov_Offset_Register1
                                && mov_Offset_Register1.Source == dec_Register.Destination // if the registers only reference is moving it back into the offset it came from
                                && mov_Offset_Register1.Destination.Equals(value.RegisterOffset)
                                && !IsReferenced(fn.Instructions, i + 2, dec_Register.Destination))
                        {
                            optimizedInstructions.Add(TrackInstruction(X86Instructions.Dec(value.RegisterOffset)));
                            continue;
                        }

                    }
                }
                if (instruction is Inc_Offset inc_Offset)
                {

                }
                if (instruction is Dec_Offset dec_Offset)
                {

                }
                if (instruction is Mov_SymbolOffset_Byte_Register__Byte mov_SymbolOffset_Byte_Register__Byte)
                {

                }
                if (instruction is Mov_RegisterOffset_Byte_Register__Byte mov_RegisterOffset_Byte_Register__Byte)
                {

                }

                optimizedInstructions.Add(TrackInstruction(instruction));
            }

            fn.Instructions = optimizedInstructions;
        }
        return compilationResult;
    }



    private bool IsReferenced(List<X86Instruction> instructions, int index, RegisterOffset originalOffset, RegisterOffset? offset = null, HashSet<string>? exploredLabels = null)
    {
        if (index >= instructions.Count) return false;
        if (offset == null) offset = originalOffset;
        if (exploredLabels == null) exploredLabels = new HashSet<string>();
        var instruction = instructions[index];

        if (instruction is Cdq cdq)
        {
            if (offset.Register == X86Register.eax || offset.Register == X86Register.edx) return false;
        }
        if (instruction is Push_Register push_Register)
        {
            if (offset.Register == X86Register.esp)
                offset = Offset.Create(offset.Register, offset.Offset + 4); // pushing to the stack alters it by -4 (here we add 4 since IE [esp+4] push 0 means [esp+4] is actually [esp+8] now)
        }
        if (instruction is Push_Offset push_Offset)
        {
            if (push_Offset.Offset.Equals(offset)) return true;
            if (offset.Register == X86Register.esp)
                offset = Offset.Create(offset.Register, offset.Offset + 4); // pushing to the stack alters it by -4 (here we add 4 since IE [esp+4] push 0 means [esp+4] is actually [esp+8] now)
        }
        if (instruction is Push_Address push_Address)
        {
            if (offset.Register == X86Register.esp)
                offset = Offset.Create(offset.Register, offset.Offset + 4); // pushing to the stack alters it by -4 (here we add 4 since IE [esp+4] push 0 means [esp+4] is actually [esp+8] now)
        }
        if (instruction is Push_Immediate<int> push_Immediate)
        {
            if (offset.Register == X86Register.esp)
                offset = Offset.Create(offset.Register, offset.Offset + 4); // pushing to the stack alters it by -4 (here we add 4 since IE [esp+4] push 0 means [esp+4] is actually [esp+8] now)
        }
        if (instruction is Lea_Register_Offset lea_Register_Offset)
        {
            if (lea_Register_Offset.Source.Equals(offset)) return true;
        }
        if (instruction is Mov_Register_Offset mov_Register_Offset)
        {
            if (mov_Register_Offset.Source.Equals(offset)) return true;
        }
        if (instruction is Mov_Offset_Register mov_Offset_Register)
        {
            if (mov_Offset_Register.Destination.Equals(offset)) return false;
        }
        if (instruction is Mov_Offset_Immediate mov_Offset_Immediate)
        {
            if (mov_Offset_Immediate.Destination.Equals(offset)) return false;
        }
        if (instruction is Mov_Register_Register mov_Register_Register)
        {
            if (mov_Register_Register.Destination == offset.Register) return false;
        }
        if (instruction is Mov_Register_Immediate mov_Register_Immediate)
        {
            if (mov_Register_Immediate.Destination == offset.Register) return false;
        }
        if (instruction is Mov_Offset_Register__Byte mov_Offset_Register__Byte)
        {
            if (mov_Offset_Register__Byte.Destination.Equals(offset)) return false;
        }
        if (instruction is Movsx_Register_Offset movsx_Register_Offset)
        {
            if (movsx_Register_Offset.Source.Equals(offset)) return true;
        }
        if (instruction is Sub_Register_Immediate sub_Register_Immediate)
        {
            if (sub_Register_Immediate.Destination == offset.Register)
            {
                //eax + 4
                // [eax-4]
                // sub eax, 4 //eax
                //
                // [eax+4]
                // sub eax, 4 // eax+8
                offset = Offset.Create(offset.Register, offset.Offset + sub_Register_Immediate.ValueToSubtract);
            }
        }
        if (instruction is Sub_Register_Register sub_Register_Register)
        {
            if (sub_Register_Register.Destination == offset.Register) return true; // Unable to determine so we must assume so
        }
        if (instruction is Add_Register_Immediate add_Register_Immediate)
        {
            if (add_Register_Immediate.Destination == offset.Register)
            {
                //
                // [eax-4]
                // add eax, 4 //eax-8
                //
                // [eax+4]
                // add eax, 4 // eax
                offset = Offset.Create(offset.Register, offset.Offset - add_Register_Immediate.ValueToAdd);
            }
        }
        if (instruction is Add_Register_Register add_Register_Register)
        {
            if (add_Register_Register.Destination == offset.Register) return true; // Unable to determine so we must assume so
        }
        if (instruction is And_Register_Register and_Register_Register)
        {
            if (and_Register_Register.Destination == offset.Register) return true; // Unable to determine so we must assume so
        }
        if (instruction is Or_Register_Register or_Register_Register)
        {
            if (or_Register_Register.Destination == offset.Register) return true; // Unable to determine so we must assume so
        }
        if (instruction is Xor_Register_Register xor_Register_Register)
        {
            if (xor_Register_Register.Destination == offset.Register) return true; // Unable to determine so we must assume so
        }
        if (instruction is Pop_Register pop_Register)
        {
            if (pop_Register.Destination == offset.Register) return false;
            if (offset.Register == X86Register.esp)
                offset = Offset.Create(offset.Register, offset.Offset - 4); // popping from the stack alters it by +4 (here we subtract 4 since IE [esp+4] pop eax means [esp+4] is actually [esp] now)
        }
        if (instruction is Neg_Offset neg_Offset)
        {
            if (neg_Offset.Operand.Equals(offset)) return true;
        }
        if (instruction is Not_Offset not_Offset)
        {
            if (not_Offset.Operand.Equals(offset)) return true;
        }
        if (instruction is IDiv_Offset idiv_Offset)
        {
            if (idiv_Offset.Divisor.Equals(offset)) return true;
        }
        if (instruction is IMul_Register_Register imul_Register_Register)
        {
            if (imul_Register_Register.Destination == offset.Register) return true; // Unable to determine so we must assume so
        }
        if (instruction is IMul_Register_Immediate imul_Register_Immediate)
        {
            if (imul_Register_Immediate.Destination == offset.Register) return true; // Unable to determine so we must assume so
        }
        if (instruction is Add_Register_Offset add_Register_Offset)
        {
            if (add_Register_Offset.Source.Equals(offset)) return true;
        }
        if (instruction is Jmp jmp)
        {
            if (!exploredLabels.Contains(jmp.Label))
            {
                var labelIndex = instructions.FindIndex(x => x is Label l && l.Text == jmp.Label);
                if (labelIndex == -1) return true; // we cannot find the label so we must assume it is referenced

                if (jmp.Emit().StartsWith("jmp")) // if it is unconditional jump
                    return IsReferenced(instructions, labelIndex, originalOffset, null, exploredLabels);
                else
                {
                    var refencedInBranch = IsReferenced(instructions, labelIndex, originalOffset, null, exploredLabels);
                    if (refencedInBranch) return true;
                    // Otherwise keep going
                }
            }
            else
            {
                // otherwise, we've already explored the jump
                if (jmp.Emit().StartsWith("jmp")) // it is an unconditional jump that we've already explored
                    return false;

            }
        }
        if (instruction is Test_Register_Register test_Register_Register)
        {

        }
        if (instruction is Test_Register_Offset test_Register_Offset)
        {
            if (test_Register_Offset.Operand2.Equals(offset)) return true;
        }
        if (instruction is Cmp_Register_Register cmp_Register_Register)
        {

        }
        if (instruction is Cmp_Register_Immediate cmp_Register_Immediate)
        {

        }
        if (instruction is Cmp_Byte_Byte cmp_Byte_Byte)
        {

        }
        if (instruction is Call call)
        {

        }
        if (instruction is Label label)
        {
            if (exploredLabels.Contains(label.Text)) return false;
            exploredLabels.Add(label.Text);
        }
        if (instruction is Ret ret)
        {
            return false;
        }
        if (instruction is Ret_Immediate ret_Immediate)
        {
            return false;
        }
        if (instruction is Fstp_Offset fstp_Offset)
        {
            if (fstp_Offset.Destination.Equals(offset)) return false;
        }
        if (instruction is Fld_Offset fld_Offset)
        {
            if (fld_Offset.Source.Equals(offset)) return true;
        }
        if (instruction is Movss_Offset_Register movss_Offset_Register)
        {
            if (movss_Offset_Register.Destination.Equals(offset)) return false;
        }
        if (instruction is Movss_Register_Offset movss_Register_Offset)
        {
            if (movss_Register_Offset.Source.Equals(offset)) return true;
        }
        if (instruction is Movss_Register_Register movss_Register_Register)
        {

        }
        if (instruction is Comiss_Register_Offset comiss_Register_Offset)
        {
            if (comiss_Register_Offset.Operand2.Equals(offset)) return true;
        }
        if (instruction is Comiss_Register_Register comiss_Register_Register)
        {

        }
        if (instruction is Ucomiss_Register_Register ucomiss_Register_Register)
        {

        }
        if (instruction is Addss_Register_Offset addss_Register_Offset)
        {
            if (addss_Register_Offset.Source.Equals(offset)) return true;
        }
        if (instruction is Subss_Register_Offset subss_Register_Offset)
        {
            if (subss_Register_Offset.Source.Equals(offset)) return true;
        }
        if (instruction is Mulss_Register_Offset mulss_Register_Offset)
        {
            if (mulss_Register_Offset.Source.Equals(offset)) return true;
        }
        if (instruction is Divss_Register_Offset divss_Register_Offset)
        {
            if (divss_Register_Offset.Source.Equals(offset)) return true;
        }
        if (instruction is Cvtsi2ss_Register_Offset cvtsi2Ss_Register_Offset)
        {
            if (cvtsi2Ss_Register_Offset.Source.Equals(offset)) return true;
        }
        if (instruction is Cvtss2si_Register_Offset cvtss2Si_Register_Offset)
        {
            if (cvtss2Si_Register_Offset.Source.Equals(offset)) return true;
        }
        if (instruction is Push_SymbolOffset push_SymbolOffset)
        {

        }
        if (instruction is Lea_Register_SymbolOffset lea_Register_SymbolOffset)
        {
            if (lea_Register_SymbolOffset.Destination == offset.Register) return false;

        }
        if (instruction is Mov_SymbolOffset_Register mov_SymbolOffset_Register)
        {

        }
        if (instruction is Mov_SymbolOffset_Register__Byte mov_SymbolOffset_Register__Byte)
        {

        }
        if (instruction is Movsx_Register_SymbolOffset__Byte movsx_Register_SymbolOffset)
        {
            if (movsx_Register_SymbolOffset.Destination == offset.Register) return false;
        }
        if (instruction is Mov_SymbolOffset_Immediate mov_SymbolOffset_Immediate)
        {

        }
        if (instruction is Inc_Register inc_Register)
        {
            if (inc_Register.Destination == offset.Register)
            {
                //
                // [eax-4]
                // add eax, 1 //eax-5
                //
                // [eax+4]
                // add eax, 1 // eax+3
                offset = Offset.Create(offset.Register, offset.Offset - 1);
            }
        }
        if (instruction is Dec_Register dec_Register)
        {
            if (dec_Register.Destination == offset.Register)
            {
                //eax + 4
                // [eax-4]
                // sub eax, 1 //eax-3
                //
                // [eax+4]
                // sub eax, 1 // eax+5
                offset = Offset.Create(offset.Register, offset.Offset + 1);
            }
        }
        if (instruction is Inc_Offset inc_Offset)
        {
            if (inc_Offset.Destination.Equals(offset)) return true;
        }
        if (instruction is Dec_Offset dec_Offset)
        {
            if (dec_Offset.Destination.Equals(offset)) return true;
        }
        else if (instruction is Mov_SymbolOffset_Byte_Register__Byte mov_SymbolOffset_Byte_Register__Byte)
        {
        }
        else if (instruction is Mov_RegisterOffset_Byte_Register__Byte mov_RegisterOffset_Byte_Register__Byte)
        {
            if (mov_RegisterOffset_Byte_Register__Byte.Destination.Equals(offset)) return true;
        }

        return IsReferenced(instructions, index + 1, originalOffset, offset, exploredLabels);
    }

    private bool IsReferenced(List<X86Instruction> instructions, int index, X86Register register, HashSet<string>? exploredLabels = null)
    {
        if (index >= instructions.Count) return false;
        if (register == X86Register.esp) return true; // Ignore stack operations (See IsEspReferenced)
        if (exploredLabels == null) exploredLabels = new HashSet<string>();
        var instruction = instructions[index];

        if (instruction is Jmp jmp)
        {
            if (!exploredLabels.Contains(jmp.Label))
            {
                var labelIndex = instructions.FindIndex(x => x is Label l && l.Text == jmp.Label);
                if (labelIndex == -1) return true; // we cannot find the label so we must assume it is referenced

                if (jmp.Emit().StartsWith("jmp")) // if it is unconditional jump
                    return IsReferenced(instructions, labelIndex, register, exploredLabels);
                else
                {
                    var refencedInBranch = IsReferenced(instructions, labelIndex, register, exploredLabels);
                    if (refencedInBranch) return true;
                    // Otherwise keep going
                }
            }
            else
            {
                // otherwise, we've already explored the jump
                if (jmp.Emit().StartsWith("jmp")) // it is an unconditional jump that we've already explored
                    return false;

            }
        }
        else if (instruction is Label label)
        {
            if (exploredLabels.Contains(label.Text)) return false;
            exploredLabels.Add(label.Text);
        }
        else
        {
            var isReferenced = IsRegisterReferencedHelper(instruction, register);
            if (isReferenced != null) return isReferenced.Value;
            // If null, we are unable to determine yet, so continue
        }


        return IsReferenced(instructions, index + 1, register, exploredLabels);
    }

    private bool IsEspReferenced(List<X86Instruction> instructions, int index, HashSet<string>? exploredLabels = null)
    {
        // Esp requires specific handling due to it being the stack pointer

        if (index >= instructions.Count) return false;
        if (exploredLabels == null) exploredLabels = new HashSet<string>();
        var instruction = instructions[index];

        if (instruction is Jmp jmp)
        {
            if (!exploredLabels.Contains(jmp.Label))
            {
                var labelIndex = instructions.FindIndex(x => x is Label l && l.Text == jmp.Label);
                if (labelIndex == -1) return true; // we cannot find the label so we must assume it is referenced

                if (jmp.Emit().StartsWith("jmp")) // if it is unconditional jump
                    return IsEspReferenced(instructions, labelIndex, exploredLabels);
                else
                {
                    var refencedInBranch = IsEspReferenced(instructions, labelIndex, exploredLabels);
                    if (refencedInBranch) return true;
                    // Otherwise keep going
                }
            }
            else
            {
                // otherwise, we've already explored the jump
                if (jmp.Emit().StartsWith("jmp")) // it is an unconditional jump that we've already explored
                    return false;
            }
        }
        else if (instruction is Label label)
        {
            if (exploredLabels.Contains(label.Text)) return false;
            exploredLabels.Add(label.Text);
        }
        else
        {
            var isReferenced = IsRegisterReferencedHelper(instruction, X86Register.esp);
            if (isReferenced != null) return isReferenced.Value;
            // If null, we are unable to determine yet, so continue
        }

        return IsEspReferenced(instructions, index + 1, exploredLabels);
    }

    private bool IsReferenced(List<X86Instruction> instructions, int index, XmmRegister register, HashSet<string>? exploredLabels = null)
    {
        if (index >= instructions.Count) return false;
        if (exploredLabels == null) exploredLabels = new HashSet<string>();
        var instruction = instructions[index];

        if (instruction is Jmp jmp)
        {
            if (!exploredLabels.Contains(jmp.Label))
            {
                var labelIndex = instructions.FindIndex(x => x is Label l && l.Text == jmp.Label);
                if (labelIndex == -1) return true; // we cannot find the label so we must assume it is referenced

                if (jmp.Emit().StartsWith("jmp")) // if it is unconditional jump
                    return IsReferenced(instructions, labelIndex, register, exploredLabels);
                else
                {
                    var refencedInBranch = IsReferenced(instructions, labelIndex, register, exploredLabels);
                    if (refencedInBranch) return true;
                    // Otherwise keep going
                }
            }
            else
            {
                // otherwise, we've already explored the jump
                if (jmp.Emit().StartsWith("jmp")) // it is an unconditional jump that we've already explored
                    return false;

            }
        }
        else if (instruction is Label label)
        {
            if (exploredLabels.Contains(label.Text)) return false;
            exploredLabels.Add(label.Text);
        }
        else
        {
            var isReferenced = IsRegisterReferencedHelper(instruction, register);
            if (isReferenced != null) return isReferenced.Value;
            // If null, we are unable to determine yet, so continue
        }


        return IsReferenced(instructions, index + 1, register, exploredLabels);
    }



    private bool? IsRegisterReferencedHelper(X86Instruction instruction, X86Register register)
    {
        if (instruction is Cdq cdq)
        {
            if (register == X86Register.eax) return true;
            if (register == X86Register.edx) return false;
        }
        else if (instruction is Push_Register push_Register)
        {
            if (push_Register.Register == register) return true;
            if (register == X86Register.esp) return true;
        }
        else if (instruction is Push_Offset push_Offset)
        {
            if (push_Offset.Offset.Register == register) return true;
            if (register == X86Register.esp) return true;
        }
        else if (instruction is Push_Address push_Address)
        {
            if (register == X86Register.esp) return true;
        }
        else if (instruction is Push_Immediate<int> push_Immediate)
        {
            if (register == X86Register.esp) return true;
        }
        else if (instruction is Lea_Register_Offset lea_Register_Offset)
        {
            if (lea_Register_Offset.Source.Register == register) return true;
            if (lea_Register_Offset.Destination == register) return false;
        }
        else if (instruction is Mov_Register_Offset mov_Register_Offset)
        {
            if (mov_Register_Offset.Source.Register == register) return true;
            if (mov_Register_Offset.Destination == register) return false;
        }
        else if (instruction is Mov_Offset_Register mov_Offset_Register)
        {
            if (mov_Offset_Register.Destination.Register == register || mov_Offset_Register.Source == register) return true;
        }
        else if (instruction is Mov_Offset_Immediate mov_Offset_Immediate)
        {
            if (mov_Offset_Immediate.Destination.Register == register) return true;
        }
        else if (instruction is Mov_Register_Register mov_Register_Register)
        {
            if (mov_Register_Register.Source == register) return true;
            if (mov_Register_Register.Destination == register) return false;
        }
        else if (instruction is Mov_Register_Immediate mov_Register_Immediate)
        {
            if (mov_Register_Immediate.Destination == register) return false;
        }
        else if (instruction is Mov_Offset_Register__Byte mov_Offset_Register__Byte)
        {
            if (mov_Offset_Register__Byte.Destination.Register == register || mov_Offset_Register__Byte.Source.ToFullRegister() == register) return true;
        }
        else if (instruction is Movsx_Register_Offset movsx_Register_Offset)
        {
            if (movsx_Register_Offset.Destination == register || movsx_Register_Offset.Source.Register == register) return true;
        }
        else if (instruction is Sub_Register_Immediate sub_Register_Immediate)
        {
            if (sub_Register_Immediate.Destination == register) return true;
        }
        else if (instruction is Sub_Register_Register sub_Register_Register)
        {
            if (sub_Register_Register.Destination == register || sub_Register_Register.Source == register) return true;
        }
        else if (instruction is Add_Register_Immediate add_Register_Immediate)
        {
            if (add_Register_Immediate.Destination == register) return true;
        }
        else if (instruction is Add_Register_Register add_Register_Register)
        {
            if (add_Register_Register.Destination == register || add_Register_Register.Source == register) return true;
        }
        else if (instruction is And_Register_Register and_Register_Register)
        {
            if (and_Register_Register.Destination == register || and_Register_Register.Source == register) return true;
        }
        else if (instruction is Or_Register_Register or_Register_Register)
        {
            if (or_Register_Register.Destination == register || or_Register_Register.Source == register) return true;
        }
        else if (instruction is Xor_Register_Register xor_Register_Register)
        {
            if (xor_Register_Register.Destination == register || xor_Register_Register.Source == register) return true;
        }
        else if (instruction is Pop_Register pop_Register)
        {
            if (register == X86Register.esp) return true;
            if (pop_Register.Destination == register) return false;
        }
        else if (instruction is Neg_Offset neg_Offset)
        {
            if (neg_Offset.Operand.Register == register) return true;
        }
        else if (instruction is Not_Offset not_Offset)
        {
            if (not_Offset.Operand.Register == register) return true;
        }
        else if (instruction is IDiv_Offset idiv_Offset)
        {
            if (idiv_Offset.Divisor.Register == register) return true;
        }
        else if (instruction is IMul_Register_Register imul_Register_Register)
        {
            if (imul_Register_Register.Destination == register || imul_Register_Register.Source == register) return true;
        }
        else if (instruction is IMul_Register_Immediate imul_Register_Immediate)
        {
            if (imul_Register_Immediate.Destination == register) return true;
        }
        else if (instruction is Add_Register_Offset add_Register_Offset)
        {
            if (add_Register_Offset.Source.Register == register || add_Register_Offset.Destination == register) return true;
        }
        else if (instruction is Test_Register_Register test_Register_Register)
        {
            if (test_Register_Register.Operand1 == register || test_Register_Register.Operand2 == register) return true;
        }
        else if (instruction is Test_Register_Offset test_Register_Offset)
        {
            if (test_Register_Offset.Operand1 == register || test_Register_Offset.Operand2.Register == register) return true;
        }
        else if (instruction is Cmp_Register_Register cmp_Register_Register)
        {
            if (cmp_Register_Register.Operand1 == register || cmp_Register_Register.Operand2 == register) return true;
        }
        else if (instruction is Cmp_Register_Immediate cmp_Register_Immediate)
        {
            if (cmp_Register_Immediate.Operand1 == register) return true;
        }
        else if (instruction is Cmp_Byte_Byte cmp_Byte_Byte)
        {
            if (cmp_Byte_Byte.Operand1.ToFullRegister() == register || cmp_Byte_Byte.Operand2.ToFullRegister() == register) return true;
        }
        else if (instruction is Call call)
        {
            if (register == X86Register.eax) return false;
            if (register == X86Register.ebx) return false;
            if (register == X86Register.ecx) return false;
            if (register == X86Register.edx) return false;
        }
        else if (instruction is Ret ret)
        {
            // return values placed on eax
            return register == X86Register.eax;
        }
        else if (instruction is Ret_Immediate ret_Immediate)
        {
            // return values placed on eax
            return register == X86Register.eax;
        }
        else if (instruction is Fstp_Offset fstp_Offset)
        {
            if (fstp_Offset.Destination.Register == register) return true;
        }
        else if (instruction is Fld_Offset fld_Offset)
        {
            if (fld_Offset.Source.Register == register) return true;
        }
        else if (instruction is Movss_Offset_Register movss_Offset_Register)
        {
            if (movss_Offset_Register.Destination.Register == register) return true;
        }
        else if (instruction is Movss_Register_Offset movss_Register_Offset)
        {
            if (movss_Register_Offset.Source.Register == register) return true;
        }
        if (instruction is Movss_Register_Register movss_Register_Register)
        {

        }
        else if (instruction is Comiss_Register_Offset comiss_Register_Offset)
        {
            if (comiss_Register_Offset.Operand2.Register == register) return true;
        }
        else if (instruction is Comiss_Register_Register comiss_Register_Register)
        {

        }
        else if (instruction is Ucomiss_Register_Register ucomiss_Register_Register)
        {

        }
        else if (instruction is Addss_Register_Offset addss_Register_Offset)
        {
            if (addss_Register_Offset.Source.Register == register) return true;
        }
        else if (instruction is Subss_Register_Offset subss_Register_Offset)
        {
            if (subss_Register_Offset.Source.Register == register) return true;
        }
        else if (instruction is Mulss_Register_Offset mulss_Register_Offset)
        {
            if (mulss_Register_Offset.Source.Register == register) return true;
        }
        else if (instruction is Divss_Register_Offset divss_Register_Offset)
        {
            if (divss_Register_Offset.Source.Register == register) return true;
        }
        else if (instruction is Cvtsi2ss_Register_Offset cvtsi2Ss_Register_Offset)
        {
            if (cvtsi2Ss_Register_Offset.Source.Register == register) return true;
        }
        else if (instruction is Cvtss2si_Register_Offset cvtss2Si_Register_Offset)
        {
            if (cvtss2Si_Register_Offset.Source.Register == register) return true;
        }
        else if (instruction is Push_SymbolOffset push_SymbolOffset)
        {
            if (register == X86Register.esp) return true;
        }
        else if (instruction is Lea_Register_SymbolOffset lea_Register_SymbolOffset)
        {
            if (lea_Register_SymbolOffset.Destination == register) return false;
        }
        else if (instruction is Mov_SymbolOffset_Register mov_SymbolOffset_Register)
        {
            if (mov_SymbolOffset_Register.Source == register) return true;
        }
        else if (instruction is Mov_SymbolOffset_Register__Byte mov_SymbolOffset_Register__Byte)
        {
            if (mov_SymbolOffset_Register__Byte.Source.ToFullRegister() == register) return true;
        }
        else if (instruction is Movsx_Register_SymbolOffset__Byte movsx_Register_SymbolOffset)
        {
            if (movsx_Register_SymbolOffset.Destination == register) return false;
        }
        else if (instruction is Mov_SymbolOffset_Immediate mov_SymbolOffset_Immediate)
        {

        }
        else if (instruction is Inc_Register inc_Register)
        {
            if (inc_Register.Destination == register) return true;
        }
        else if (instruction is Dec_Register dec_Register)
        {
            if (dec_Register.Destination == register) return true;
        }
        else if (instruction is Inc_Offset inc_Offset)
        {
            if (inc_Offset.Destination.Register == register) return true;
        }
        else if (instruction is Dec_Offset dec_Offset)
        {
            if (dec_Offset.Destination.Register == register) return true;
        }
        else if (instruction is Mov_SymbolOffset_Byte_Register__Byte mov_SymbolOffset_Byte_Register__Byte)
        {
            if (mov_SymbolOffset_Byte_Register__Byte.Source.ToFullRegister() == register) return true;
        }
        else if (instruction is Mov_RegisterOffset_Byte_Register__Byte mov_RegisterOffset_Byte_Register__Byte)
        {
            if (mov_RegisterOffset_Byte_Register__Byte.Source.ToFullRegister() == register) return true;
            if (mov_RegisterOffset_Byte_Register__Byte.Destination.Register == register) return true;
        }
        return null;
    }

    private bool? IsRegisterReferencedHelper(X86Instruction instruction, XmmRegister register)
    {
        if (instruction is Cdq cdq)
        {

        }
        else if (instruction is Push_Register push_Register)
        {

        }
        else if (instruction is Push_Offset push_Offset)
        {

        }
        else if (instruction is Push_Address push_Address)
        {
        }
        else if (instruction is Push_Immediate<int> push_Immediate)
        {
        }
        else if (instruction is Lea_Register_Offset lea_Register_Offset)
        {

        }
        else if (instruction is Mov_Register_Offset mov_Register_Offset)
        {

        }
        else if (instruction is Mov_Offset_Register mov_Offset_Register)
        {
        }
        else if (instruction is Mov_Offset_Immediate mov_Offset_Immediate)
        {
        }
        else if (instruction is Mov_Register_Register mov_Register_Register)
        {

        }
        else if (instruction is Mov_Register_Immediate mov_Register_Immediate)
        {
        }
        else if (instruction is Mov_Offset_Register__Byte mov_Offset_Register__Byte)
        {
        }
        else if (instruction is Movsx_Register_Offset movsx_Register_Offset)
        {
        }
        else if (instruction is Sub_Register_Immediate sub_Register_Immediate)
        {
        }
        else if (instruction is Sub_Register_Register sub_Register_Register)
        {
        }
        else if (instruction is Add_Register_Immediate add_Register_Immediate)
        {
        }
        else if (instruction is Add_Register_Register add_Register_Register)
        {
        }
        else if (instruction is And_Register_Register and_Register_Register)
        {
        }
        else if (instruction is Or_Register_Register or_Register_Register)
        {
        }
        else if (instruction is Xor_Register_Register xor_Register_Register)
        {
        }
        else if (instruction is Pop_Register pop_Register)
        {

        }
        else if (instruction is Neg_Offset neg_Offset)
        {
        }
        else if (instruction is Not_Offset not_Offset)
        {
        }
        else if (instruction is IDiv_Offset idiv_Offset)
        {
        }
        else if (instruction is IMul_Register_Register imul_Register_Register)
        {
        }
        else if (instruction is IMul_Register_Immediate imul_Register_Immediate)
        {
        }
        else if (instruction is Add_Register_Offset add_Register_Offset)
        {
        }
        else if (instruction is Test_Register_Register test_Register_Register)
        {
        }
        else if (instruction is Test_Register_Offset test_Register_Offset)
        {
        }
        else if (instruction is Cmp_Register_Register cmp_Register_Register)
        {
        }
        else if (instruction is Cmp_Register_Immediate cmp_Register_Immediate)
        {
        }
        else if (instruction is Cmp_Byte_Byte cmp_Byte_Byte)
        {
        }
        else if (instruction is Call call)
        {
            if (register == XmmRegister.xmm0) return false;
            if (register == XmmRegister.xmm1) return false;
        }
        else if (instruction is Ret ret)
        {
            // return values placed on xmm0
            return register == XmmRegister.xmm0;
        }
        else if (instruction is Ret_Immediate ret_Immediate)
        {
            // return values placed on xmm0
            return register == XmmRegister.xmm0;
        }
        else if (instruction is Fstp_Offset fstp_Offset)
        {
        }
        else if (instruction is Fld_Offset fld_Offset)
        {
        }
        else if (instruction is Movss_Offset_Register movss_Offset_Register)
        {
            if (movss_Offset_Register.Source == register) return true;
        }
        else if (instruction is Movss_Register_Offset movss_Register_Offset)
        {
            if (movss_Register_Offset.Destination == register) return false;
        }
        else if (instruction is Movss_Register_Register movss_Register_Register)
        {
            if (movss_Register_Register.Destination == register) return false;
            if (movss_Register_Register.Source == register) return true;
        }
        else if (instruction is Comiss_Register_Offset comiss_Register_Offset)
        {
            if (comiss_Register_Offset.Operand1 == register) return true;
        }
        else if (instruction is Comiss_Register_Register comiss_Register_Register)
        {
            if (comiss_Register_Register.Operand1 == register || comiss_Register_Register.Operand2 == register) return true;
        }
        else if (instruction is Ucomiss_Register_Register ucomiss_Register_Register)
        {
            if (ucomiss_Register_Register.Operand1 == register || ucomiss_Register_Register.Operand2 == register) return true;
        }
        else if (instruction is Addss_Register_Offset addss_Register_Offset)
        {
            if (addss_Register_Offset.Destination == register) return true;
        }
        else if (instruction is Subss_Register_Offset subss_Register_Offset)
        {
            if (subss_Register_Offset.Destination == register) return true;
        }
        else if (instruction is Mulss_Register_Offset mulss_Register_Offset)
        {
            if (mulss_Register_Offset.Destination == register) return true;
        }
        else if (instruction is Divss_Register_Offset divss_Register_Offset)
        {
            if (divss_Register_Offset.Destination == register) return true;
        }
        else if (instruction is Cvtsi2ss_Register_Offset cvtsi2Ss_Register_Offset)
        {
            if (cvtsi2Ss_Register_Offset.Destination == register) return false;
        }
        else if (instruction is Cvtss2si_Register_Offset cvtss2Si_Register_Offset)
        {
        }
        else if (instruction is Push_SymbolOffset push_SymbolOffset)
        {
        }
        else if (instruction is Lea_Register_SymbolOffset lea_Register_SymbolOffset)
        {
        }
        else if (instruction is Mov_SymbolOffset_Register mov_SymbolOffset_Register)
        {
        }
        else if (instruction is Mov_SymbolOffset_Register__Byte mov_SymbolOffset_Register__Byte)
        {
        }
        else if (instruction is Movsx_Register_SymbolOffset__Byte movsx_Register_SymbolOffset)
        {
        }
        else if (instruction is Mov_SymbolOffset_Immediate mov_SymbolOffset_Immediate)
        {

        }
        else if (instruction is Inc_Register inc_Register)
        {
        }
        else if (instruction is Dec_Register dec_Register)
        {
        }
        else if (instruction is Inc_Offset inc_Offset)
        {
        }
        else if (instruction is Dec_Offset dec_Offset)
        {
        }
        else if (instruction is Mov_SymbolOffset_Byte_Register__Byte mov_SymbolOffset_Byte_Register__Byte)
        {
        }
        else if (instruction is Mov_RegisterOffset_Byte_Register__Byte mov_RegisterOffset_Byte_Register__Byte)
        {

        }
        return null;
    }
    private bool DoesRegisterLoseIntegrity(X86Instruction instruction, X86Register register)
    {
        if (instruction is Cdq cdq)
        {
            if (register == X86Register.eax) return true;
            if (register == X86Register.edx) return true;
        }
        else if (instruction is Push_Register push_Register)
        {
            if (push_Register.Register == register) return true;
            if (register == X86Register.esp) return true;
        }
        else if (instruction is Push_Offset push_Offset)
        {
            if (push_Offset.Offset.Register == register) return true;
            if (register == X86Register.esp) return true;
        }
        else if (instruction is Push_Address push_Address)
        {
            if (register == X86Register.esp) return true;
        }
        else if (instruction is Push_Immediate<int> push_Immediate)
        {
            if (register == X86Register.esp) return true;
        }
        else if (instruction is Lea_Register_Offset lea_Register_Offset)
        {
            if (lea_Register_Offset.Destination == register) return true;
        }
        else if (instruction is Mov_Register_Offset mov_Register_Offset)
        {
            if (mov_Register_Offset.Destination == register) return true;
        }
        else if (instruction is Mov_Offset_Register mov_Offset_Register)
        {
        }
        else if (instruction is Mov_Offset_Immediate mov_Offset_Immediate)
        {
        }
        else if (instruction is Mov_Register_Register mov_Register_Register)
        {
            if (mov_Register_Register.Destination == register) return true;
        }
        else if (instruction is Mov_Register_Immediate mov_Register_Immediate)
        {
            if (mov_Register_Immediate.Destination == register) return true;
        }
        else if (instruction is Mov_Offset_Register__Byte mov_Offset_Register__Byte)
        {
        }
        else if (instruction is Movsx_Register_Offset movsx_Register_Offset)
        {
            if (movsx_Register_Offset.Destination == register) return true;
        }
        else if (instruction is Sub_Register_Immediate sub_Register_Immediate)
        {
            if (sub_Register_Immediate.Destination == register) return true;
        }
        else if (instruction is Sub_Register_Register sub_Register_Register)
        {
            if (sub_Register_Register.Destination == register) return true;
        }
        else if (instruction is Add_Register_Immediate add_Register_Immediate)
        {
            if (add_Register_Immediate.Destination == register) return true;
        }
        else if (instruction is Add_Register_Register add_Register_Register)
        {
            if (add_Register_Register.Destination == register) return true;
        }
        else if (instruction is And_Register_Register and_Register_Register)
        {
            if (and_Register_Register.Destination == register) return true;
        }
        else if (instruction is Or_Register_Register or_Register_Register)
        {
            if (or_Register_Register.Destination == register) return true;
        }
        else if (instruction is Xor_Register_Register xor_Register_Register)
        {
            if (xor_Register_Register.Destination == register) return true;
        }
        else if (instruction is Pop_Register pop_Register)
        {
            if (register == X86Register.esp) return true;
            if (pop_Register.Destination == register) return true;
        }
        else if (instruction is Neg_Offset neg_Offset)
        {
        }
        else if (instruction is Not_Offset not_Offset)
        {
        }
        else if (instruction is IDiv_Offset idiv_Offset)
        {
        }
        else if (instruction is IMul_Register_Register imul_Register_Register)
        {
            if (imul_Register_Register.Destination == register) return true;
        }
        else if (instruction is IMul_Register_Immediate imul_Register_Immediate)
        {
            if (imul_Register_Immediate.Destination == register) return true;
        }
        else if (instruction is Add_Register_Offset add_Register_Offset)
        {
            if (add_Register_Offset.Destination == register) return true;
        }
        else if (instruction is Test_Register_Register test_Register_Register)
        {
        }
        else if (instruction is Test_Register_Offset test_Register_Offset)
        {
        }
        else if (instruction is Cmp_Register_Register cmp_Register_Register)
        {
        }
        else if (instruction is Cmp_Register_Immediate cmp_Register_Immediate)
        {
        }
        else if (instruction is Cmp_Byte_Byte cmp_Byte_Byte)
        {
        }
        else if (instruction is Call call)
        {
            if (register == X86Register.eax) return true;
            if (register == X86Register.ebx) return true;
            if (register == X86Register.ecx) return true;
            if (register == X86Register.edx) return true;
        }
        else if (instruction is Ret ret)
        {
        }
        else if (instruction is Ret_Immediate ret_Immediate)
        {
        }
        else if (instruction is Fstp_Offset fstp_Offset)
        {
        }
        else if (instruction is Fld_Offset fld_Offset)
        {
        }
        else if (instruction is Movss_Offset_Register movss_Offset_Register)
        {
        }
        else if (instruction is Movss_Register_Offset movss_Register_Offset)
        {
        }
        else if (instruction is Comiss_Register_Offset comiss_Register_Offset)
        {
            if (comiss_Register_Offset.Operand2.Register == register) return true;
        }
        else if (instruction is Comiss_Register_Register comiss_Register_Register)
        {

        }
        else if (instruction is Ucomiss_Register_Register ucomiss_Register_Register)
        {

        }
        else if (instruction is Addss_Register_Offset addss_Register_Offset)
        {
        }
        else if (instruction is Subss_Register_Offset subss_Register_Offset)
        {
        }
        else if (instruction is Mulss_Register_Offset mulss_Register_Offset)
        {
        }
        else if (instruction is Divss_Register_Offset divss_Register_Offset)
        {
        }
        else if (instruction is Cvtsi2ss_Register_Offset cvtsi2Ss_Register_Offset)
        {
        }
        else if (instruction is Cvtss2si_Register_Offset cvtss2Si_Register_Offset)
        {
            if (cvtss2Si_Register_Offset.Destination == register) return true;
        }
        else if (instruction is Push_SymbolOffset push_SymbolOffset)
        {
            if (register == X86Register.esp) return true;
        }
        else if (instruction is Lea_Register_SymbolOffset lea_Register_SymbolOffset)
        {
            if (lea_Register_SymbolOffset.Destination == register) return true;
        }
        else if (instruction is Mov_SymbolOffset_Register mov_SymbolOffset_Register)
        {
        }
        else if (instruction is Mov_SymbolOffset_Register__Byte mov_SymbolOffset_Register__Byte)
        {
        }
        else if (instruction is Movsx_Register_SymbolOffset__Byte movsx_Register_SymbolOffset)
        {
            if (movsx_Register_SymbolOffset.Destination == register) return true;
        }
        else if (instruction is Mov_SymbolOffset_Immediate mov_SymbolOffset_Immediate)
        {

        }
        else if (instruction is Inc_Register inc_Register)
        {
            if (inc_Register.Destination == register) return true;
        }
        else if (instruction is Dec_Register dec_Register)
        {
            if (dec_Register.Destination == register) return true;
        }
        else if (instruction is Inc_Offset inc_Offset)
        {
            if (inc_Offset.Destination.Register == register) return true;
        }
        else if (instruction is Dec_Offset dec_Offset)
        {
            if (dec_Offset.Destination.Register == register) return true;
        }

        return false;
    }
}