using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{ 

    public sealed class MyraCecilProperty : IXamlProperty
    {
        private readonly PropertyDefinition _property;
        private readonly MyraCecilType _declaringType;


        public object Id => _property;

        public string Name => _property.Name;


        public IXamlType DeclaringType => _declaringType;


        public IXamlType PropertyType =>
            new MyraCecilType(
                _property.PropertyType,
                _declaringType.Assembly);
         
        public IXamlMethod? Setter =>
            _property.SetMethod == null
                ? null
                : _setter ??= new MyraCecilMethod(
                    _property.SetMethod,
                    PropertyType);

        private IXamlMethod? _setter;


        public IXamlMethod? Getter =>
            _property.GetMethod == null
                ? null
                : _getter ??= new MyraCecilMethod(
                    _property.GetMethod,
                    PropertyType);
        private IXamlMethod? _getter;

        public IReadOnlyList<IXamlType> IndexerParameters =>
            _property.Parameters
                .Select(x =>
                    (IXamlType)new MyraCecilType(
                        x.ParameterType,
                        _declaringType.Assembly))
                .ToArray();


        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
            _customAttributes ??= CreateCustomAttributes();

        private IReadOnlyList<IXamlCustomAttribute>? _customAttributes;

        public MyraCecilProperty(
            PropertyDefinition property,
            MyraCecilType declaringType)
        {
            _property = property;
            _declaringType = declaringType;
        }


        private IReadOnlyList<IXamlCustomAttribute> CreateCustomAttributes()
        {
            if (!_property.HasCustomAttributes)
                return Array.Empty<IXamlCustomAttribute>();

            return _property.CustomAttributes
                .Select(x =>
                    (IXamlCustomAttribute)
                        new MyraCecilCustomAttribute(
                            x,
                            _declaringType))
                .ToArray();
        }


        public bool Equals(
            IXamlProperty? other)
        {
            return other is MyraCecilProperty property &&
                   property._property.FullName ==
                   _property.FullName;
        }


        public override bool Equals(object? obj)
        {
            return obj is IXamlProperty other &&
                   Equals(other);
        }


        public override int GetHashCode()
        {
            return _property.FullName.GetHashCode();
        }
    }
}
