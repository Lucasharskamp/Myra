using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
    /// <summary>
    /// Contains a x:StyleSheet handler.
    /// </summary>
    public sealed class XamlStyleSheetContainer
    {
        public IXamlType Container { get; }
        public IXamlProperty Property { get; }
        public XamlStyleSheetContainer(IXamlType container, IXamlProperty property)
        {
            Container = container;
            Property = property;
        }
    }
}
