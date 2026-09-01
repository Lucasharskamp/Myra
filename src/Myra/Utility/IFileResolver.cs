using FontStashSharp;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.TextureAtlases;

namespace Myra.Utility
{
    /// <summary>
    /// File resolver interface for .xaml, .xmat and .xmms files
    /// </summary>
    public interface IFileResolver
    {
        /// <summary>
        /// Retrieve an already registered font by its <paramref name="id"/>
        /// </summary> 
        public SpriteFontBase GetFont(string id);

        /// <summary>
        /// Retrieve an already registered texture by its <paramref name="id"/>
        /// </summary> 
        public Texture2D GetTexture(string id);

        /// <summary>
        /// Retrieves a <see cref="TextureRegionAtlas"/> from its specified <paramref name="path"/>
        /// </summary> 
        public TextureRegionAtlas GetAtlas(GraphicsDevice device, string path);

        /// <summary>
        /// Register a font from a .xmms file
        /// </summary> 
        public SpriteFontBase RegisterFont(GraphicsDevice device, string id, string path, int size);

        /// <summary>
        /// Register a texture referenced in a .xmat, .xmms or .xaml file.
        /// </summary> 
        public Texture2D RegisterTexture(GraphicsDevice device, string path);
    }
}
