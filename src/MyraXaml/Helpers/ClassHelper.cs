using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using XamlX;
using XamlX.Ast;

namespace Myra.Xaml.Helpers
{
    public static class ClassHelper
    {
        public static string? GetClassName(string xamlPath, string projectDirectory)
        {
            var fullXamlPath = Path.GetFullPath(xamlPath);
            var fullProjectDirectory = Directory.GetParent(Path.GetFullPath(projectDirectory));
            var relativePath = PathNetCore.GetRelativePath(fullProjectDirectory.FullName, fullXamlPath);
            var directory = Path.GetDirectoryName(relativePath).Replace('\\', '.');

            var name = Path.GetFileNameWithoutExtension(relativePath);

            if (string.IsNullOrWhiteSpace(name))
                return null;

            return directory + "." + name;
        }

        public static string? GetClassType(XamlAstObjectNode node, ITaskItem item, string xamlPath, string projectDirectory)
        {
            // get "x:Class" directive from the element, in case there is an override
            var documentDirectives = node.Children
                .OfType<XamlAstXmlDirective>()
                .Where(d => d.Namespace == XamlNamespaces.Xaml2006 && d.Name == "Class")
                .ToArray();

            if (documentDirectives.Length > 1)
            {
                throw new XamlLoadException("x:Class can only be defined once on a document!", node);
            }

            if (documentDirectives.Length == 1)
            {
                var classDirective = documentDirectives[0];
                node.Children.Remove(classDirective);
                // There should only be 1 value in there, namely the full specification of the class
                // (and nothing else)
                if (classDirective.Values.Count != 1 ||
                    classDirective.Values[0] is not XamlAstTextNode text)
                {
                    throw new XamlLoadException(
                        "x:Class must have a single string value.", classDirective);
                }
                return text.Text;
            }

            var explicitClass = item.GetMetadata("XamlClass");

            if (!string.IsNullOrWhiteSpace(explicitClass))
                return explicitClass;

            return GetClassName(xamlPath, projectDirectory);
        }
    }
}
