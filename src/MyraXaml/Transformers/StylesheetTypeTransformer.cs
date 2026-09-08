using Myra.Xaml.Helpers;
using Myra.Xaml.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;
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
                throw new InvalidOperationException("This should never happen");
            }

            var innerType = niClrType.Type.GenericTypeDefinition;
            bool hasGenericType = innerType != null;
            if (hasGenericType)
            {
                innerType = niClrType.Type.GenericArguments.Last();
            }
            else
            {
                innerType = niClrType.Type;
            }

            string foundId = "";
            for (var c = ni.Children.Count - 1; c >= 0; c--)
            {
                var child = ni.Children[c];
                if (child is XamlAstXamlPropertyValueNode propertyNode)
                { 
                    // property 
                    var innerProp = (XamlAstNamePropertyReference)propertyNode.Property;
 
                    // first check if the "innerTypeRef" is actually a property of the parent; if not, it must be a type.
                    // And if it is a type, it means its part of a property of a instance of a property.
                    IXamlType declaringType;
                    IXamlProperty? parentProperty = null;
                    var innerProperty = innerType.GetAllProperties().FirstOrDefault(p => p.Name == innerProp.Name);
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

                        innerProperty = declaringType.GetAllProperties().FirstOrDefault(p => p.Name == innerProp.Name);
                        if (innerProperty == null)
                        {
                            throw new XamlLoadException($"Element '{innerTypeRef.Name}' is neither a property nor a type!", child);
                        }

                        parentProperty = innerType.GetAllProperties().FirstOrDefault(p => p.Name == innerTypeRef.Name);
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
                    var value = propertyNode.Values.FirstOrDefault();
                    if (value is not XamlAstTextNode valueNode)
                    {
                        throw new XamlLoadException($"Value of property '{propertyNode}' must have a valid text value!", child);
                    } 
                      
                    if (valueNode.Type is not XamlAstClrProperty)
                    {
                        valueNode.Type = new XamlAstClrTypeReference(valueNode, innerProperty.Getter!.ReturnType, false);
                    }

                    if (innerProperty.Name == "Id")
                    {
                        foundId = valueNode.Text;
                        ni.Children.RemoveAt(c);
                    } 
                }
                else if (child is XamlAstObjectNode objectNode && objectNode.Type is XamlAstXmlTypeReference xmlTypeRef)
                {
                    var innerProperty = innerType.GetAllProperties().FirstOrDefault(p => p.Name == xmlTypeRef.Name);
                    if (innerProperty == null)
                    {
                        continue;
                    }
                    if (innerProperty.Getter == null || !innerProperty.Getter.IsPublic || innerProperty.Getter.IsStatic)
                    {
                        throw new XamlLoadException(
                            $"Property '{xmlTypeRef.Name}' of type '{innerType.FullName}' must have a public, non-static getter!",
                            node);
                    }

                    // unless it is a collection type, the property must have a public, non-static setter
                    if (innerProperty.PropertyType.GenericTypeDefinition != context.Configuration.WellKnownTypes.DictionaryOfT2 &&
                        (innerProperty.Setter == null || !innerProperty.Setter.IsPublic || innerProperty.Setter.IsStatic))
                    {
                        throw new XamlLoadException(
                            $"Property '{xmlTypeRef.Name}' of type '{innerType.FullName}' must have a public, non-static setter!",
                            node);
                    }

                    ni.Children.RemoveAt(c); 
                    ni.Children.Insert(c, new XamlAstXamlPropertyValueNode(child,
                        new XamlAstClrProperty(node, innerProperty, context.Configuration),
                        objectNode.Children.OfType<XamlAstObjectNode>().Select((p) =>
                        {
                            if (p is XamlAstObjectNode objectNode)
                            {
                                return new XamlAstObjectNode(objectNode, new XamlAstClrTypeReference(objectNode, innerProperty.PropertyType, false))
                                {
                                    Arguments = objectNode.Arguments,
                                    Children = objectNode.Children
                                };
                            }

                            return p;
                        }),
                        false)); 
                }
            }

            // handle ID, given we don't use x:Key and need to create that manually.
            // if Id attribute is present, use that. If not, use empty string.
            if (hasGenericType &&
                TypesContainer.WidgetStyle.IsAssignableFrom(innerType)
                || innerType == TypesContainer.StylesheetFontsCollection)
            {
                ni.Children.Insert(0, new XamlAstXmlDirective(ni,
                        XamlNamespaces.Xaml2006,
                        "Key",
                        [new XamlConstantNode(ni, context.Configuration.WellKnownTypes.String, foundId)]));
            }

            return node;
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
