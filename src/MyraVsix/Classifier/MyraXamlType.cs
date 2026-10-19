using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Myra.Classifier
{
    internal class MyraXamlType
    {
        public MyraXamlType(INamedTypeSymbol type)
        {
            Type = type;
        }

        public INamedTypeSymbol Type { get; }
    }
}
