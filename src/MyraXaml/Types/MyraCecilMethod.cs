using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using XamlX.TypeSystem;


namespace Myra.Xaml.Types
{ 

    public class MyraCecilMethod : IXamlMethod
    {
        private readonly MethodReference _method; 
        private readonly IXamlType _declaringType;


        public virtual object Id => _method;
        public virtual string Name => _method.Name;


        public virtual IXamlType DeclaringType =>
            _declaringType;


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
                _method.ReturnType,
                _declaringType.Assembly);


        public virtual IReadOnlyList<IXamlType> Parameters =>
            _method.Parameters
                .Select(x => new MyraCecilType(x.ParameterType, _declaringType.Assembly))
                .ToArray();


        public virtual IReadOnlyList<IXamlCustomAttribute> CustomAttributes =>
            CreateCustomAttributes();


        public virtual IReadOnlyList<IXamlType> GenericParameters =>
            _method.GenericParameters
                .Select(x => new MyraCecilType(x, _declaringType.Assembly))
                .ToArray();


        public virtual IReadOnlyList<IXamlType> GenericArguments =>
            _method is GenericInstanceMethod generic
                ? generic.GenericArguments
                    .Select(x => new MyraCecilType(x,_declaringType.Assembly))
                    .ToArray()
                : Array.Empty<IXamlType>();


        public MethodReference MethodReference =>
            _method;


        public MyraCecilMethod(MethodReference method, IXamlType declaringType)
        {
            _method = method;
            _declaringType = declaringType;
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

            return new MyraCecilMethod(generic, _declaringType);
        }


        public virtual IXamlParameterInfo GetParameterInfo(int index)
        {
            return new MyraCecilParameterInfo(_method.Parameters[index], _declaringType);
        }


        private IReadOnlyList<IXamlCustomAttribute> CreateCustomAttributes()
        {
            var definition = _method.Resolve();

            if (definition == null ||
                !definition.HasCustomAttributes)
            {
                return Array.Empty<IXamlCustomAttribute>();
            }


            return definition.CustomAttributes
                .Select(x =>
                    (IXamlCustomAttribute)
                        new MyraCecilCustomAttribute(
                            x,
                            _declaringType))
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
