using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
  
    public sealed class MyraCecilConstructor : IXamlConstructor
    {
        private readonly MethodReference _constructor;

        private readonly MyraCecilType _declaringType;


        public object Id =>
            _constructor;


        public string Name =>
            _constructor.Name;


        public IXamlType DeclaringType =>
            _declaringType;


        public bool IsPublic =>
            _constructor.Resolve()?.IsPublic ?? false;


        public bool IsStatic =>
            _constructor.Resolve()?.IsStatic ?? false;


        public IReadOnlyList<IXamlType> Parameters =>
            _constructor.Parameters
                .Select(x => new MyraCecilType(x.ParameterType, _declaringType.Assembly))
                .ToArray();


        public MethodReference ConstructorReference =>
            _constructor;


        public MyraCecilConstructor(
            MethodReference constructor,
            MyraCecilType declaringType)
        {
            _constructor = constructor;
            _declaringType = declaringType;
        }


        public IXamlParameterInfo GetParameterInfo(
            int index)
        {
            return new MyraCecilParameterInfo(
                _constructor.Parameters[index],
                _declaringType);
        }


        public bool Equals(
            IXamlConstructor? other)
        {
            return other is MyraCecilConstructor ctor &&
                   ctor._constructor.FullName ==
                   _constructor.FullName;
        }


        public override bool Equals(object? obj)
        {
            return obj is IXamlConstructor other &&
                   Equals(other);
        }


        public override int GetHashCode()
        {
            return _constructor.FullName.GetHashCode();
        }
    }
}
