using Mono.Cecil;
using Myra.Xaml.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
     
    public sealed class MyraCecilMethodBuilder : MyraCecilMethod, IXamlMethodBuilder<IXamlILEmitter>
    {
        private readonly MethodDefinition _definition;

        public MethodDefinition MethodDefinition => _definition;

        public IXamlILEmitter Generator { get; }

        public override object Id => _definition;
        public override string Name => _definition.Name;


        public override IXamlType DeclaringType { get; }


        public MyraCecilMethodBuilder(
            MethodDefinition definition,
            IXamlType declaringType,
            IXamlILEmitter generator) : base(definition, definition.Resolve().DeclaringType, null)
        {
            _definition = definition;
            DeclaringType = declaringType;
            Generator = generator;
        }


        public override bool IsPublic => _definition.IsPublic;

        public override bool IsPrivate => _definition.IsPrivate;

        public override bool IsFamily => _definition.IsFamily;

        public override bool IsStatic => _definition.IsStatic;

        public override bool ContainsGenericParameters => _definition.HasGenericParameters;

        public override bool IsGenericMethod => _definition.HasGenericParameters;

        public override bool IsGenericMethodDefinition => _definition.HasGenericParameters;


        public override IXamlType ReturnType =>
            new MyraCecilType(_definition.ReturnType, DeclaringType.Assembly);


        public override IReadOnlyList<IXamlType> Parameters =>
            _parameters ??= _definition.Parameters
                .Select(p => new MyraCecilType(p.ParameterType, DeclaringType.Assembly))
                .ToArray();

        private IReadOnlyList<IXamlType>? _parameters;

        public override IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
            _customAttributes ??= _definition.CustomAttributes
                .Select(c => new MyraCecilCustomAttribute(c, (MyraCecilType)DeclaringType))
                .ToArray();

        private IReadOnlyList<IXamlCustomAttribute>? _customAttributes;


        public override IXamlParameterInfo GetParameterInfo(int index)
        {
            throw new NotSupportedException("Cecil parameter metadata wrapper required.");
        }


        public override IXamlMethod MakeGenericMethod(IReadOnlyList<IXamlType> typeArguments)
        {
            throw new NotSupportedException();
        }


        public override IReadOnlyList<IXamlType> GenericParameters =>
            Array.Empty<IXamlType>();


        public override IReadOnlyList<IXamlType> GenericArguments =>
            Array.Empty<IXamlType>();


        public override bool Equals(IXamlMethod? other)
        {
            return other is MyraCecilMethodBuilder mb &&
                   mb._definition == _definition;
        }


        public override int GetHashCode() =>
            _definition.GetHashCode();
    }
}
