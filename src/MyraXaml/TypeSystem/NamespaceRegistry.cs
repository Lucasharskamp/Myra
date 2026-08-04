using System;
using System.Collections.Generic;
using System.Text;

namespace Myra.Xaml.TypeSystem
{
    public sealed class NamespaceRegistry
    {
        private readonly Dictionary<string, string> _namespaces = [];

        public NamespaceRegistry()
        {
            _namespaces["https://github.com/MyraUI/Myra"] = "Myra.Graphics2D.UI"; 
        }

        public void Register(string xmlNamespace, string clrNamespace)
        {
            _namespaces[xmlNamespace] = clrNamespace;
        }

        public bool TryResolve(string xmlNamespace, out string clrNamespace)
        {
            return _namespaces.TryGetValue(
                xmlNamespace,
                out clrNamespace!);
        }
    }
}
