using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using XamlX.TypeSystem;

namespace Myra.Xaml.Helpers
{
    internal static class TypesContainer
    {
        /// <summary>
        /// Current XAML file code-behind type definition (only for use in Transformers!)
        /// </summary>
        public static TypeDefinition? CurrentClassDefinition { get; set; }

        /// <summary>
        /// / Current XAML file code-behind type (only for use in Transformers!)
        /// </summary>
        public static IXamlType? CurrentClass { get; set; }

        /// <summary>
        /// <see cref="System.EventHandler"/> delegate.
        /// </summary>
        public static IXamlType EventHandler { get; set; } = default!;

        /// <summary>
        /// <see cref="System.ComponentModel.INotifyPropertyChanged"/> interface
        /// </summary>
        public static IXamlType INotifyPropertyChanged { get; set; } = default!;

        /// <summary>
        /// <see cref="INotifyPropertyChanged.PropertyChanged"/>'s += operator method.
        /// </summary>
        public static IXamlMethod PropertyChangedEventAdd { get; set; } = default!;

        /// <summary>
        /// <see cref="System.ComponentModel.PropertyChangedEventArgs"/> class
        /// </summary>
        public static IXamlType PropertyChangedEventArgs { get; set; } = default!;

        /// <summary>
        /// <see cref="System.ComponentModel.PropertyChangedEventHandler"/> method.
        /// </summary>
        public static IXamlType PropertyChangedEventHandler { get; set; } = default!; 
    }
}
