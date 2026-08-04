using System.Collections.Generic;
using Mono.Cecil;
using System.Linq;
using XamlX.TypeSystem;
using Myra.Xaml.Compiler;

namespace Myra.Xaml.Types
{ 

    public sealed class MyraCecilConstructorBuilder
        : IXamlConstructorBuilder<MyraCecilILEmitter>
    {
        private readonly MethodDefinition _constructor;

        private readonly MyraCecilILEmitter _generator;


        public object Id =>
            _constructor;


        public string Name =>
            _constructor.Name;


        public IXamlType DeclaringType { get; }


        public bool IsPublic =>
            _constructor.IsPublic;


        public bool IsStatic =>
            _constructor.IsStatic;


        public IReadOnlyList<IXamlType> Parameters =>
            _constructor.Parameters
                .Select(x => new MyraCecilType(x.ParameterType, DeclaringType.Assembly))
                .ToArray();


        public MyraCecilILEmitter Generator =>
            _generator;


        public MyraCecilConstructorBuilder(
            MethodDefinition constructor,
            IXamlType declaringType,
            MyraCecilILEmitter generator)
        {
            _constructor = constructor;
            DeclaringType = declaringType;
            _generator = generator;
        }


        public IXamlParameterInfo GetParameterInfo(int index)
        {
            return new MyraCecilParameterInfo(
                _constructor.Parameters[index],
                (MyraCecilType)DeclaringType);
        }


        public bool Equals(IXamlConstructor? other)
        {
            return other is MyraCecilConstructorBuilder cb &&
                   cb._constructor == _constructor;
        }


        public override bool Equals(object? obj)
        {
            return obj is IXamlConstructor other &&
                   Equals(other);
        }


        public override int GetHashCode()
        {
            return _constructor.GetHashCode();
        }


        public MethodDefinition ConstructorDefinition =>
            _constructor;
    }
}
