using Myra.Events;
using Myra.Graphics2D.UI.Styles;
using Myra.Utility;
using System;

namespace Myra.Graphics2D.UI
{
	/// <summary>
	/// An abstract base class for button-like content controls that can be pressed and respond to click events.
	/// </summary>
	public abstract class ButtonBase : ContentControl
	{
		private bool _isClicked = false;

		/// <summary>
		/// Occurs when the button is clicked.
		/// </summary>
		public event MyraEventHandler Click;

		/// <summary>
		/// Simulates a click on the button by firing touch down and touch up events.
		/// </summary>
		public void DoClick()
		{
			OnTouchDown();
			OnTouchUp();
		}

		/// <summary>
		/// Called when a touch point is released on the button. Derived classes should override to implement custom touch up behavior.
		/// </summary>
		protected abstract void InternalOnTouchUp();
		/// <summary>
		/// Called when a touch point is pressed on the button. Derived classes should override to implement custom touch down behavior.
		/// </summary>
		protected abstract void InternalOnTouchDown();

		/// <summary>
		/// Handles touch up events on the button.
		/// </summary>
		public override void OnTouchUp()
		{
			base.OnTouchUp();

			if (!Enabled)
			{
				return;
			}

			InternalOnTouchUp();

			if (_isClicked)
			{
				Click.Invoke(this, InputEventType.TouchUp);
				_isClicked = false;
			}
		}

		/// <summary>
		/// Handles touch down events on the button.
		/// </summary>
		public override void OnTouchDown()
		{
			base.OnTouchDown();

			if (!Enabled)
			{
				return;
			}

			InternalOnTouchDown();

			_isClicked = true;
		}

		/// <summary>
		/// Applies the specified button style to this button.
		/// </summary>
		/// <param name="style">The button style to apply.</param>
		public void ApplyButtonStyle(ButtonStyle style) => ApplyStyle(style);

		/// <summary>
		/// Applies the specified image button style to the button.
		/// </summary>
		/// <param name="style">The image button style to apply.</param>
		public void ApplyImageButtonStyle(ImageButtonStyle style)
		{
			ApplyStyle(style);

			if (style.ImageStyle != null)
			{
				var image = (Image)Content;
				image.ApplyImageStyle(style.ImageStyle);
			}
		}

		/// <summary>
		/// Copies the button properties from another button.
		/// </summary>
		/// <param name="w">The source button to copy from.</param>
		protected internal override void CopyFrom<T>(T w)
		{
			base.CopyFrom(w);

            if (w is not ButtonBase buttonBase)
            {
                throw new InvalidOperationException();
            } 
			PressedBackground = buttonBase.PressedBackground;
			IsPressed = buttonBase.IsPressed;
		}
	}
}
