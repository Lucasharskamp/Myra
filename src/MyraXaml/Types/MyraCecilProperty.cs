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
            _propertyType ??= new MyraCecilType(_property.PropertyType, _declaringType.Assembly);

        private IXamlType? _propertyType;

        public IXamlMethod? Setter =>
            GetSetter();

        private IXamlMethod? GetSetter()
        {
            if (_setter != null)
                return _setter;

            var propertyDefinition = _property.Resolve();

            if (_property.SetMethod != null)
            {
                _setter = new MyraCecilMethod(
                                _property.SetMethod,
                                propertyDefinition.PropertyType,
                                DeclaringType.Assembly);
                return _setter;
            }

            var propertyDefinitionResolved = propertyDefinition.PropertyType.Resolve();
            var newSetter = FindCollectionAdder(); 
            return _setter;
             

        }

        private IXamlMethod? FindCollectionAdder()
        {
            var propertyType = _property.PropertyType;

            var resolved = propertyType.Resolve();
            if (resolved == null)
                return null;

            // Search direct methods
            var add =
                resolved.Methods.FirstOrDefault(m =>
                    m.Name == "Add" && m.IsPublic && !m.IsStatic &&  m.Parameters.Count == 1);

            if (add != null)
            {
                return new MyraCecilMethod(
                    add,
                    propertyType,
                    DeclaringType.Assembly);
            }

            // Search interfaces (IList<T>, ICollection<T>, etc.)
            foreach (var iface in resolved.Interfaces)
            {
                var ifaceType = iface.InterfaceType;

                var ifaceResolved = ifaceType.Resolve();
                if (ifaceResolved == null)
                    continue;

                var ifaceAdd =
                    ifaceResolved.Methods.FirstOrDefault(m =>
                        m.Name == "Add" &&
                        m.IsPublic && !m.IsStatic && m.Parameters.Count == 1);

                if (ifaceAdd != null)
                {
                    var bound = ifaceType is GenericInstanceType generic
                        ? MyraCecilType.BindGenericMethod(ifaceAdd, generic)
                        : ifaceAdd;
                    
                    return new MyraCecilMethod(bound, ifaceType, DeclaringType.Assembly);
                }
            }

            return null;
        }

        private IXamlMethod? _setter;


        public IXamlMethod? Getter =>
            _property.GetMethod == null
                ? null
                : _getter ??= new MyraCecilMethod(
                    _property.GetMethod,
                    _property.Resolve().PropertyType,
                    DeclaringType.Assembly);
        private IXamlMethod? _getter;

        public IReadOnlyList<IXamlType> IndexerParameters =>
            _property.Parameters
                .Select(x => new MyraCecilType(x.ParameterType, _declaringType.Assembly))
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
                .Select(x => new MyraCecilCustomAttribute(x, _declaringType))
                .ToArray();
        }


        public bool Equals(IXamlProperty? other)
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
