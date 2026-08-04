using System; 
using System.Collections.Generic;
using Mono.Cecil;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{ 
    public sealed class MyraCecilField : IXamlField
    {
        private readonly FieldDefinition _field;

        private readonly MyraCecilType _declaringType;


        public object Id =>
            _field;


        public string Name =>
            _field.Name;


        public IXamlType DeclaringType =>
            _declaringType;


        public IXamlType FieldType =>
            new MyraCecilType(
                _field.FieldType,
                _declaringType.Assembly);


        public bool IsPublic =>
            _field.IsPublic;


        public bool IsStatic =>
            _field.IsStatic;


        public bool IsLiteral =>
            _field.IsLiteral;


        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
            _customAttributes ??= CreateCustomAttributes();

        private IReadOnlyList<IXamlCustomAttribute>? _customAttributes;


        public FieldDefinition FieldDefinition =>
            _field;


        public MyraCecilField(
            FieldDefinition field,
            MyraCecilType declaringType)
        {
            _field = field;
            _declaringType = declaringType;
        }


        public object GetLiteralValue()
        {
            if (!IsLiteral)
            {
                throw new InvalidOperationException(
                    $"Field '{Name}' is not a literal field.");
            }

            return _field.Constant!;
        }


        private IReadOnlyList<IXamlCustomAttribute> CreateCustomAttributes()
        {
            if (!_field.HasCustomAttributes)
                return Array.Empty<IXamlCustomAttribute>();

            var result = new List<IXamlCustomAttribute>();

            foreach (var attribute in _field.CustomAttributes)
            {
                result.Add(
                    new MyraCecilCustomAttribute(
                        attribute,
                        _declaringType));
            }

            return result;
        }


        public bool Equals(IXamlField? other)
        {
            return other is MyraCecilField field &&
                   field._field == _field;
        }


        public override bool Equals(object? obj)
        {
            return obj is IXamlField other &&
                   Equals(other);
        }


        public override int GetHashCode()
        {
            return _field.GetHashCode();
        }
    }
}
