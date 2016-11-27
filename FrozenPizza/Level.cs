﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using TiledSharp;

namespace FrozenPizza
{
    public enum Layers
    {
        Floor,
        Wall,
        Ceiling,
        Meta,
        Spawn
    }

	public enum Orientation
	{
		North,
		East,
		South,
		West
	}

    public enum Meta
    {
        Melee = 1,
        Pistol,
        Rifle
    }

    public class Level
    {
        int _drawMargin;
        int _twidth, _theight;
        int _ttwidth, _ttheight;
        TmxMap _map;
        List<Item>[] _entities;
        Texture2D _tileset;

        public Level(string mapName)
        {
            _map = new TmxMap(mapName);
            _twidth = _map.Tilesets[0].TileWidth;
            _theight = _map.Tilesets[0].TileHeight;
            _drawMargin = 10;
        }

        public bool Load(ContentManager content)
        { 
            _tileset = content.Load<Texture2D>("maps/" + _map.Tilesets[0].Name.ToString());
            _ttwidth = _tileset.Width / _twidth;
            _ttheight = _tileset.Height / _theight;
            _entities = new List<Item>[_map.Width * _map.Height];
            return (true);
        }

        public Rectangle mapToGrid(Vector2 pos)
        {
            return (new Rectangle((int)pos.X / _twidth, (int)pos.Y / _theight, ((int)pos.X / _twidth), (int)pos.Y / _twidth));
        }

        public Vector2 vmapToGrid(Vector2 pos)
        {
            return (new Vector2(pos.X / _twidth, pos.Y / _theight));
        }

		public Vector2 gridToMap(Vector2 pos)
		{
			return (new Vector2(pos.X * _twidth, pos.Y * _theight));
		}

        public void GenerateItems(Collection collection)
        {
            Random rnd = new Random();

            for (int i = 0; i < _map.Layers[(int)Layers.Meta].Tiles.Count; i++)
            {
                int gid = _map.Layers[(int)Layers.Meta].Tiles[i].Gid;

                if (gid == 0)
                    continue;
                else
                {
                    _entities[i] = new List<Item>();
                    _entities[i].Add(collection.MeleeList[rnd.Next(0, collection.MeleeList.Count)]);
                }
            }
        }

        public Vector2 getSpawnPoint()
        {
            Random rnd = new Random();
			int pos;
			TmxLayerTile spawn;

			pos = rnd.Next(0, _map.Layers[(int)Layers.Spawn].Tiles.Count);
			while (_map.Layers[(int)Layers.Spawn].Tiles[pos].Gid == 0)
				pos = rnd.Next(0, _map.Layers[(int)Layers.Spawn].Tiles.Count);
			spawn = _map.Layers[(int)Layers.Spawn].Tiles[pos];
			return (gridToMap(new Vector2(spawn.X, spawn.Y)));
        }

        public bool Collide(Vector2 pos)
        {
            if ((pos.X < 0 || pos.X > (_map.Width * _twidth))
                || (pos.Y < 0 || pos.Y > (_map.Height * _theight)))
                return (true);
            Rectangle posCell = mapToGrid(pos);

            if ((_map.Layers[(int)Layers.Wall].Tiles[(int)(posCell.X + (posCell.Y * _map.Width))].Gid != 0)
                || (_map.Layers[(int)Layers.Wall].Tiles[(int)(posCell.Width + (posCell.Height * _map.Width))].Gid != 0))
                return (true);
            return (false);
        }

        public bool Indoor(Vector2 pos)
        {
            Rectangle posCell = mapToGrid(pos);

            if ((_map.Layers[(int)Layers.Ceiling].Tiles[(int)(posCell.X + (posCell.Y * _map.Width))].Gid != 0)
                || (_map.Layers[(int)Layers.Ceiling].Tiles[(int)(posCell.Width + (posCell.Height * _map.Width))].Gid != 0))
                return (true);
            return (false);
        }

        public List<Item> getEntities(Vector2 pos)
        {
            Vector2 realpos = vmapToGrid(pos);

            return (_entities[(int)((realpos.Y * (float)_map.Width) + realpos.X)]);
        }

        public void Draw(SpriteBatch spriteBatch, Camera cam, Player mainPlayer, Collection collection)
        {
            Rectangle viewport = cam.getViewport();
            int xoffset = (viewport.X / _twidth);
            int xend = ((viewport.X + viewport.Width) / _twidth);
            int yoffset = (viewport.Y / _theight);
            int yend = ((viewport.Y + viewport.Height) / _theight);
            
            if (xoffset > _drawMargin)
                xoffset -= _drawMargin;
            if (yoffset > _drawMargin)
                yoffset -= _drawMargin;
            if (xend + _drawMargin < _map.Width)
                xend += _drawMargin;
            if (yend + _drawMargin < _map.Height)
                yend += _drawMargin;
            for (int y = 0; y <= yend; y++)
            {
                for (int x = 0; x <= xend; x++)
                {
                    for (int l = _map.Layers.Count - 1; l >= 0; l--)
                    {
						if ((Layers)l == Layers.Meta || (Layers)l == Layers.Spawn || (Indoor(mainPlayer.Pos) && (Layers)l == Layers.Ceiling))
                            continue;
                        if (((yoffset + y) < 0 || (xoffset + x) < 0) //TopLeft
                            || ((yoffset + y) > (_map.Height - 1) || (xoffset + x) > (_map.Width - 1))) //TopRight
                            continue;
                        int gid = _map.Layers[l].Tiles[((yoffset + y) * _map.Width) + xoffset + x].Gid;

                        // Empty tile, do nothing
                        if (gid == 0)
                            continue;

                            int tileFrame = gid - 1;
                            int column = tileFrame % _ttwidth;
                            int row = (int)Math.Floor((double)tileFrame / (double)_ttwidth);

                            Rectangle tilesetRec = new Rectangle(_twidth * column, _theight * row, _twidth, _theight);

                            spriteBatch.Draw(_tileset, new Rectangle((int)(xoffset + x) * _twidth, (int)(yoffset + y) * _theight, _twidth, _theight), tilesetRec, Color.White, 0, Vector2.Zero, SpriteEffects.None, (Layers)l == Layers.Ceiling ? 0.2f : 0.5f);
                            if (_entities[((yoffset + y) * _map.Width) + xoffset + x] != null)
                            {
                                for (int i = 0; i < _entities[((yoffset + y) * _map.Width) + xoffset + x].Count; i++)
                                {
                                    Item item = _entities[((yoffset + y) * _map.Width) + xoffset + x][i];

                                    spriteBatch.Draw(collection.MeleeTileset, new Rectangle((int)(xoffset + x) * _twidth, (int)(yoffset + y) * _theight, _twidth, _theight), item.SkinRect, Color.White, 0, Vector2.Zero, SpriteEffects.None, 0.3f);
                                }
                            }
                            break;
                    }
                }

            }
        }
    }
}
