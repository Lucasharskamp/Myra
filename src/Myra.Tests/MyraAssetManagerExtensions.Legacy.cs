using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Properties;
using Myra.Graphics2D.UI.Styles;
using Myra.MML;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Linq;

namespace AssetManagementBase
{
    public static partial class MyraAssetManagerExtensions
    {
        private static readonly Dictionary<string, string> LegacyClassNames = new()
        {
            { "VerticalBox", "VerticalStackPanel" },
            { "HorizontalBox", "HorizontalStackPanel"},
            { "TextField", "TextBox"},
            { "TextBlock", "Label"},
            { "ScrollPane", "ScrollViewer"}
        };

        /// <summary>
        /// Loads a project from an XML string representation.
        /// </summary>
        /// <param name="data">The XML string containing the project definition.</param>
        /// <param name="assetManager">The asset manager used to load resources referenced by the project. If null, resources will not be loaded.</param>
        /// <param name="customStylesheet">An optional custom stylesheet to apply to the project. If not provided, the stylesheet path from the project's XML will be used.</param>
        /// <returns>A new Project instance loaded from the provided XML data, or null if loading fails.</returns>
        public static Project LoadProjectFromXml(string data, AssetManager assetManager = null, Stylesheet customStylesheet = null)
        {
            var xDoc = XDocument.Parse(data, LoadOptions.SetLineInfo);

            // Check if project specifies external stylesheet
            Stylesheet stylesheet;
            if (customStylesheet == null)
            {
                var stylesheetPathAttr = xDoc.Root.Attribute("StylesheetPath");
                if (stylesheetPathAttr != null)
                {
                    if (assetManager == null)
                    {
                        throw new Exception($"assetManager couldn't be null if the project has external stylesheet");
                    }

                    stylesheet = assetManager.LoadStylesheet(stylesheetPathAttr.Value);
                }
                else
                {
                    stylesheet = Stylesheet.Current;
                }
            }
            else
            {
                stylesheet = customStylesheet;
            }

            var result = new Project(stylesheet);

            var loadContext = CreateLoadContext(assetManager, stylesheet);
            loadContext.Load(result, xDoc.Root);
            result.ObjectsNodes = loadContext.ObjectsNodes;

            return result;
        }

        /// <summary>
        /// Creates a load context for deserializing UI projects from XML.
        /// Sets up asset loading, widget type resolution, and legacy name mapping.
        /// </summary> 
        internal static LoadContext CreateLoadContext(AssetManager assetManager, Stylesheet stylesheet)
        {
            // Collect widget assemblies: both Myra core types and user-supplied custom widgets
            Dictionary<Assembly, string[]> assemblies = [];
            assemblies.Add(typeof(Widget).Assembly, [typeof(Widget).Namespace, typeof(PropertyGrid).Namespace]);

            return new LoadContext
            {
                Assemblies = assemblies,
                LegacyClassNames = LegacyClassNames,
                ObjectCreator = (t, el) => CreateItem(t, el, stylesheet),
                AssetManager = assetManager,
                Stylesheet = stylesheet
            };
        }

        // Instantiates an object of the given type, handling special case of Widget constructors that accept StyleName parameter
        private static object CreateItem(Type type, XElement element, Stylesheet stylesheet)
        {
            if (typeof(Widget).IsAssignableFrom(type))
            {
                // Check if widget constructor accepts a style name parameter (string)
                var acceptsStyle = false;
                foreach (var c in type.GetConstructors())
                {
                    var p = c.GetParameters();
                    if (p != null && p.Length == 2)
                    {
                        if (p[0].ParameterType == typeof(Stylesheet) && p[1].ParameterType == typeof(string))
                        {
                            acceptsStyle = true;
                            break;
                        }
                    }
                }

                if (acceptsStyle)
                {
                    if (stylesheet == null)
                    {
                        throw new NullReferenceException(nameof(stylesheet));
                    }

                    // Extract StyleName from XML attribute, defaulting if not found
                    var styleName = Stylesheet.DefaultStyleName;
                    var styleNameAttr = element.Attribute("StyleName");
                    if (styleNameAttr != null)
                    {
                        styleName = styleNameAttr.Value;
                    }

                    // Create widget with style name parameter
                    try
                    {
                        return (Widget)Activator.CreateInstance(type, stylesheet, styleName);
                    }
                    catch (TargetInvocationException ex)
                    {
                        if (ex.InnerException != null)
                        {
                            throw ex.InnerException;
                        }

                        throw;
                    }
                }
            }

            // Create non-widget object or widget without style parameter
            return Activator.CreateInstance(type);
        }
    }
}
