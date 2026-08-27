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
        /// <see cref="System.Byte"/>
        /// </summary>
        public static IXamlType Byte { get; set; } = default!;

        /// <summary>
        /// <see cref="System.Int16"/>
        /// </summary>
        public static IXamlType Int16 { get; set; } = default!;

        /// <summary>
        /// <see cref="System.UInt16"/>
        /// </summary>
        public static IXamlType UInt16 { get; set; } = default!;

        /// <summary>
        /// <see cref="System.UInt32"/>
        /// </summary>
        public static IXamlType UInt32 { get; set; } = default!;

        /// <summary>
        /// <see cref="System.Int64"/>
        /// </summary>
        public static IXamlType Int64 { get; set; } = default!;

        /// <summary>
        /// <see cref="System.UInt64"/>
        /// </summary>
        public static IXamlType UInt64 { get; set; } = default!;

        /// <summary>
        /// <see cref="System.ComponentModel.PropertyChangedEventArgs"/> class
        /// </summary>
        public static IXamlType PropertyChangedEventArgs { get; set; } = default!;

        /// <summary>
        /// <see cref="System.ComponentModel.PropertyChangedEventHandler"/> method.
        /// </summary>
        public static IXamlType PropertyChangedEventHandler { get; set; } = default!;

        /// <summary>
        /// "Microsoft.Xna.Framework.Color" class
        /// </summary>
        public static IXamlType Color { get; set; } = default!;

        /// <summary>
        /// "Myra.Graphics2D.Thickness" class
        /// </summary>
        public static IXamlType Thickness { get; set; } = default!;

        /// <summary>
        /// "Myra.Graphics2D.UI.Proportion" class
        /// </summary>
        public static IXamlType Proportion { get; set; } = default!;

        /// <summary>
        /// "Myra.Graphics2D.UI.Widget" class.
        /// </summary>
        public static IXamlType Widget { get; set; } = default!;
    }
}
