using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{ 
    public sealed class MyraCecilCustomAttribute : IXamlCustomAttribute
    {
        private readonly CustomAttribute _attribute;
        private readonly IXamlType _declaringType;


        public IXamlType Type => new MyraCecilType(_attribute.AttributeType, _declaringType.Assembly);


        public List<object?> Parameters => _parameters ??= CreateParameters();

        private List<object?>? _parameters;

        public Dictionary<string, object?> Properties => _properties ??= CreateProperties();

        private Dictionary<string, object?>? _properties;

        public CustomAttribute Attribute => _attribute;

        public MyraCecilCustomAttribute(CustomAttribute attribute, IXamlType declaringType)
        {
            _attribute = attribute;
            _declaringType = declaringType;
        }


        private List<object?> CreateParameters()
        {
            if (!_attribute.HasConstructorArguments)
                return new List<object?>();


            return _attribute.ConstructorArguments
                .Select(ConvertArgument)
                .ToList();
        }


        private Dictionary<string, object?> CreateProperties()
        {
            var result = new Dictionary<string, object?>();

            if (_attribute.HasProperties)
            {
                foreach (var property in _attribute.Properties)
                {
                    result[property.Name] =
                        ConvertArgument(property.Argument);
                }
            }


            if (_attribute.HasFields)
            {
                foreach (var field in _attribute.Fields)
                {
                    result[field.Name] =
                        ConvertArgument(field.Argument);
                }
            }


            return result;
        }


        private object? ConvertArgument(
            CustomAttributeArgument argument)
        {
            if (argument.Value is CustomAttributeArgument[] array)
            {
                return array
                    .Select(ConvertArgument)
                    .ToArray();
            }


            return argument.Value;
        }

        public bool Equals(IXamlCustomAttribute? other)
        {
            return other is MyraCecilCustomAttribute attribute &&
                   attribute._attribute.AttributeType.FullName ==
                   _attribute.AttributeType.FullName;
        }

        public override bool Equals(object? obj)
        {
            return obj is IXamlCustomAttribute other && Equals(other);
        }


        public override int GetHashCode()
        {
            return _attribute.AttributeType.FullName.GetHashCode();
        }
    }
}
