using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace Myra
{
    /// <summary>
    /// Establishes an <see cref="IAdornmentLayer"/> to place the adornment on and exports the <see cref="IWpfTextViewCreationListener"/>
    /// that instantiates the adornment on the event of a <see cref="IWpfTextView"/>'s creation
    /// </summary>
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("MyraXaml")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed class MyraXamlAdornmentTextViewCreationListener : IWpfTextViewCreationListener
    {
        /// <summary>
        /// Defines the adornment layer for the adornment. This layer is ordered
        /// after the selection layer in the Z-order
        /// </summary>
        [Export(typeof(AdornmentLayerDefinition))]
        [Name("MyraXamlAdornment")]
        [Order(After = PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)]
        private AdornmentLayerDefinition editorAdornmentLayer = default!;

        #region IWpfTextViewCreationListener

        /// <summary>
        /// Called when a text view having matching roles is created over a text data model having a matching content type.
        /// Instantiates a MyraXamlAdornment manager when the textView is created.
        /// </summary>
        /// <param name="textView">The <see cref="IWpfTextView"/> upon which the adornment should be placed</param>
        public void TextViewCreated(IWpfTextView textView)
        { 
            if (!textView.TextDataModel.DocumentBuffer.Properties.TryGetProperty<ITextDocument>(typeof(ITextDocument), out var textDocument)
                || !textDocument.FilePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (textView.TextDataModel.DocumentBuffer.ContentType.IsOfType("MyraXaml"))
            {

            }

            // The adornment will listen to any event that changes the layout (text changes, scrolling, etc)
            new MyraXamlAdornment(textView);
        }

        #endregion
    }
}
