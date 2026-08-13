using Mono.Cecil;
using Myra.Xaml.Compiler;
using System.Collections.Generic;
using System.Linq;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{ 

    public sealed class MyraCecilConstructorBuilder
        : IXamlConstructorBuilder<IXamlILEmitter>
    {
        private readonly MethodDefinition _constructor;

        private readonly IXamlILEmitter _generator;
         
        public object Id => _constructor;
        public string Name => _constructor.Name;

        public IXamlType DeclaringType { get; }

        public bool IsPublic => _constructor.IsPublic;


        public bool IsStatic =>
            _constructor.IsStatic;


        public IReadOnlyList<IXamlType> Parameters =>
            _constructor.Parameters
                .Select(x => new MyraCecilType(x.ParameterType, DeclaringType.Assembly))
                .ToArray();


        public IXamlILEmitter Generator =>
            _generator;


        public MyraCecilConstructorBuilder(
            MethodDefinition constructor,
            IXamlType declaringType,
            IXamlILEmitter generator)
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
