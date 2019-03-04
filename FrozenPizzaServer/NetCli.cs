﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FrozenPizzaServer
{
    public class NetCli
    {
        TcpClient _client;
        Thread _cThread;
        NetworkStream _stream;
        CommandHandler _cmdHandle;
        bool _worldSent, _dataSent;
        int _id;
        String _name;
        Player _player;

        //Stack
        Queue<String> _receiveStack;

        public NetCli(TcpClient inClientSocket, int cliId)
        {
            _receiveStack = new Queue<string>();
            _client = inClientSocket;
            _id = cliId;
            _worldSent = false;
            Ready = false;
        }

        public bool Ready { get; set; }

        public int Id
        {
            get { return (_id); }
        }

        public Player Player
        {
            get { return (_player); }
        }

        public void startClient()
        {
            _stream = _client.GetStream();
            _cmdHandle = new CommandHandler(this);
            _cThread = new Thread(Update);
            _cThread.Start();
        }

        public void terminateClient()
        {
            Console.Write(" >> Client ID " + _id + " disconnected.");
            _client.Close();
            Server.ClientList[_id] = null;
        }

        public bool handShake()
        {
            send(".WELCOME");
            if (!_cmdHandle.ParseExpectedCmd(receive(), ".ACK"))
                return (false);
            send("?VERSION");
            if (!_cmdHandle.ParseExpectedCmd(receive(), "!VERSION"))
                return (false);
			_player = new Player(Id, Server.Level.getSpawnLocation());
            send("?WHOIS");
            if (!_cmdHandle.ParseExpectedCmd(receive(), "!WHOIS"))
                return (false);
            send("!MAP " + Server.MapName);
            if (receive() != ".ACK")
                return (false);
            send(".HANDSHAKE");
            if (receive() != ".HANDSHAKE")
                return (false);
            send(".OK");
            Thread.Sleep(1000); //Give client some time to generate.
            return (true); //HandShake success!
        }

        public bool clientInfo()
        {
            send("!PLAYER " + Id + " " + _player.pos.X + " " + _player.pos.Y);
            if (!_cmdHandle.ParseExpectedCmd(receive(), ".ACK"))
                return (false);
            Server.broadcast(Id, "!+PLAYER " + Id + " " + _player.hp + " " + _player.pos.X + " " + _player.pos.Y);
            return (true);
        }

        private void Update()
        {
            if (!handShake() || !clientInfo())
                return;
            while (_client.Connected)
            {
                String msg = receive();

                if (msg != null)
                {
                    _cmdHandle.ParseCmd(msg);
                }
            }
        }

        public void send(String msg)
        {
            byte[] buffer = new byte[msg.Length];
            
            Console.Write(">[" + _id + "] " + msg + "\n");
            buffer = Encoding.UTF8.GetBytes(msg+"\r\n");
            try
            {
                _stream.Write(buffer, 0, buffer.Length);
            }
            catch (System.IO.IOException e)
            {
                terminateClient();
            }
        }

        public String receive()
        {
            int receivingBufferSize = (int)_client.ReceiveBufferSize;
            byte[] buffer = new byte[receivingBufferSize];
            int readCount = 0;
            String msg, msgprocess;

            if (_receiveStack.Count != 0) //If we have loads of messages to treat
            {
                if (_receiveStack.First().IndexOf("!MOVE") == -1 && _receiveStack.First().IndexOf("!AIM") == -1)
                    Console.Write("<[" + _id + "] " + _receiveStack.First() + "\n");
                return (_receiveStack.Dequeue());
            }
            try
            {
                readCount = _stream.Read(buffer, 0, receivingBufferSize);
            }
            catch (System.Exception e)
            {
                terminateClient();
            }
            msg = Encoding.UTF8.GetString(buffer, 0, readCount);
            if (msg == "" || msg.IndexOf("\r\n") == -1)
            {
                terminateClient();
                return (null);
            }
            msg = msg.Substring(0, msg.IndexOf("\r\n"));
            if (msg.IndexOf("\r\n") != msg.Length - 2)
            {
                msgprocess = msg;
                while (msgprocess.IndexOf("\r\n") != -1)
                {
                    msgprocess = msgprocess.Remove(0, msgprocess.IndexOf("\r\n") + 2);
                    _receiveStack.Enqueue(msgprocess.Substring(0, msgprocess.IndexOf("\r\n")));
                }
            }
            if (msg.IndexOf("!MOVE") == -1 && msg.IndexOf("!AIM") == -1)
                Console.Write("<[" + _id + "] " + msg + "\n");
            return (msg);
        }
    }
}
