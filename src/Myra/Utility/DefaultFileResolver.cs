using FontStashSharp;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.TextureAtlases;
using System.Collections.Generic;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace Myra.Utility
{
    /// <summary>
    /// Default resolver for retrieving files referenced in .xaml, .xmms or .xmat files
    /// </summary>
    internal sealed class DefaultFileResolver : IFileResolver
    {
        private readonly FontSystem fontSystem = new FontSystem();
        private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, SpriteFontBase> _fonts = new Dictionary<string, SpriteFontBase>();

        public TextureRegionAtlas GetAtlas(GraphicsDevice device, string path)
        {
            return TextureRegionAtlas.FromXml(File.ReadAllText(path), (t) => RegisterTexture(device, t));
        }

        public SpriteFontBase GetFont(string id)
        {
            return _fonts[id];
        }

        public Texture2D GetTexture(string id)
        {
            return _textures[id];
        }

        public SpriteFontBase RegisterFont(GraphicsDevice device, string id, string path, int size)
        {
            if (_fonts.TryGetValue(id, out var font))
            {
                return font;
            }
            using var fileStream = File.OpenRead(path);
            fontSystem.AddFont(fileStream);
            var result = fontSystem.GetFont(size);
            _fonts.Add(id, result);
            return result;
        }

        public Texture2D RegisterTexture(GraphicsDevice device, string path)
        {
            if (_textures.TryGetValue(path, out var texture))
            {
                return texture;
            }
            var result = Texture2D.FromFile(device, path);
            _textures.Add(path, result);
            return result;
        }
    }
}
