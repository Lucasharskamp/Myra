using Myra.Xaml.Types; 
using System.IO;
using System.Linq; 
using System.Xml;
using XamlX;
using XamlX.Ast; 
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Compiler
{
    public sealed class MyraXamlParser
    {
        private readonly TransformerConfiguration _configuration;
         
         
        public MyraXamlParser(TransformerConfiguration configuration)
        {
            _configuration = configuration;
        }


        public XamlDocument Parse(string text)
        {
            using var stringReader = new StringReader(text);

            using var xmlReader = XmlReader.Create(stringReader,
                    new XmlReaderSettings
                    {
                        IgnoreComments = true,
                        IgnoreWhitespace = false
                    });

            // read until we hit content to work with.
            while (xmlReader.Read() && xmlReader.NodeType != XmlNodeType.Element)
            { }

            var root = ParseElement(xmlReader);

            return new XamlDocument
            {
                Root = root
            };
        }

        private XamlAstObjectNode ParseElement(XmlReader reader)
        {
            var lineInfo = GetLineInfo(reader);

            var type = ResolveType(reader.NamespaceURI, reader.LocalName);

            var node = new XamlAstObjectNode(lineInfo, new XamlAstClrTypeReference(lineInfo,  type, false));

            ParseAttributes(reader, node);

            if (!reader.IsEmptyElement)
            {
                reader.Read();

                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        node.Children.Add(ParseElement(reader));
                    }
                    else if (reader.NodeType == XmlNodeType.Text &&
                             !string.IsNullOrWhiteSpace(reader.Value))
                    {
                        node.Children.Add(
                            new XamlAstTextNode(
                                GetLineInfo(reader),
                                reader.Value));
                    }

                    reader.Read();
                }
            }

            return node;
        }


        private void ParseAttributes(XmlReader reader, XamlAstObjectNode node)
        {
            if (!reader.HasAttributes)
                return;


            while (reader.MoveToNextAttribute())
            {
                if (reader.Prefix == "xmlns" ||
                    reader.Name.StartsWith("xmlns"))
                    continue;

                var lineInfo = GetLineInfo(reader);
                var objectType = ((XamlAstClrTypeReference)node.Type).Type;

                var property = objectType.Properties.FirstOrDefault(x => x.Name == reader.LocalName);

                if (property == null)
                {
                    throw new XamlParseException(
                        $"Property '{reader.LocalName}' " +
                        $"does not exist on '{objectType.FullName}'.",
                        lineInfo.Line,
                        lineInfo.Position);
                }

                var propertyReference = new XamlAstClrProperty(lineInfo, property, _configuration);
                var value = new XamlAstTextNode(lineInfo, reader.Value);
                node.Children.Add(new XamlAstXamlPropertyValueNode(lineInfo, propertyReference, value, isAttributeSyntax: true));
            }


            reader.MoveToElement();
        }

        private IXamlType ResolveType(string xmlns, string name)
        {
            if (!_configuration.XmlnsMappings.Namespaces.TryGetValue(xmlns, out var mappings))
            {
                throw new XamlParseException($"Unknown namespace '{xmlns}'", 0, 0);
            }

            foreach (var mapping in mappings)
            {
                var type = mapping.asm.FindType(mapping.ns + "." + name);

                if (type != null)
                    return type;
            }

            var regularType = _configuration.TypeSystem.FindType(name);
            if (regularType != null)
                return regularType;

            throw new XamlParseException($"Unable to resolve type '{name}'", 0, 0);
        } 

        private IXamlLineInfo GetLineInfo(XmlReader reader)
        {
            if (reader is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
            {
                return new MyraLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
            }

            return new MyraLineInfo(0, 0);
        }
    }
}
