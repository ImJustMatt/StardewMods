using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace ImJustMatt.ExpandedStorage.Framework.Controllers
{
    internal class StorageSpriteController
    {
        private readonly int _depth;
        private readonly int _frames;
        private readonly string _path;
        private readonly bool _playerColor;

        internal StorageSpriteController(StorageController storage)
        {
            _frames = storage.Frames;
            _depth = storage.Depth;
            _playerColor = storage.PlayerColor;
            _path = storage.Path;
        }

        /// <summary>Property to access the SpriteSheet image.</summary>
        internal Texture2D Texture
        {
            get
            {
                var texture = Game1.content.Load<Texture2D>(_path);
                Width = texture.Width / Math.Max(1, _frames);
                Height = _playerColor ? texture.Height / 3 : texture.Height;
                TileWidth = Width / 16;
                TileHeight = (_depth is { } depth && depth > 0 ? depth : Height - 16) / 16;
                return texture;
            }
        }

        internal int Width { get; private set; }
        internal int Height { get; private set; }
        internal int TileWidth { get; private set; }
        internal int TileHeight { get; private set; }

        internal float ScaleSize
        {
            get
            {
                var tilesWide = Width / 16f;
                var tilesHigh = Height / 16f;
                return tilesWide switch
                {
                    >= 7 => 0.5f,
                    >= 6 => 0.66f,
                    >= 5 => 0.75f,
                    _ => tilesHigh switch
                    {
                        >= 5 => 0.8f,
                        >= 3 => 1f,
                        _ => tilesWide switch
                        {
                            <= 2 => 2f,
                            <= 4 => 1f,
                            _ => 0.1f
                        }
                    }
                };
            }
        }

        internal void ForEachPos(int x, int y, Action<Vector2> doAction)
        {
            for (var i = 0; i < TileWidth; i++)
            {
                for (var j = 0; j < TileHeight; j++)
                {
                    var pos = new Vector2(x + i, y + j);
                    doAction.Invoke(pos);
                }
            }
        }
    }
}