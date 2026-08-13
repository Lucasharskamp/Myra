using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using XamlX.TypeSystem;


namespace Myra.Xaml.Types
{ 

    public class MyraCecilMethod : IXamlMethod
    {
        private readonly IXamlAssembly? _assembly;
        private readonly MethodReference _method; 
        private readonly TypeReference _declaringTypeReference;
        private readonly Dictionary<GenericParameter, TypeReference> _genericMap;

        public virtual object Id => _method;
        public virtual string Name => _method.Name;


        public virtual IXamlType DeclaringType 
            => _declaringType ??= new MyraCecilType(_declaringTypeReference, _assembly);

        private IXamlType? _declaringType;

        public virtual bool IsPublic =>
            _method.Resolve()?.IsPublic ?? false;


        public virtual bool IsPrivate =>
            _method.Resolve()?.IsPrivate ?? false;


        public virtual bool IsFamily =>
            _method.Resolve()?.IsFamily ?? false;


        public virtual bool IsStatic =>
            _method.Resolve()?.IsStatic ?? false;


        public virtual bool ContainsGenericParameters =>
            _method.ContainsGenericParameter;


        public virtual bool IsGenericMethod =>
            _method.HasGenericParameters ||
            _method is GenericInstanceMethod;


        public virtual bool IsGenericMethodDefinition =>
            _method.Resolve()?.HasGenericParameters ?? false;


        public virtual IXamlType ReturnType =>
            new MyraCecilType(
                ResolveType(_method.ReturnType),
                _assembly);


        public virtual IReadOnlyList<IXamlType> Parameters =>
            _method.Parameters
                .Select(x => new MyraCecilType(ResolveType(x.ParameterType), _assembly))
                .ToArray();

        private TypeReference ResolveType(TypeReference type)
        {
            if (type is GenericParameter gp && _genericMap.TryGetValue(gp, out var actual))
            {
                return actual;
            }

            return type;
        }

         
        public virtual IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
            CreateCustomAttributes();


        public virtual IReadOnlyList<IXamlType> GenericParameters =>
            _method.GenericParameters
                .Select(x => new MyraCecilType(ResolveType(x), _assembly))
                .ToArray();


        public virtual IReadOnlyList<IXamlType> GenericArguments =>
            _method is GenericInstanceMethod generic
                ? generic.GenericArguments
                    .Select(x => new MyraCecilType(ResolveType(x), _assembly))
                    .ToArray()
                : Array.Empty<IXamlType>();


        public MethodReference MethodReference =>
            _method;


        public MyraCecilMethod(MethodReference method, TypeReference declaringType, IXamlAssembly? assembly)
        {
            _assembly = assembly;
            _method = method ?? throw new ArgumentNullException(nameof(method));
            _declaringTypeReference = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
            _genericMap = BuildMap(declaringType);
        }

        public static Dictionary<GenericParameter, TypeReference> BuildMap(TypeReference type)
        {
            var map = new Dictionary<GenericParameter, TypeReference>();

            if (type is GenericInstanceType git)
            {
                var def = git.ElementType.Resolve();

                for (int i = 0; i < def.GenericParameters.Count; i++)
                    map[def.GenericParameters[i]] = git.GenericArguments[i];
            }

            return map;
        }


        public virtual IXamlMethod MakeGenericMethod(IReadOnlyList<IXamlType> typeArguments)
        {
            if (!_method.HasGenericParameters)
            {
                throw new InvalidOperationException($"Method '{Name}' is not generic.");
            }

            var generic = new GenericInstanceMethod(_method);

            foreach (var argument in typeArguments)
            {
                generic.GenericArguments.Add(((MyraCecilType)argument).TypeReference);
            }

            return new MyraCecilMethod(generic, _declaringTypeReference, _assembly);
        }


        public virtual IXamlParameterInfo GetParameterInfo(int index)
        {
            return new MyraCecilParameterInfo(_method.Parameters[index], DeclaringType);
        }


        private IXamlCustomAttribute[] CreateCustomAttributes()
        {
            var definition = _method.Resolve();

            if (definition == null || !definition.HasCustomAttributes)
            {
                return Array.Empty<IXamlCustomAttribute>();
            }


            return definition.CustomAttributes
                .Select(x => new MyraCecilCustomAttribute(x, DeclaringType))
                .ToArray();
        }


        public virtual bool Equals(IXamlMethod? other)
        {
            return other is MyraCecilMethod method &&
                   method._method.FullName == _method.FullName;
        }


        public override bool Equals(object? obj)
        {
            return obj is IXamlMethod other &&
                   Equals(other);
        }


        public override int GetHashCode()
        {
            return _method.FullName.GetHashCode();
        }

        public override string ToString()
        {
            return _method.FullName;
        }
    }
}
