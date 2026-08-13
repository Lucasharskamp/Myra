using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil; 
using XamlX.TypeSystem; 

namespace Myra.Xaml.Types
{ 

    public sealed class MyraCecilType : IXamlType
    {
        private readonly TypeReference _type;

        public object Id => _type;

        public string Name => _type.Name;

        public string? Namespace => _type.Namespace;

        public string FullName => _type.FullName;

        public bool IsPublic => _type.Resolve().IsPublic;

        public bool IsNestedPrivate => _type.Resolve().IsNestedPrivate;

        public IXamlAssembly? Assembly { get; }


        public IReadOnlyList<IXamlProperty> Properties =>
            _properties ??= _type.Resolve()
                            .Properties
                            .Select(p => new MyraCecilProperty(p, this))
                            .ToArray();

        private IReadOnlyList<IXamlProperty>? _properties;

        public IReadOnlyList<IXamlEventInfo> Events =>
            _events ??= _type.Resolve()
                        .Events
                        .Select(e => new MyraCecilEvent(e, this))
                        .ToArray(); 

        private IReadOnlyList<IXamlEventInfo>? _events;

        public IReadOnlyList<IXamlField> Fields =>
            _fields ??= _type.Resolve()
                            .Fields
                            .Select(x => new MyraCecilField(x, this))
                            .ToArray();

        private IReadOnlyList<IXamlField>? _fields;

        public IReadOnlyList<IXamlMethod> Methods =>
            _methods ??= GetMethods().ToArray();

        private IReadOnlyList<IXamlMethod>? _methods;

        private IEnumerable<IXamlMethod> GetMethods()
        {
            var resolved = _type.Resolve();
            if (resolved == null)
                yield break;

            // Methods declared on this type
            foreach (var method in resolved.Methods)
            {
                var reference = _type.Module.ImportReference(method);

                if (_type is GenericInstanceType generic)
                    reference = BindGenericMethod(reference, generic);

                yield return new MyraCecilMethod(
                    reference,
                    _type,
                    Assembly);
            }

            // Interface methods are part of the contract as well
            foreach (var iface in resolved.Interfaces)
            {
                var ifaceType = iface.InterfaceType.Resolve();
                if (ifaceType == null)
                    continue;

                foreach (var method in ifaceType.Methods)
                {
                    var reference = ifaceType.Module.ImportReference(method);

                    if (_type is GenericInstanceType generic)
                        reference = BindGenericMethod(reference, generic);

                    yield return new MyraCecilMethod(
                        reference,
                        _type,
                        Assembly);
                }
            }
        }

        public static MethodReference BindGenericMethod(MethodReference method, GenericInstanceType declaringType)
        {
            var bound = new MethodReference(
                method.Name,
                SubstituteType(method.ReturnType, declaringType),
                declaringType)
            {
                HasThis = method.HasThis,
                ExplicitThis = method.ExplicitThis,
                CallingConvention = method.CallingConvention
            };

            foreach (var parameter in method.Parameters)
            {
                bound.Parameters.Add(
                    new ParameterDefinition(
                        SubstituteType(parameter.ParameterType, declaringType)));
            }

            foreach (var gp in method.GenericParameters)
            {
                bound.GenericParameters.Add(
                    new GenericParameter(
                        gp.Name,
                        bound));
            }

            return bound;
        }

        private static TypeReference SubstituteType(TypeReference type, GenericInstanceType context)
        {
            if (type is GenericParameter gp)
            {
                // Generic parameters of ICollection<T>, IList<T>, etc.
                if (gp.Position < context.GenericArguments.Count)
                    return context.GenericArguments[gp.Position];

                return type;
            }

            if (type is GenericInstanceType generic)
            {
                var result =
                    new GenericInstanceType(
                        SubstituteType(
                            generic.ElementType,
                            context));

                foreach (var argument in generic.GenericArguments)
                {
                    result.GenericArguments.Add(
                        SubstituteType(argument, context));
                }

                return result;
            }

            if (type is ArrayType array)
            {
                return new ArrayType(
                    SubstituteType(array.ElementType, context),
                    array.Rank);
            }

            if (type is ByReferenceType byRef)
            {
                return new ByReferenceType(
                    SubstituteType(byRef.ElementType, context));
            }

            if (type is PointerType pointer)
            {
                return new PointerType(
                    SubstituteType(pointer.ElementType, context));
            }

            return type;
        }


        public IReadOnlyList<IXamlConstructor> Constructors =>
            _constructors ??= _type.Resolve()
                .Methods
                .Where(x => x.IsConstructor)
                .Select(x => new MyraCecilConstructor(x, this))
                .ToArray();

        private IReadOnlyList<IXamlConstructor>? _constructors;


        public IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
            _customAttributes ??= _type.Resolve()
                .CustomAttributes
                .Select(x => new MyraCecilCustomAttribute(x, this))
                .ToArray();

        private IReadOnlyList<IXamlCustomAttribute>? _customAttributes;

        public IReadOnlyList<IXamlType> GenericArguments =>
            _genericArguments ??= (_type is GenericInstanceType generic
                ? generic.GenericArguments
                    .Select(x => new MyraCecilType(x, Assembly))
                    .ToArray()
                : Array.Empty<IXamlType>());

        private IReadOnlyList<IXamlType>? _genericArguments;

        public IReadOnlyList<IXamlType> GenericParameters =>
            _genericParameters ??= _type.GenericParameters
                .Select(x => new MyraCecilType(x, Assembly))
                .ToArray();
        private IReadOnlyList<IXamlType>? _genericParameters;

        public bool IsArray => _type is ArrayType;

        public IXamlType? ArrayElementType =>
            _type is ArrayType array
                ? new MyraCecilType(array.ElementType, Assembly)
                : null;

        public IXamlType? BaseType =>
            _type.Resolve()?.BaseType is TypeReference baseType
                ? new MyraCecilType(baseType, Assembly)
                : null;
         
        public IXamlType? DeclaringType =>
            _type.DeclaringType is TypeReference declaring
                ? new MyraCecilType(declaring, Assembly)
                : null;


        public bool IsValueType => _type.Resolve()?.IsValueType ?? false;

        public bool IsEnum => _type.Resolve()?.IsEnum ?? false;

        public IReadOnlyList<IXamlType> Interfaces =>
            _interfaces ??= _type.Resolve()
                .Interfaces
                .Select(x => new MyraCecilType(ApplyGenericContext(x.InterfaceType), Assembly))
                .ToArray();

        private IReadOnlyList<IXamlType>? _interfaces;

        private TypeReference ApplyGenericContext(TypeReference type)
        {
            if (_type is not GenericInstanceType instance)
                return type;

            if (type is GenericParameter gp)
                return instance.GenericArguments[gp.Position];

            if (type is GenericInstanceType git)
            {
                var clone = new GenericInstanceType(
                    ApplyGenericContext(git.ElementType));

                foreach (var arg in git.GenericArguments)
                    clone.GenericArguments.Add(ApplyGenericContext(arg));

                return clone;
            }

            return type;
        }

        public bool IsInterface =>
            _type.Resolve()?.IsInterface ?? false;


        public bool IsFunctionPointer =>
            _type is FunctionPointerType;


        public MyraCecilType(
            TypeReference type,
            IXamlAssembly? assembly)
        {
            _type = type ?? throw new ArgumentNullException(nameof(type));
            Assembly = assembly;
        }

        public MyraCecilType(Type type, AssemblyDefinition assemblyDefinition, IXamlAssembly? assembly)
            : this(ResolveReflectionType(type, assemblyDefinition), assembly)
        {

        }

        private static TypeReference ResolveReflectionType(Type type, AssemblyDefinition assembly)
        { 
            var module = assembly.MainModule;

            var resolved = module.GetType(type.FullName);

            if (resolved == null)
                throw new InvalidOperationException($"Unable to resolve {type.FullName}");

            return resolved;
        }

        public TypeReference TypeReference => _type;
         
        public bool IsAssignableFrom(IXamlType type)
        {
            if (type is not MyraCecilType other)
                return false;

            var current = other._type.Resolve();

            var target = _type.Resolve();

            if (current == null || target == null)
                return false;

            while (current != null)
            {
                if (current.FullName == target.FullName)
                    return true;

                current = current.BaseType?.Resolve();
            }

            return false;
        }


        public IXamlType MakeGenericType(
            IReadOnlyList<IXamlType> typeArguments)
        {
            var generic = new GenericInstanceType(_type);

            foreach (var argument in typeArguments)
            {
                generic.GenericArguments.Add(
                    ((MyraCecilType)argument)._type);
            }

            return new MyraCecilType(
                generic,
                Assembly);
        }


        public IXamlType? GenericTypeDefinition =>
            _type is GenericInstanceType generic
                ? new MyraCecilType(
                    generic.ElementType,
                    Assembly)
                : null;


        public IXamlType MakeArrayType(int dimensions)
        {
            TypeReference result = _type;

            for (int i = 0; i < dimensions; i++)
                result = new ArrayType(result);

            return new MyraCecilType(
                result,
                Assembly);
        }


        public IXamlType GetEnumUnderlyingType()
        {
            var definition = _type.Resolve();

            if (definition == null || !definition.IsEnum)
                throw new InvalidOperationException(
                    "Type is not an enum.");

            var field = definition.Fields
                .First(x => x.Name == "value__");

            return new MyraCecilType(
                field.FieldType,
                Assembly);
        }


        public bool Equals(IXamlType? other)
        {
            return other is MyraCecilType mt &&
                   mt._type.FullName == _type.FullName;
        }


        public override bool Equals(object? obj)
        {
            return obj is IXamlType other &&
                   Equals(other);
        }


        public override int GetHashCode()
        {
            return _type.FullName.GetHashCode();
        }

        public override string ToString()
        {
            return FullName;
        }
    }
}
