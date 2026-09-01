using Mono.Cecil;
using Mono.Collections.Generic;
using Myra.Xaml.Compiler;
using Myra.Xaml.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using XamlX;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Helpers
{
    public static class ConverterHelper
    {
        public static bool MyraValueConverters(AstTransformationContext context, IXamlAstValueNode node, IReadOnlyList<IXamlCustomAttribute>? customAttributes, IXamlType type, [NotNullWhen(true)] out IXamlAstValueNode? result)
        {
            result = null!;

            if (type == TypesContainer.Color)
            {
                return TryAssignColor(context, node, type, out result);
            }

            if (type == TypesContainer.IBrush)
            {
                if (TryAssignColor(context, node, type, out result))
                {
                    result = new XamlAstNewClrObjectNode(node,
                        new XamlAstClrTypeReference(node, TypesContainer.SolidBrush, false),
                        TypesContainer.SolidBrush.GetConstructor([TypesContainer.Color]),
                        [result]);
                    return true;
                }

                return TryAssignImage(context, node, out result);
            }

            if (type == TypesContainer.IImage)
            {
                return TryAssignImage(context, node, out result);
            }

            // handle proportions
            if (type == TypesContainer.Proportion)
            {
                if (!GetText(node, out var text))
                    return false;

                // check if value is one of the static readonly properties.
                var field = type.GetAllFields().FirstOrDefault(t => t.Name == text);
                if (field != null && field.IsStatic)
                {
                    result = new XamlStaticFieldNode(node, field);
                    return true;
                }

                return false;
            } 

            if (type == TypesContainer.SolidBrush)
            {
                return TryAssignSolidBrush(context, node, type, out result);
            }

            // handle SpriteFontBase
            if (type == TypesContainer.SpriteFontBase)
            {
                return AssignFileResource(context, node, "GetFont", ref result);
            }

            // handle Texture2D
            if (type == TypesContainer.Texture2D)
            {
                return AssignFileResource(context, node, "GetTexture", ref result);
            }

            // handle SpriteFontBase
            if (type == TypesContainer.TextureRegionAtlas)
            {
                if (!GetText(node, out var text))
                    return false;

                var loadMethod = TypesContainer.IFileResolver.GetMethod(m => m.IsStatic && m.Name == "GetAtlas");
                result = new XamlStaticOrTargetedReturnMethodCallNode(node, loadMethod,
                    [
                        new XamlStaticOrTargetedReturnMethodCallNode(node,
                                    TypesContainer.MyraEnvironment.GetAllProperties().First(p => p.Name == "Resolver").Getter!,
                                    null),
                        new XamlStaticOrTargetedReturnMethodCallNode(node,
                                    TypesContainer.MyraEnvironment.GetAllProperties().First(p => p.Name == "GraphicsDevice").Getter!,
                                    null),
                        new XamlConstantNode(node, context.Configuration.WellKnownTypes.String, text)
                    ]);
                return true;
            }

            // handle thickness  
            if (type == TypesContainer.Thickness)
            {
                if (!GetText(node, out var text))
                    return false;

                // check if value is one of the static readonly properties.
                var field = type.GetAllFields().FirstOrDefault(t => t.Name == text);
                if (field != null && field.IsStatic)
                {
                    result = new XamlStaticFieldNode(node, field);
                    return true;
                }

                // conversion time!
                var values = text.Split(',');
                var arguments = new List<int>();
                foreach (var value in values)
                {
                    if (!Int32.TryParse(value.Trim(), out int r))
                    {
                        return false;
                    }
                    arguments.Add(r);
                }
                var constructor = TypesContainer.Thickness.FindConstructor([.. arguments.Select(a => context.Configuration.WellKnownTypes.Int32)]);
                if (constructor == null)
                {
                    context.ReportDiagnostic(new XamlDiagnostic("MYRA003", XamlDiagnosticSeverity.Fatal, $"No constructor for Thickness has {arguments.Count} parameters!", node));
                    return false;
                }

                result = new XamlAstNewClrObjectNode(node,
                    new XamlAstClrTypeReference(node, TypesContainer.Thickness, false),
                    constructor,
                    [.. arguments.Select(a => (IXamlAstValueNode)new XamlConstantNode(node, context.Configuration.WellKnownTypes.Int32, a))]);
                return true;
            }


            return false;
        }

        private static bool AssignFileResource(AstTransformationContext context, IXamlAstValueNode node, string methodName, ref IXamlAstValueNode result)
        {
            if (!GetText(node, out var text))
                return false;

            var loadMethod = TypesContainer.IFileResolver.GetMethod(m => m.IsStatic && m.Name == methodName);
            result = new XamlStaticOrTargetedReturnMethodCallNode(node, loadMethod,
                [
                    new XamlStaticOrTargetedReturnMethodCallNode(node,
                                    TypesContainer.MyraEnvironment.GetAllProperties().First(p => p.Name == "Resolver").Getter!,
                                    []),
                        new XamlConstantNode(node, context.Configuration.WellKnownTypes.String, text)
                ]);
            return true;
        }

        private static bool TryAssignSolidBrush(AstTransformationContext context, IXamlAstValueNode node, IXamlType type, [NotNullWhen(true)] out IXamlAstValueNode? result)
        {
            result = null;
            if (TryAssignColor(context, node, type, out result))
            {
                result = new XamlAstNewClrObjectNode(node,
                    new XamlAstClrTypeReference(node, TypesContainer.SolidBrush, false),
                    TypesContainer.SolidBrush.GetConstructor([TypesContainer.Color]),
                    [result]);
                return true;
            }

            return false;
        }

        private static bool TryAssignImage(AstTransformationContext context, IXamlAstValueNode node, [NotNullWhen(true)] out IXamlAstValueNode? result)
        {
            result = null;
            if (!GetText(node, out var text))
                return false;

            var styleSheetContainer = TransformerHelpers.GetStylesheet(context, node);

            var atlas = TypesContainer.StyleSheet.Properties.First(p => p.Name == "Atlas");
            var callAtlas = new XamlStaticOrTargetedReturnMethodCallNode(node, atlas.Getter!, [styleSheetContainer]);
            var ensureRegionMethod = TypesContainer.TextureRegionAtlas.GetMethod(m => m.Name == "EnsureRegion");
            result = new XamlStaticOrTargetedReturnMethodCallNode(node,
                new XamlWrappedMethod(ensureRegionMethod),
                [callAtlas, new XamlConstantNode(node, context.Configuration.WellKnownTypes.String, text)]);
            return true;
        }

        private static bool TryAssignColor(AstTransformationContext context, IXamlAstValueNode node, IXamlType type, [NotNullWhen(true)] out IXamlAstValueNode? result)
        {
            result = null;
            if (!GetText(node, out var text))
                return false;

            // check if value is one of the static readonly properties.
            var property = TypesContainer.Color.GetAllProperties().FirstOrDefault(t => t.Name == text);
            if (property != null && property.Getter!.IsStatic)
            {
                result = new XamlStaticOrTargetedReturnMethodCallNode(node, property.Getter, null);
                return true;
            }

            // parse value
            if (ParseHex(text, out var hex))
            {
                if (hex.Length != 6 && hex.Length != 8)
                {
                    context.ReportDiagnostic(new XamlDiagnostic("MYRA004", XamlDiagnosticSeverity.Fatal, "Hex number must have 6 or 8 hexadecimal characters!", node));
                    return false;
                }

                if (!uint.TryParse(hex,
                                   NumberStyles.HexNumber,
                                   CultureInfo.CurrentCulture,
                                   out var color))
                {
                    context.ReportDiagnostic(new XamlDiagnostic("MYRA005", XamlDiagnosticSeverity.Fatal, "Hex number is not valid!", node));
                    return false;
                }

                result = new XamlAstNewClrObjectNode(node,
                     new XamlAstClrTypeReference(node, TypesContainer.Color, false),
                     TypesContainer.Color.FindConstructor([TypesContainer.UInt32])!,
                     [(new XamlConstantNode(node, TypesContainer.UInt32, color))]);
                return true;
            }

            // parse r,g,b[,a]
            if (text.Contains(','))
            {
                var values = text.Split(',');
                if (values.Length != 3 && values.Length != 4)
                    return false;

                if (!values.Any(v => v.Contains('.')))
                {
                    // use integers 
                    var intArguments = new List<int>();
                    foreach (var value in values)
                    {
                        if (!Int32.TryParse(value.Trim(), out int r))
                        {
                            return false;
                        }
                        intArguments.Add(r);
                    }

                    var intConstructor = TypesContainer.Color.FindConstructor([.. intArguments.Select(a => context.Configuration.WellKnownTypes.Int32)])!;
                    result = new XamlAstNewClrObjectNode(node,
                        new XamlAstClrTypeReference(node, TypesContainer.Color, false),
                        intConstructor,
                        [.. intArguments.Select(a => (IXamlAstValueNode)new XamlConstantNode(node, context.Configuration.WellKnownTypes.Int32, a))]);
                    return true;
                }

                // use floats 
                var floatArguments = new List<float>();
                foreach (var value in values)
                {
                    if (!Single.TryParse(value.Trim(), out float r))
                    {
                        return false;
                    }
                    floatArguments.Add(r);
                }

                var constructor = TypesContainer.Color.FindConstructor([.. floatArguments.Select(a => TypesContainer.Single)])!;
                result = new XamlAstNewClrObjectNode(node,
                    new XamlAstClrTypeReference(node, TypesContainer.Color, false),
                    constructor,
                    [.. floatArguments.Select(a => (IXamlAstValueNode)new XamlConstantNode(node, TypesContainer.Single, a))]);
                return true;
            }

            return false;
        }

        private static bool ParseHex(string input, [NotNullWhen(true)] out string? hex)
        {
            if (input[0] == '#')
            {
                hex = input.Substring(1);
                return true;
            }

            if (input[0] == '0' && input[1] == 'x'
                || input[0] == '&' & input[1] == 'H')
            {
                hex = input.Substring(2);
                return true;
            }
            hex = null;
            return false;
        }

        private static bool GetText(IXamlAstValueNode node, [NotNullWhen(true)] out string? text)
        {
            text = null;
            if (node is not XamlAstTextNode textNode)
                return false;

            if (string.IsNullOrWhiteSpace(textNode.Text))
                return false;

            text = textNode.Text;
            return true;
        }

        public static TypeDefinition? FindTypeRecursive(this Collection<TypeDefinition> types, string fullName)
        {
            foreach (var type in types)
            {
                if (type.FullName == fullName)
                    return type;

                var nested = FindTypeRecursive(type.NestedTypes, fullName);

                if (nested != null)
                    return nested;
            }

            return null;
        }
    }
}
