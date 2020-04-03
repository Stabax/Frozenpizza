﻿using System;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;
using TiledSharp;
using System.Diagnostics;

namespace FrozenPizzaServer
{
    public enum Layers
    {
        Floor,
        HWall,
        Wall,
        Ceiling,
        Meta,
        Spawn
    }
    public enum Meta
    {
        Melee = 1,
        Pistol,
        Rifle
    }
    public class Level
    {
        //Tiles
        int _twidth, _theight;

        //Map
        TmxMap _map;
        List<Item> _entities;

		//EntIDs
		Int64 _currentUid;

        //Dynamic
        List<Projectile> _projectiles;

        //Thread
        Thread _thread;
        DateTime _lastTick;

        public TmxMap Map {  get { return (_map); } }
        public List<Item> Entities { get { return (_entities); } }
        public List<Projectile> Projectiles { get { return (_projectiles); } }

        public Level(String mapName)
        {
            _map = new TmxMap(mapName);
            _twidth = _map.Tilesets[0].TileWidth;
            _theight = _map.Tilesets[0].TileHeight;
			_entities = new List<Item>();
            _projectiles = new List<Projectile>();
			_currentUid = 0;
            GenerateItems();
            _lastTick = DateTime.Now;
        }

        public PointF vmapToGrid(PointF pos)
        {
            return (new PointF((int)pos.X / _twidth, (int)pos.Y / _theight));
        }

        public PointF vgridToMap(PointF pos)
        {
            return (new PointF((int)pos.X * _twidth, (int)pos.Y * _theight));
        }

        public void startUpdateThread()
        {
            _thread = new Thread(Update);
            _thread.Start();
        }

        //Generation
        public void GenerateItems()
        {
            Random rnd = new Random();

			for (int y = 0; y < _map.Height; y++)
			{
				for (int x = 0; x < _map.Width; x++)
				{
					int id = 0;
					int gid = _map.Layers[(int)Layers.Meta].Tiles[(_map.Width * y) + x].Gid;

                    if (gid == 0 || rnd.Next(0, 2) == 0) //Skip empty & 50% chance of spawn
                        continue;
                    else
                    {
                        if (gid == (int)Meta.Melee)
                        {
                            id = rnd.Next(1, 4);
                        }
                        else if (gid == (int)Meta.Pistol)
                        {
                            id = rnd.Next(1000, 1004);
                        }
                        _entities.Add(new Item(_currentUid, id, new PointF(x * _twidth, y * _theight) + new SizeF(_twidth / 2, _theight / 2)));
                        _currentUid++;
                    }
				}
			}
        }

		public int getEntityIndex(Int64 uid)
		{
			for (int i = 0; i < _entities.Count; i++)
			{
				if (_entities[i].Uid == uid)
					return (i);
			}
			return (-1);
		}
        //Bool checks
        public bool Collide(PointF pos)
        {
            PointF realpos = vmapToGrid(pos);

            if ((realpos.X < 0 || realpos.X >= _map.Width)
                || (realpos.Y < 0 || realpos.Y >= _map.Height))
                return (true);
            if (_map.Layers[(int)Layers.Wall].Tiles[(int)((_map.Width * realpos.Y) + realpos.X)].Gid != 0)
                return (true);
            return (false);
        }

        public bool RCollide(Rectangle rect)
        {
            if ((Collide(rect.Location) || Collide(rect.Location + rect.Size))
                || Collide(rect.Location + new Size(rect.Width, 0)) || Collide(rect.Location + new Size(0, rect.Height)))
                return (true);
            return (false);
        }



        public PointF getSpawnLocation()
		{
			Random rnd = new Random();
			int pos;
			TmxLayerTile spawn;

			pos = rnd.Next(0, _map.Layers[(int)Layers.Spawn].Tiles.Count);
			while (_map.Layers[(int)Layers.Spawn].Tiles[pos].Gid == 0)
				pos = rnd.Next(0, _map.Layers[(int)Layers.Spawn].Tiles.Count);
			spawn = _map.Layers[(int)Layers.Spawn].Tiles[pos];
			return (new PointF((spawn.X * _twidth) + (_twidth / 2), (spawn.Y * _theight) + (_theight / 2)));
		}

        public void updateProjectiles()
        {
            for (int i = Projectiles.Count - 1; i >= 0; i--)
            {
                if (!Projectiles[i].Update())
                {
                    Projectiles.RemoveAt(i);
                }
            }
        }

        void Update(object state)
        {
            while (true)
            {
                TimeSpan elapsedTime = DateTime.Now - _lastTick;
                _lastTick = DateTime.Now;

                updateProjectiles();
                for (int i = 0; i < Server.ClientList.Count; i++)
                {
                    if (Server.ClientList[i] == null || Server.ClientList[i].Player == null)
                        continue;
                    Server.ClientList[i].Player.Update(elapsedTime);
                }
                Thread.Sleep(10);
            }
        }
    }
}