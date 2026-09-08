using Myra.Attributes;
using System.Collections.Generic;
using System.Collections;
using System.Xml.Serialization;


#if MONOGAME || FNA
using Microsoft.Xna.Framework;
#elif STRIDE
using Stride.Core.Mathematics;
#else
using System.Drawing;
#endif

namespace Myra.Graphics2D.UI.Styles
{
	/// <summary>
	/// Represents a collection of <see cref="StylesheetFont"/> objects used by a stylesheet.
	/// </summary>
	[XmlName("Fonts")]
	public class StylesheetFontsCollection : Dictionary<string, StylesheetFont>
	{
		/// <summary>
		/// Gets or sets the atlas used space rectangle for the font collection.
		/// </summary>
		[XmlName("UsedSpace")]
		public Rectangle? AtlasUsedSpace { get; set; }
	}
}
