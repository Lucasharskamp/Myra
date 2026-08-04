using Mono.Cecil;
using Mono.Cecil.Cil;
using Myra.Xaml.Types;
using System.Collections.Generic;
using System.Linq;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Myra.Xaml.Compiler
{ 
    public sealed class MyraCecilILEmitter  
    {
        private readonly ILProcessor _processor;

        public IXamlTypeSystem TypeSystem { get; }

        private readonly Stack<MyraCecilLocal> _availableLocals = [];

        private readonly List<MyraCecilLocal> _activeLocals = [];


        public MyraCecilILEmitter(ILProcessor processor, IXamlTypeSystem typeSystem)
        {
            _processor = processor;
            TypeSystem = typeSystem;
        }


        public MyraCecilILEmitter Emit(OpCode code)
        {
            _processor.Append(Instruction.Create(code));
            return this;
        }


        public MyraCecilILEmitter Emit(
            OpCode code,
            IXamlField field)
        {
            _processor.Append(
                Instruction.Create(code, ((MyraCecilType)field.DeclaringType).TypeReference));

            return this;
        }


        public MyraCecilILEmitter Emit(OpCode code, IXamlMethod method)
        {
            var reference =
                ((MyraCecilMethodBuilder)method)
                    .MethodDefinition;

            _processor.Append(
                Instruction.Create(code, reference));

            return this;
        }


        public MyraCecilILEmitter Emit(OpCode code, IXamlConstructor ctor)
        {
            var reference =
                ((MyraCecilConstructorBuilder)ctor)
                    .ConstructorDefinition;

            _processor.Append(
                Instruction.Create(code, reference));

            return this;
        }


        public MyraCecilILEmitter Emit(
            OpCode code,
            IXamlType type)
        {
            var reference =
                ((MyraCecilType)type)
                    .TypeReference;

            _processor.Append(
                Instruction.Create(code, reference));

            return this;
        }


        public MyraCecilILEmitter Emit(
            OpCode code,
            string arg)
        {
            _processor.Append(
                Instruction.Create(code, arg));

            return this;
        }


        public MyraCecilILEmitter Emit(
            OpCode code,
            int arg)
        {
            _processor.Append(
                Instruction.Create(code, arg));

            return this;
        }


        public MyraCecilILEmitter Emit(OpCode code, long arg)
        {
            _processor.Append(
                Instruction.Create(code, arg));

            return this;
        }


        public MyraCecilILEmitter Emit(OpCode code, sbyte arg)
        {
            _processor.Append(
                Instruction.Create(code, arg));

            return this;
        }


        public MyraCecilILEmitter Emit(OpCode code, byte arg)
        {
            _processor.Append(Instruction.Create(code, arg));

            return this;
        }


        public MyraCecilILEmitter Emit(OpCode code, float arg)
        {
            _processor.Append(Instruction.Create(code, arg));
            return this;
        }

        public MyraCecilILEmitter Emit(OpCode code, double arg)
        {
            _processor.Append(Instruction.Create(code, arg));
            return this;
        }


        public MyraCecilILEmitter Emit(OpCode code, IXamlLabel label)
        {
            var cecilLabel = ((MyraCecilLabel)label).Label;

            _processor.Append(
                Instruction.Create(code, cecilLabel));

            return this;
        }


        public MyraCecilILEmitter Emit(OpCode code, IXamlLocal local)
        {
            var variable =
                ((MyraCecilLocal)local)
                    .Variable;

            _processor.Append(
                Instruction.Create(code, variable));

            return this;
        }

        public IXamlLocal GetLocal(IXamlType type)
        {
            // Reuse compatible locals
            foreach (var local in _availableLocals)
            {
                if (local.Type.Equals(type))
                {
                    _availableLocals.Pop();
                    _activeLocals.Add(local);
                    return local;
                }
            }

            var variable = new VariableDefinition(((MyraCecilType)type).TypeReference);
            var result = new MyraCecilLocal(variable, type);

            _activeLocals.Add(result);

            return result;
        }

        public void ReleaseLocal(IXamlLocal local)
        {
            if (local is not MyraCecilLocal cecilLocal)
                return; 

            if (_activeLocals.Remove(cecilLocal))
            {
                _availableLocals.Push(cecilLocal);
            }
        }
    }
}
