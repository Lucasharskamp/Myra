using System; 

namespace Myra.Xaml.TypeSystem
{
    public sealed class NamespaceResolver
    {
        public const string MyraNamespace = "https://github.com/MyraUI/Myra";

        public string? Resolve(string xmlNamespace)
        {
            if (string.Equals(xmlNamespace, MyraNamespace, StringComparison.OrdinalIgnoreCase))
                return "Myra.Graphics2D.UI";

            return null;
        }
    }
}
