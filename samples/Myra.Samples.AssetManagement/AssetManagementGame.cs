using AssetManagementBase;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;
using System;
using System.IO;

namespace Myra.Samples.AssetManagement
{
	public class AssetManagementGame : Game
	{
		private readonly GraphicsDeviceManager _graphics;
		private MainForm _mainForm;
		private Desktop _desktop;

        public static IImage Logo { get; private set; }
        public static SpriteFontBase Arial64 { get; private set; }
        public static SpriteFontBase Calibri32 { get; private set; }
        public static SpriteFontBase ComicSans48 { get; private set; }

        public AssetManagementGame()
		{
			_graphics = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = 1200,
				PreferredBackBufferHeight = 800
			};
			Window.AllowUserResizing = true;
			IsMouseVisible = true;
		}

		protected override void LoadContent()
		{
			base.LoadContent();

			MyraEnvironment.Game = this;

			MyraEnvironment.DefaultAssetManager = AssetManager.CreateFileAssetManager(Path.Combine(AppContext.BaseDirectory, "Assets"));
            Arial64 = MyraEnvironment.DefaultAssetManager.LoadFont("fonts/arial64.fnt");
            Calibri32 = MyraEnvironment.DefaultAssetManager.LoadFont("fonts/calibri32.fnt");
            ComicSans48 = MyraEnvironment.DefaultAssetManager.LoadFont("fonts/comicSans48.fnt");
			Logo = MyraEnvironment.DefaultAssetManager.LoadImage("images/LogoOnly_64px.png");

            _mainForm = new MainForm();
			_mainForm._mainMenu.HoverIndex = 0;
			_mainForm._menuItemQuit.Selected += (s, a) => Exit();

			_desktop = new Desktop
			{
				FocusedKeyboardWidget = _mainForm._mainMenu
			};

			// Make main menu permanently hold keyboard focus
			_desktop.WidgetLosingKeyboardFocus += (s, a) =>
			{
				a.Cancel = true;
			};

			_desktop.Root = _mainForm;

            // Inform Myra that external text input is available
            // So it stops translating Keys to chars
            _desktop.HasExternalTextInput = true;

			// Provide that text input
			Window.TextInput += (s, a) =>
			{
                _desktop.OnChar(a.Character);
			};
		} 

		protected override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			GraphicsDevice.Clear(Color.Black);
			_desktop.Render();
		}

        protected override void Dispose(bool disposing)
        {
			_desktop.Dispose();
			_graphics.Dispose();
			base.Dispose(disposing);
        }
	}
}