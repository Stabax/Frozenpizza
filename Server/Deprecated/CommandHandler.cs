﻿using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrozenPizzaServer
{
  public class CommandHandler
  {
    Dictionary<String, Func<String[], bool>> _commands;
    NetCli _client;

    public CommandHandler(NetCli client)
    {
      _client = client;
      _commands = new Dictionary<String, Func<String[], bool>>();
      _commands.Add("!VERSION", checkVersion);
      _commands.Add("!WHOIS", whoisClient);
      _commands.Add("!STATE", playerState);
      _commands.Add("!FIRE", fireWeapon);
      _commands.Add("!MELEE", meleeHit);
      _commands.Add("!+ITEM", dropItem);
      _commands.Add("!-ITEM", removeItem);
      _commands.Add("?WORLD", sendWorldData);
      _commands.Add("?PLAYERS", sendPlayers);
      _commands.Add(".READY", accept);
    }

    public static String getCmd(String msg)
    {
      if (msg.IndexOf(' ') == -1)
        return (msg);
      return (msg.Substring(0, msg.IndexOf(' ')));
    }

    public static String[] getArgs(String msg)
    {
      String[] args;
      int argc;

      argc = msg.Split(' ').Length - 1;
      args = new String[argc];
      msg = msg.Remove(0, msg.IndexOf(' ') + 1);
      for (int i = 0; i < argc; i++)
      {
        int nextSpace = msg.IndexOf(' ');

        if (nextSpace > 0)
        {
          args[i] = msg.Substring(0, nextSpace);
          msg = msg.Remove(0, nextSpace + 1);
        }
        else
          args[i] = msg;
      }
      return (args);
    }

    public bool ParseCmd(String msg)
    {
      String cmd;
      String[] args;

      if (msg == null)
        return (false);
      cmd = getCmd(msg);
      if (cmd == ".ACK")
        return (true);
      args = getArgs(msg);
      if (!_commands.ContainsKey(cmd))
        return (false);

      _commands[cmd](args);
      return (true);
    }

    public bool ParseExpectedCmd(String msg, String expected)
    {
      if (msg == null)
        return (false);
      String cmd = getCmd(msg);

      if (cmd != expected)
        return (false);
      return (ParseCmd(msg));
    }

    //Handshake
    bool checkVersion(String[] args)
    {
      //No mismatch for now
      accept(null);
      return (true);
    }

    bool whoisClient(String[] args)
    {
      accept(null);
      return (true);
    }

    bool handShake(String[] args)
    {
      _client.send(".HANDSHAKE");
      return (true);
    }

    //Player
    bool playerState(String[] args)
    {
      float x, y;
      float orientation;

      float.TryParse(args[1], out x);
      float.TryParse(args[2], out y);
      float.TryParse(args[3], out orientation);
      Server.ClientList[_client.Id].Player.pos = new PointF(x, y);
      Server.ClientList[_client.Id].Player.orientation = orientation;
      Server.broadcast(_client.Id, "!STATE " + _client.Id + " " + args[1] + " " + args[2] + " " + args[3]);
      return (true);
    }

    bool aimPlayer(String[] args)
    {
      float aim;

      float.TryParse(args[0], out aim);
      Server.ClientList[_client.Id].Player.orientation = aim;
      Server.broadcast(_client.Id, "!AIM " + _client.Id + " " + args[0]);
      return (true);
    }


    //World data
    bool sendWorldData(String[] args)
    {
      Level level = Server.Level;
      for (int i = 0; i < level.Entities.Count; i++)
      {
        _client.send("!++ITEM " + level.Entities[i].Uid + " " + level.Entities[i].Id + " " + level.Entities[i].Pos.X + " " + level.Entities[i].Pos.Y);
        if (!ParseExpectedCmd(_client.receive(), ".ACK"))
          return (false);
      }
      _client.send(".OK");
      _client.send(".READY");
      _client.Ready = true;
      return (true);
    }

    bool sendPlayers(String[] args)
    {
      for (int i = 0; i < Server.ClientList.Count; i++)
      {
        if (Server.ClientList[i] == null || _client.Id == i)
          continue;
        _client.send("!+PLAYER " + Server.ClientList[i].Id + " " + Server.ClientList[i].Player.hp + " " + Server.ClientList[i].Player.pos.X + " " + Server.ClientList[i].Player.pos.Y);
        if (!ParseExpectedCmd(_client.receive(), ".ACK"))
          return (false);
      }
      accept(null);
      return (true);
    }
    bool fireWeapon(String[] args)
    {
      int id, type, damage;
      float angle, velocity;
      PointF firepos;

      Int32.TryParse(args[0], out id);
      Int32.TryParse(args[1], out type);
      float.TryParse(args[2], out angle);
      float.TryParse(args[3], out velocity);
      Int32.TryParse(args[4], out damage);
      firepos = _client.Player.calcFirePos();
      Server.Level.Projectiles.Add(new Projectile((ProjectileType)type, firepos, angle, velocity, damage));
      Server.broadcast(-1, "!+FIRE " + args[0] + " " + args[1] + " " + firepos.X + " " + firepos.Y + " " + args[2] + " " + args[3] + " " + args[4]);
      accept(null);
      return (true);
    }

    bool meleeHit(String[] args)
    {
      float size;
      int damage;

      float.TryParse(args[0], out size);
      Int32.TryParse(args[1], out damage);
      for (int i = 0; i < Server.ClientList.Count; i++)
      {
        if (Server.ClientList[i] == null || Server.ClientList[i].Id == _client.Id)
          continue;
        if (Server.ClientList[i].Player.getHitbox().Contains(Point.Truncate(_client.Player.calcFirePos())))
        {
          Server.ClientList[i].Player.hp -= damage;
          Server.broadcast(-1, "!HIT " + Server.ClientList[i].Id + " " + damage);
        }
      }
      return (true);
    }

    bool dropItem(String[] args)
    {
      Int64 uid;

      Int64.TryParse(args[0], out uid);
      if (Server.Level.getEntityIndex(uid) == -1)
        return (refuse(null));
      Server.Level.Entities[Server.Level.getEntityIndex(uid)].Pos = _client.Player.pos;
      accept(null);
      Server.broadcast(-1, "!+ITEM " + args[0] + " " + _client.Player.pos.X.ToString() + " " + _client.Player.pos.Y.ToString());
      return (true);
    }

    bool removeItem(String[] args)
    {
      Int64 uid;

      Int64.TryParse(args[0], out uid);
      accept(null);
      Server.Level.Entities[Server.Level.getEntityIndex(uid)].Pos = new PointF(-1, -1);
      Server.broadcast(-1, "!-ITEM " + args[0]);
      return (true);
    }

    //Accept / Refuse switches
    bool accept(String[] args)
    {
      _client.send(".OK");
      return (true);
    }

    bool refuse(String[] args)
    {
      _client.send(".KO");
      return (true);
    }
  }
}