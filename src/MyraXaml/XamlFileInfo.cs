using System;
using System.Xml.Linq;

namespace Myra.Xaml
{
    public class XamlFileInfo
    {
        public string? XClass { get; set; }

        public static XamlFileInfo Parse(string data)
        {
            var xdoc = XDocument.Parse(data);
            var xclass = xdoc.Root.Attribute(XName.Get("Class", "http://schemas.microsoft.com/winfx/2006/xaml"));
            return new XamlFileInfo
            {
                XClass = xclass?.Value
            };
        }
    }
}
