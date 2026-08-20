using System;
using System.Collections.Generic;
using System.Text;

namespace Myra.Xaml.Types
{
    public sealed class XamlNameReference
    {
        public string Name { get; }

        public XamlNameReference(string name)
        {
            Name = name;
        }
    }
}
