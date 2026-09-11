using Myra.Xaml.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.Transform.Transformers;
using XamlX.TypeSystem; 

namespace Myra.Xaml.Transformers
{
    /// <summary>
    /// Type resolver for stylesheets; same as <see cref="TypeReferenceResolver"/>,
    /// except when the type is not found we'll presume it's a property and let <see cref="StylesheetPropertyTransformer"/> handle it.
    /// </summary>
    internal sealed class StylesheetTypeTransformer : IXamlAstTransformer
    {
        class TypeResolverCache
        {
            public Dictionary<(string?, string, bool), IXamlType?> CacheDictionary = [];
        }

        public static XamlAstClrTypeReference? AttemptResolveType(AstTransformationContext context,
            string? xmlns, string name, bool isMarkupExtension, IXamlLineInfo lineInfo)
        {
            var cache = context.GetOrCreateItem<TypeResolverCache>();
            var cacheKey = (xmlns, name, isMarkupExtension);
            if (cache.CacheDictionary.TryGetValue(cacheKey, out var type))
            {
                if (type == null)
                    return null;
                return new XamlAstClrTypeReference(lineInfo, type!, isMarkupExtension);
            }

            var res = ResolveTypeCore(context, xmlns, name, isMarkupExtension, lineInfo);
            cache.CacheDictionary[cacheKey] = res?.Type;
            return res;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2062", Justification = TrimmingMessages.TypePreservedElsewhere)]
        [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = TrimmingMessages.TypePreservedElsewhere)]
        static XamlAstClrTypeReference? ResolveTypeCore(AstTransformationContext context,
            string? xmlns, string name, bool isMarkupExtension, IXamlLineInfo lineInfo)
        {

            IXamlType? Attempt(Func<string, IXamlType?> cb, string xname)
            {
                if (isMarkupExtension)
                    return cb(xname + "Extension") ?? cb(xname);
                else
                    return cb(xname) ?? cb(xname + "Extension");
            }

            IXamlType? found = null;

            // Try to resolve from system
            if (xmlns == XamlNamespaces.Xaml2006)
                found = context.Configuration.TypeSystem.FindType("System." + name);


            if (found == null)
            {
                var resolvedNamespaces = NamespaceInfoHelper.TryResolve(context.Configuration, xmlns);
                if (resolvedNamespaces?.Count > 0)
                    found = Attempt(formedName =>
                    {
                        foreach (var resolvedNs in resolvedNamespaces)
                        {
                            var rname = resolvedNs.ClrNamespace + "." + formedName;
                            IXamlType? subRes = null;
                            if (resolvedNs.Assembly != null)
                                subRes = resolvedNs.Assembly.FindType(rname);
                            else if (resolvedNs.AssemblyName != null)
                                subRes = context.Configuration.TypeSystem.FindType(rname, resolvedNs.AssemblyName);
                            else
                            {
                                foreach (var assembly in context.Configuration.TypeSystem.Assemblies)
                                {
                                    subRes = assembly.FindType(rname);
                                    if (subRes != null)
                                        break;
                                }
                            }
                            if (subRes != null)
                                return subRes;
                        }

                        return null;
                    }, name);
            }

            if (found != null)
            {
                return new XamlAstClrTypeReference(lineInfo, found,
                    isMarkupExtension || found.Name.EndsWith("Extension"));
            }

            return null;
        }

        public IXamlAstNode Transform(AstTransformationContext context, IXamlAstNode node)
        {
            if (node is not XamlAstObjectNode ni)
            {
                return node;
            }

            if (ni.Type is XamlAstXmlTypeReference xmlref)
            {
                var resolved = AttemptResolveType(context,
                                            xmlref.XmlNamespace,
                                            xmlref.Name,
                                            xmlref.IsMarkupExtension,
                                            xmlref);
                if (resolved != null)
                {
                    ni.Type = resolved;
                }
            }

            if (ni.Type is not XamlAstClrTypeReference niClrType)
            {
                return node; // we do not work unless we have the overhead type.
            }

            var isGenericType = niClrType.Type.GenericTypeDefinition != null || niClrType.Type.BaseType?.GenericTypeDefinition != null;

            for (var c = ni.Children.Count - 1; c >= 0; c--)
            {
                var child = ni.Children[c]; 

                if (child is XamlAstXamlPropertyValueNode propertyNode && propertyNode.Property is XamlAstNamePropertyReference innerProp)
                { 
                    // first check if the "innerTypeRef" is actually a property of the parent; if not, it must be a type.
                    // And if it is a type, it means its part of a property of a instance of a property.
                    IXamlType declaringType;
                    IXamlProperty? parentProperty = null;
                    var innerProperty = niClrType.Type.GetAllProperties().FirstOrDefault(p => p.Name == innerProp.Name);
                    if (innerProperty == null)
                    {
                        var innerTypeRef = (XamlAstXmlTypeReference)innerProp.DeclaringType;
                        declaringType = GetLocalType(context, innerTypeRef)!;
                        if (declaringType == null)
                        {
                            throw new XamlLoadException(
                                $"Property '{innerProp.Name}' references type '{innerTypeRef.Name}' which is not a valid style type!",
                                child);
                        }

                        innerProperty = declaringType.GetTypeProperty(child, innerProp.Name); 
                        parentProperty = niClrType.Type.GetTypeProperty(child, innerTypeRef.Name);
                    }
                    else
                    {
                        declaringType = innerProperty.PropertyType;
                    }

                    var innerPropertyClr = new XamlAstClrProperty(node,
                                                            innerProperty.Name,
                                                            innerProperty.DeclaringType,
                                                            innerProperty.Getter,
                                                            [innerProperty.Setter],
                                                            innerProperty.CustomAttributes);
                    propertyNode.Property = innerPropertyClr;

                    // value
                    var value = propertyNode.Values.OfType<XamlAstTextNode>().FirstOrDefault();
                    if (value is not XamlAstTextNode valueNode)
                    {
                        continue;
                    }

                    valueNode.Type = new XamlAstClrTypeReference(child, context.Configuration.WellKnownTypes.String, false); 

                    if (!XamlTransformHelpers.TryGetCorrectlyTypedValue(context, value, innerProperty.PropertyType, out var rv))
                    {
                        throw new XamlLoadException("Type not found", child);
                    }

                    propertyNode.Values.Remove(value);
                    propertyNode.Values.Add(rv);

                    continue;
                }

                // for properties, convert it to a property assignment rather than dictionary "content" 
                if (!isGenericType && child is XamlAstObjectNode objectNode && objectNode.Type is XamlAstXmlTypeReference xmlReference)
                {
                    var innerProperty = niClrType.Type.GetAllProperties().First(p => p.Name == xmlReference.Name);
                      
                    if (innerProperty.Getter == null || !innerProperty.Getter.IsPublic || innerProperty.Getter.IsStatic)
                    {
                        throw new XamlLoadException(
                            $"Property '{innerProperty.PropertyType.Name}' of type '{niClrType.Type.FullName}' must have a public, non-static getter!",
                            node);
                    }

                    // unless it is a collection type, the property must have a public, non-static setter
                    if (innerProperty.PropertyType.GenericTypeDefinition != context.Configuration.WellKnownTypes.DictionaryOfT2 &&
                        (innerProperty.Setter == null || !innerProperty.Setter.IsPublic || innerProperty.Setter.IsStatic))
                    {
                        throw new XamlLoadException(
                            $"Property '{innerProperty.PropertyType.Name}' of type '{niClrType.Type.FullName}' must have a public, non-static setter!",
                            node);
                    }

                    objectNode.Type = new XamlAstClrTypeReference(child, innerProperty.PropertyType, false);
                    var replacement = ResolveChildren(context, objectNode, innerProperty);
                    ni.Children.Remove(child);
                    ni.Children.Insert(c, replacement);

                    continue;
                }

            }
             
            return node;
        }

        private IXamlAstNode ResolveChildren(AstTransformationContext context, XamlAstObjectNode objectNode, IXamlProperty? parentProperty)
        { 
            var declaringType = objectNode.Type.GetClrType();
            var isGenericType = declaringType.GenericTypeDefinition != null || declaringType.BaseType?.GenericTypeDefinition != null;

            for (int c = objectNode.Children.Count - 1; c >= 0; c--)
            { 
                IXamlAstNode child = objectNode.Children[c];
                if (child is XamlAstTextNode textNode && String.IsNullOrWhiteSpace(textNode.Text))
                {
                    objectNode.Children.RemoveAt(c);
                    continue;
                }


                if (child is XamlAstXamlPropertyValueNode propertyNode)
                {
                    // property 
                    var innerProp = (XamlAstNamePropertyReference)propertyNode.Property;

                    var innerProperty = declaringType.GetTypeProperty(child, innerProp.Name);
                    var innerPropertyClr = new XamlAstClrProperty(child,
                                                           innerProperty.Name,
                                                           innerProperty.DeclaringType,
                                                           innerProperty.Getter,
                                                           [innerProperty.Setter],
                                                           innerProperty.CustomAttributes);
                    propertyNode.Property = innerPropertyClr;

                    // value
                    var value = propertyNode.Values.FirstOrDefault();
                    if (value is not XamlAstTextNode valueNode)
                    {
                        throw new XamlLoadException($"Value of property '{propertyNode}' must have a valid text value!", child);
                    }

                    valueNode.Type = new XamlAstClrTypeReference(child, context.Configuration.WellKnownTypes.String, false);

                    if (!XamlTransformHelpers.TryGetCorrectlyTypedValue(context, value, innerProperty.PropertyType, out var rv))
                    {
                        throw new XamlLoadException("Type not found", child);
                    }

                    propertyNode.Values.Clear();
                    propertyNode.Values.Add(rv); 
                    continue;
                }

                if (child is XamlAstObjectNode childNode)
                {
                    if (isGenericType)
                    {
                        // ensure the child has a x:Key attribute.
                        var xKey = childNode.Children.OfType<XamlAstXmlDirective>().FirstOrDefault();
                        if (xKey == null)
                        {
                            childNode.Children.Insert(0, new XamlAstXmlDirective(childNode,
                                XamlNamespaces.Xaml2006,
                                "Key",
                                [new XamlConstantNode(childNode, context.Configuration.WellKnownTypes.String, "")]));

                        }
                        childNode.Type = new XamlAstClrTypeReference(child, declaringType.GetGenericTypeArgument()!, false);
                        ResolveChildren(context, childNode, null);
                        continue;
                    }

                    if (childNode.Type is XamlAstXmlTypeReference xmlType)
                    {
                        var innerProperty = declaringType.GetTypeProperty(child, xmlType.Name);
                        childNode.Type = new XamlAstClrTypeReference(child, innerProperty.PropertyType, false);
                        var result = ResolveChildren(context, childNode, innerProperty);
                        objectNode.Children.Remove(child);
                        objectNode.Children.Insert(c, result);
                    }
                }
            }
            if (parentProperty == null)
                return objectNode;

            var propReference = new XamlAstClrProperty(objectNode, parentProperty, context.Configuration);

            if (isGenericType)
            {
                return new XamlAstXamlPropertyValueNode(objectNode, propReference, objectNode.Children.OfType<IXamlAstValueNode>(), false);
            }
            else
            {
                return new XamlAstXamlPropertyValueNode(objectNode, propReference, objectNode, false);
            }
        }

        private IXamlType? GetLocalType(AstTransformationContext context, XamlAstXmlTypeReference typeReference)
        {
            var resolvedNamespaces = NamespaceInfoHelper.TryResolve(context.Configuration, typeReference.XmlNamespace);
            if (resolvedNamespaces == null)
                return null;

            foreach (var resolvedNs in resolvedNamespaces)
            {
                var rname = resolvedNs.ClrNamespace + "." + typeReference.Name;
                IXamlType? subRes = null;
                if (resolvedNs.Assembly != null)
                    subRes = resolvedNs.Assembly.FindType(rname);
                else if (resolvedNs.AssemblyName != null)
                    subRes = context.Configuration.TypeSystem.FindType(rname, resolvedNs.AssemblyName);
                else
                {
                    foreach (var assembly in context.Configuration.TypeSystem.Assemblies)
                    {
                        subRes = assembly.FindType(rname);
                        if (subRes != null)
                            break;
                    }
                }
                if (subRes != null)
                    return subRes;
            }

            return null;
        }
    }
}
