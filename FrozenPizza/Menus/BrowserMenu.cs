﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FrozenPizza
{
    class BrowserMenu : Menu
    {
        Texture2D _colorRect;
        Rectangle _browserPanel;
        public BrowserMenu(Engine engine) : base(engine, "BrowseMenu")
        {
            _colorRect = new Texture2D(engine.GraphicsDevice, 1, 1);
            _colorRect.SetData(new[] { Color.White });
            _browserPanel = new Rectangle(50, 50, _engine.GraphicsDevice.Viewport.Width - 100, (int)(_engine.GraphicsDevice.Viewport.Height * 0.5f));
        }

        public override void itemClicked(int index)
        {
            switch (index)
            {
                case 0:
                    //Connect to selected
                    break;
                case 1:
                    //Add to fav
                    break;
                case 2:
                    //refresh
                    break;
                case 3:
                    _engine.setMenu(new DirectConnectMenu(_engine, this));
                    break;
                case 4:
                    _engine.setMenu(new MainMenu(_engine));
                    break;
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            spriteBatch.Draw(_colorRect, _browserPanel, null, Color.DarkGray * 0.8f, 0f, Vector2.Zero, SpriteEffects.None, 0.1f);
            base.Draw(spriteBatch, graphicsDevice);
        }
    }

    class DirectConnectMenu : Menu
    {
        Texture2D _colorRect;
        BrowserMenu _bmenu;
        String _ip, _port;
        Rectangle _inputBox;

        public DirectConnectMenu(Engine engine, BrowserMenu menu) : base(engine, "DirectConnectMenu")
        {
            _bmenu = menu;
            _ip = "";
            _inputBox = new Rectangle((int)(_engine.GraphicsDevice.Viewport.Width * 0.25f), _engine.GraphicsDevice.Viewport.Height / 2, _engine.GraphicsDevice.Viewport.Width / 2, 40);
            _colorRect = new Texture2D(engine.GraphicsDevice, 1, 1);
            _colorRect.SetData(new[] { Color.White });
        }

        public override void itemClicked(int index)
        {
            switch(index)
            {
                case 0:
                    //Connect
                    break;
                case 1:
                    _engine.setMenu(_bmenu);
                    break;
            }
        }

        public override void Update(KeyboardState[] keybStates, MouseState[] mStates)
        {
            Keys[] prevInput = keybStates[0].GetPressedKeys();
            Keys[] input = keybStates[1].GetPressedKeys();

            if (prevInput.Length == 0 && input.Length > 0)
            {
                for (int i = 0; i < input.Length; i++)
                {
                    if (((char)input[i] >= '0' && (char)input[i] <= '9'))
                        _ip += (char)input[i];
                    else if (input[i] == Keys.OemPeriod)
                        _ip += ".";
                }
            }
            if (_ip.Length > 0 && keybStates[0].IsKeyUp(Keys.Back) && keybStates[1].IsKeyDown(Keys.Back))
                _ip = _ip.Remove(_ip.Length - 1);
            base.Update(keybStates, mStates);
        }

        public override void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            spriteBatch.Draw(_colorRect, _inputBox, null, Color.White * 0.8f, 0f, Vector2.Zero, SpriteEffects.None, 0.2f);
            spriteBatch.DrawString(_font, _ip, _inputBox.Location.ToVector2(), Color.Black);
            base.Draw(spriteBatch, graphicsDevice);
        }
    }
}
