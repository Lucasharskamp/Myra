using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Text; 

namespace Myra.Classifier
{
    internal interface IMyraValueType
    {
        ITypeSymbol? TypeSymbol { get; }
        SnapshotSpan ValueSpan { get; }
        string Value { get; }
    }
}
