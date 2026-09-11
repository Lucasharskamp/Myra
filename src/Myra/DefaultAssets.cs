using Myra.Graphics2D.TextureAtlases;
using FontStashSharp;

namespace Myra
{
	/// <summary>
	/// Provides access to the default stylesheets and assets included with Myra.
	/// </summary>
	public static class DefaultAssets
	{
		private static TextureRegion _whiteRegion;

		/// <summary>
		/// Gets a default white texture region used for placeholder graphics and fills.
		/// </summary>
		public static TextureRegion WhiteRegion
		{
			get
			{
				if (_whiteRegion == null)
				{
#if !PLATFORM_AGNOSTIC
					_whiteRegion = new TextureRegion(SpriteFontBase.GetWhite(MyraEnvironment.GraphicsDevice));
#else
					_whiteRegion = new TextureRegion(SpriteFontBase.GetWhite(MyraEnvironment.Platform.Renderer.TextureManager));
#endif
				}

				return _whiteRegion;
			}
		}

	}
}