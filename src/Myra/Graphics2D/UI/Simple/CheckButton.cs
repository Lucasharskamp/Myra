using Myra.Events;
using Myra.Graphics2D.UI.Styles;
using System.ComponentModel;
using System.Collections;

namespace Myra.Graphics2D.UI
{
	/// <summary>
	/// A check button widget that can be checked or unchecked independently.
	/// </summary>
	public class CheckButton : CheckButtonBase
	{
		/// <summary>
		/// Gets or sets a value indicating whether the check button is checked.
		/// </summary>
		[Category("Behavior")]
		[DefaultValue(false)]
		public bool IsChecked
		{
			get => IsPressed;
			set => IsPressed = value;
		}

		/// <summary>
		/// Occurs when the checked state of the check button changes.
		/// </summary>
		public event MyraEventHandler IsCheckedChanged
		{
			add
			{
				PressedChanged += value;
			}

			remove
			{
				PressedChanged -= value;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CheckButton"/> class with the specified stylesheet and style.
		/// </summary>
		/// <param name="stylesheet">The stylesheet to use for applying the style.</param>
		/// <param name="styleName">The name of the style to apply. Defaults to the default stylesheet style.</param>
		public CheckButton(Stylesheet stylesheet, string styleName)
		{
			SetStyle(stylesheet, styleName);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CheckButton"/> class with the specified style.
		/// </summary>
		/// <param name="styleName">The name of the style to apply. Defaults to the default stylesheet style.</param>
		public CheckButton(string styleName) : this(Stylesheet.Current, styleName)
		{
		}


        /// <summary>
        /// Initializes a new instance of the <see cref="CheckButton"/> class.
        /// </summary>
        public CheckButton() : this(Stylesheet.Current, Stylesheet.DefaultStyleName)
        {
        }

        internal override IDictionary GetStylesDictionary(Stylesheet stylesheet) => stylesheet.CheckBoxStyles;
	}
}
