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
        /// <see cref="System.ComponentModel.INotifyPropertyChanged"/> interface
        /// </summary>
        public static IXamlType INotifyPropertyChanged { get; set; } = default!;

        /// <summary>
        /// <see cref="System.ComponentModel.PropertyChangedEventArgs"/> class
        /// </summary>
        public static IXamlType PropertyChangedEventArgs { get; set; } = default!;

        /// <summary>
        /// <see cref="System.ComponentModel.PropertyChangedEventHandler"/> method.
        /// </summary>
        public static IXamlType PropertyChangedEventHandler { get; set; } = default!;

        /// <summary>
        /// "Myra.Graphics2D.UI.Widget" class.
        /// </summary>
        public static IXamlType Widget { get; set; } = default!;
    }
}
