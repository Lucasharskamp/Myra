using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;
using XamlX.Ast;
using XamlX.Transform;
using XamlX.TypeSystem;

namespace Myra.Xaml.Types
{
    internal sealed class XamlViewModelContainer
    { 
        public XamlViewModelContainer(IXamlType viewModelType, IXamlProperty property)
        {
            Type = viewModelType;
            Property = property;
        }

        public IXamlType Type { get; }

        /// <summary>
        /// The property in the code-behind class which points to the view model.
        /// </summary>
        public IXamlProperty Property { get; }

        public XamlStaticOrTargetedReturnMethodCallNode GetPropertyCall(IXamlLineInfo lineInfo, IXamlAstValueNode owner)
        {
            return new XamlStaticOrTargetedReturnMethodCallNode(lineInfo, Property.Getter!, [owner]);
        }
    }
}
