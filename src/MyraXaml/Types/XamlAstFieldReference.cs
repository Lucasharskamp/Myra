using XamlX.Ast;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
    public sealed class XamlAstFieldReference : XamlAstNode, IXamlAstValueNode
    {
        public IXamlField Field { get; }

        public IXamlAstNode Source { get; }

        public IXamlAstTypeReference Type { get; }

        public XamlAstFieldReference(
            IXamlLineInfo lineInfo,
            IXamlField field,
            IXamlAstNode source) : base(lineInfo)
        {
            Field = field;
            Source = source;
            Type = new XamlAstClrTypeReference(lineInfo, field.FieldType, false);
        }         
    }
}
