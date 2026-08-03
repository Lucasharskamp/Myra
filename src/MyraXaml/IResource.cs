using System.Collections.Generic;
using XamlX.TypeSystem;

namespace Myra.Xaml
{
    interface IResource : IFileSource
    {
        string Uri { get; }
        string Name { get; }
        void Remove();
    }

    interface IResourceGroup
    {
        string Name { get; }
        List<IResource> Resources { get; }
    }
}
