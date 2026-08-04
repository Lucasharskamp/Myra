using Myra.Xaml.Types;
using Myra.Xaml.TypeSystem;
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

        private XmlReader _reader = null!;


        public MyraXamlParser(
            TransformerConfiguration configuration)
        {
            _configuration = configuration;
        }


        public XamlDocument Parse(
            string fileName,
            string text)
        {
            using var stringReader = new StringReader(text);

            using var xml = XmlReader.Create(stringReader,
                    new XmlReaderSettings
                    {
                        IgnoreComments = true,
                        IgnoreWhitespace = false
                    });

            _reader = xml;
            MoveToContent();

            var root = ParseElement();

            return new XamlDocument
            {
                Root = root
            };
        }

        private XamlAstObjectNode ParseElement()
        {
            var lineInfo = GetLineInfo();

            var type = ResolveType(_reader.NamespaceURI, _reader.LocalName);

            var node = new XamlAstObjectNode(lineInfo, new XamlAstClrTypeReference(lineInfo,  type, false));

            ParseAttributes(node);

            if (!_reader.IsEmptyElement)
            {
                _reader.Read();

                while (_reader.NodeType != XmlNodeType.EndElement)
                {
                    if (_reader.NodeType == XmlNodeType.Element)
                    {
                        node.Children.Add(ParseElement());
                    }
                    else if (_reader.NodeType == XmlNodeType.Text &&
                             !string.IsNullOrWhiteSpace(_reader.Value))
                    {
                        node.Children.Add(
                            new XamlAstTextNode(
                                GetLineInfo(),
                                _reader.Value));
                    }

                    _reader.Read();
                }
            }

            return node;
        }


        private void ParseAttributes(XamlAstObjectNode node)
        {
            if (!_reader.HasAttributes)
                return;


            while (_reader.MoveToNextAttribute())
            {
                if (_reader.Prefix == "xmlns" ||
                    _reader.Name.StartsWith("xmlns"))
                    continue;

                var lineInfo = GetLineInfo();
                var objectType = ((XamlAstClrTypeReference)node.Type).Type;

                var property = objectType.Properties.FirstOrDefault(x => x.Name == _reader.LocalName);

                if (property == null)
                {
                    throw new XamlParseException(
                        $"Property '{_reader.LocalName}' " +
                        $"does not exist on '{objectType.FullName}'.",
                        lineInfo.Line,
                        lineInfo.Position);
                }

                var propertyReference = new XamlAstClrProperty(lineInfo, property, _configuration);
                var value = new XamlAstTextNode(lineInfo, _reader.Value);
                node.Children.Add(new XamlAstXamlPropertyValueNode(lineInfo, propertyReference, value, isAttributeSyntax: true));
            }


            _reader.MoveToElement();
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


        private void MoveToContent()
        {
            while (_reader.Read() &&
                   _reader.NodeType != XmlNodeType.Element)
            {
            }
        }

        private IXamlLineInfo GetLineInfo()
        {
            if (_reader is IXmlLineInfo lineInfo && lineInfo.HasLineInfo())
            {
                return new MyraLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
            }

            return new MyraLineInfo(0, 0);
        }
    }
}
