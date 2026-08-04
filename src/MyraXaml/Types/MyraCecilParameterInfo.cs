using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{ 
    public sealed class MyraCecilParameterInfo : IXamlParameterInfo
    {
        private readonly ParameterDefinition _parameter;

        private readonly IXamlType _declaringType;


        public IXamlType ParameterType =>
            new MyraCecilType(
                _parameter.ParameterType,
                _declaringType.Assembly);


        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
            _customAttributes ??= CreateCustomAttributes();

        private IReadOnlyList<IXamlCustomAttribute>? _customAttributes;


        public ParameterDefinition ParameterDefinition =>
            _parameter;


        public MyraCecilParameterInfo(
            ParameterDefinition parameter,
            IXamlType declaringType)
        {
            _parameter = parameter;
            _declaringType = declaringType;
        }


        private IReadOnlyList<IXamlCustomAttribute> CreateCustomAttributes()
        {
            if (!_parameter.HasCustomAttributes)
                return Array.Empty<IXamlCustomAttribute>();

            return _parameter.CustomAttributes
                .Select(x => new MyraCecilCustomAttribute(x, _declaringType))
                .ToArray();
        }
    }
}
