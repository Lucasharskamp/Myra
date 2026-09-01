using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using XamlX.TypeSystem;

namespace Myra.Xaml.Helpers
{
    internal static class TypesContainer
    {
        /// <summary>
        /// <see cref="System.ComponentModel.INotifyPropertyChanged"/> interface
        /// </summary>
        public static IXamlType INotifyPropertyChanged { get; private set; } = default!;

        /// <summary>
        /// <see cref="System.IServiceProvider"/>
        /// </summary>
        public static IXamlType IServiceProvider { get; private set; } = default!;

        /// <summary>
        /// <see cref="System.Byte"/>
        /// </summary>
        public static IXamlType Byte { get; private set; } = default!;

        /// <summary>
        /// <see cref="System.Int16"/>
        /// </summary>
        public static IXamlType Int16 { get; private set; } = default!;

        /// <summary>
        /// <see cref="System.UInt16"/>
        /// </summary>
        public static IXamlType UInt16 { get; private set; } = default!;

        /// <summary>
        /// <see cref="System.UInt32"/>
        /// </summary>
        public static IXamlType UInt32 { get; private set; } = default!;

        /// <summary>
        /// <see cref="System.Int64"/>
        /// </summary>
        public static IXamlType Int64 { get; private set; } = default!;

        /// <summary>
        /// <see cref="System.UInt64"/>
        /// </summary>
        public static IXamlType UInt64 { get; private set; } = default!;

        /// <summary>
        /// <see cref="System.Single"/>
        /// </summary>
        public static IXamlType Single { get; private set; } = default!;

        /// <summary>
        /// <see cref="System.ComponentModel.PropertyChangedEventArgs"/> class
        /// </summary>
        public static IXamlType PropertyChangedEventArgs { get; private set; } = default!;

        /// <summary>
        /// <see cref="System.ComponentModel.PropertyChangedEventHandler"/> method.
        /// </summary>
        public static IXamlType PropertyChangedEventHandler { get; private set; } = default!;

        /// <summary>
        /// "Microsoft.Xna.Framework.Color" class
        /// </summary>
        public static IXamlType Color { get; private set; } = default!;
         
        /// <summary>
        /// "Myra.Graphics2D.UI.Container" class.
        /// </summary>
        public static IXamlType Container { get; private set; } = default!;
         
        /// <summary>
        /// <see cref="System.Func{TResult}"/>
        /// </summary>
        public static IXamlType Func { get; private set; } = default!;

        /// <summary>
        /// "Myra.Graphics2D.IBrush" class
        /// </summary>
        public static IXamlType IBrush { get; private set; } = default!;
          
        /// <summary>
        /// "Myra.Graphics2D.IImage" class
        /// </summary>
        public static IXamlType IImage { get; private set; } = default!;

        /// <summary>
        /// "Myra.Utilities.IFileResolver" class
        /// </summary>
        public static IXamlType IFileResolver { get; private set; } = default!;

        /// <summary>
        /// <see cref="System.Lazy{T}"/>
        /// </summary>
        public static IXamlType LazyOfT1 { get; private set; } = default!;

        /// <summary>
        /// "Myra.MyraEnvironment" class
        /// </summary>
        public static IXamlType MyraEnvironment { get; private set; } = default!;
          
        /// <summary>
        /// "Myra.Graphics2D.UI.Proportion" class
        /// </summary>
        public static IXamlType Proportion { get; private set; } = default!;

        /// <summary>
        /// "Microsoft.Xna.Framework.Rectangle" class
        /// </summary>
        public static IXamlType Rectangle { get; private set; } = default!;

        /// <summary>
        /// "Myra.Graphics2D.Brushes.SolidBrush" class
        /// </summary>
        public static IXamlType SolidBrush { get; private set; } = default!;

        /// <summary>
        /// "FontStashSharp.SpriteFontBase" class
        /// </summary>
        public static IXamlType SpriteFontBase { get; private set; } = default!; 

        /// <summary>
        /// "Myra.Graphics2D.UI.Styles.StyleSheet" class
        /// </summary>
        public static IXamlType StyleSheet { get; private set; } = default!;

        /// <summary>
        /// "Myra.Graphics2D.UI.Styles.StylesheetFont" class
        /// </summary>
        public static IXamlType StylesheetFont { get; private set; } = default!;

        /// <summary>
        /// "Myra.Graphics2D.UI.Styles" class
        /// </summary>
        public static IXamlType StylesheetFontsCollection { get; private set; } = default!;

        /// <summary>
        /// "Myra.Graphics2D.TextureAtlases.Texture2D" class
        /// </summary>
        public static IXamlType Texture2D { get; private set; } = default!;

        /// <summary>
        /// "Myra.Graphics2D.TextureAtlases.TextureRegion" class
        /// </summary>
        public static IXamlType TextureRegion { get; private set; } = default!;

        /// <summary>
        /// "Myra.Graphics2D.TextureAtlases.TextureRegionAtlas" class
        /// </summary>
        public static IXamlType TextureRegionAtlas { get; private set; } = default!;

        /// <summary>
        /// "Myra.Graphics2D.Thickness" class
        /// </summary>
        public static IXamlType Thickness { get; private set; } = default!;

        /// <summary>
        /// "Myra.Graphics2D.UI.Widget" class.
        /// </summary>
        public static IXamlType Widget { get; private set; } = default!; 

        internal static void Setup(IXamlTypeSystem typeSystem)
        {
            INotifyPropertyChanged = typeSystem.GetType(typeof(INotifyPropertyChanged).FullName);
            IServiceProvider = typeSystem.GetType(typeof(IServiceProvider).FullName);
            Byte = typeSystem.GetType(typeof(byte).FullName);
            Int16 = typeSystem.GetType(typeof(short).FullName);
            UInt16 = typeSystem.GetType(typeof(ushort).FullName);
            UInt32 = typeSystem.GetType(typeof(uint).FullName);
            Int64 = typeSystem.GetType(typeof(long).FullName);
            UInt64 = typeSystem.GetType(typeof(ulong).FullName);
            Single = typeSystem.GetType(typeof(float).FullName);
            PropertyChangedEventArgs = typeSystem.GetType(typeof(PropertyChangedEventArgs).FullName);
            PropertyChangedEventHandler = typeSystem.GetType(typeof(PropertyChangedEventHandler).FullName);
            Color = typeSystem.GetType("Microsoft.Xna.Framework.Color");
            Container = typeSystem.GetType("Myra.Graphics2D.UI.Container");
            Func = typeSystem.GetType("System.Func`1");
            IBrush = typeSystem.GetType("Myra.Graphics2D.IBrush");
            IFileResolver = typeSystem.GetType("Myra.Utility.IFileResolver");
            IImage = typeSystem.GetType("Myra.Graphics2D.IImage");
            LazyOfT1 = typeSystem.GetType("System.Lazy`1");
            LazyOfT1 = typeSystem.GetType("System.Lazy`1");
            MyraEnvironment = typeSystem.GetType("Myra.MyraEnvironment");
            Thickness = typeSystem.GetType("Myra.Graphics2D.Thickness");
            Proportion = typeSystem.GetType("Myra.Graphics2D.UI.Proportion");
            Rectangle = typeSystem.GetType("Microsoft.Xna.Framework.Rectangle");
            SolidBrush = typeSystem.GetType("Myra.Graphics2D.Brushes.SolidBrush");
            SpriteFontBase = typeSystem.GetType("FontStashSharp.SpriteFontBase");
            StyleSheet = typeSystem.GetType("Myra.Graphics2D.UI.Styles.Stylesheet");
            StylesheetFont = typeSystem.GetType("Myra.Graphics2D.UI.Styles.StylesheetFont");
            StylesheetFontsCollection = typeSystem.GetType("Myra.Graphics2D.UI.Styles.StylesheetFontsCollection");
            Texture2D = typeSystem.GetType("Microsoft.Xna.Framework.Graphics.Texture2D");
            TextureRegion = typeSystem.GetType("Myra.Graphics2D.TextureAtlases.TextureRegion");
            TextureRegionAtlas = typeSystem.GetType("Myra.Graphics2D.TextureAtlases.TextureRegionAtlas");
            Widget = typeSystem.GetType("Myra.Graphics2D.UI.Widget");
        } 
    }
}
