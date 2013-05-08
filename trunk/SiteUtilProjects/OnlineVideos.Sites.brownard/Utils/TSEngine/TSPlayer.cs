using Deusty.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TSEngine
{
    public delegate void TSEngineHandler(object sender, TSEngineEventArgs e);

    class TSPlayer
    {
        object clientLock = new object();
        AsyncSocket socket = null;
        string installDir;

        string host;
        public string Host
        {
            get { return host; }
        }

        int port = 62062; //apparently the default port;
        public int Port
        {
            get { return port; }
        }

        volatile bool isConnected = false;
        public bool IsConnected
        {
            get { return isConnected; }
        }

        volatile bool isReady = false;
        public bool IsReady 
        { 
            get { return isReady; } 
        }

        string currentUrl = null;
        public string CurrentUrl
        {
            get { lock (clientLock) return currentUrl; }
        }

        public TSPlayer(string host = "127.0.0.1")
        {
            this.host = host;
            socket = new AsyncSocket();
            socket.AllowMultithreadedCallbacks = true;
            socket.WillConnect += new AsyncSocket.SocketWillConnect(socket_WillConnect);
            socket.DidConnect += new AsyncSocket.SocketDidConnect(socket_DidConnect);
            socket.WillClose += new AsyncSocket.SocketWillClose(socket_WillClose);
            socket.DidClose += new AsyncSocket.SocketDidClose(socket_DidClose);
            socket.DidRead += new AsyncSocket.SocketDidRead(socket_DidRead);
            socket.DidWrite += new AsyncSocket.SocketDidWrite(socket_DidWrite);
            installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TorrentStream\\engine");
        }

        public bool Connect(int timeout = 5000)
        {
            if (!isConnected)
            {
                if (Process.GetProcessesByName("tsengine").Length < 1)
                {
                    try
                    {
                        using (Process p = Process.Start(Path.Combine(installDir, "tsengine.exe"))) 
                        { }
                    }
                    catch
                    {
                        return false;
                    }
                }

                string portFile = Path.Combine(installDir, "acestream.port");
                int tries = timeout / 100;
                while (!File.Exists(portFile))
                {
                    tries--;
                    if (tries < 0)
                        return false;
                    Thread.Sleep(100);
                }

                try
                {
                    port = int.Parse(File.ReadAllText(portFile));
                }
                catch
                { } //try default port


                if (!socket.Connect(host, (ushort)port))
                {
                    socket.Close();
                    log("Error connecting to {0}:{1)", host, port);
                    return false;
                }
                isConnected = true;
            }
            return true;
        }

        public bool WaitForReady(int timeout = 5000)
        {
            if (isReady)
                return true;

            int tries = timeout / 10;
            while (!isReady)
            {
                tries--;
                if (tries < 0)
                    break;
                Thread.Sleep(10);
            }
            return isReady;
        }

        public string WaitForUrl(int timeout = 20000)
        {
            if (currentUrl == null)
            {
                int tries = timeout / 10;
                while (currentUrl == null)
                {
                    tries--;
                    if (tries < 0)
                        break;
                    Thread.Sleep(10);
                }
            }
            return currentUrl;
        }

        public void StartPID(string pid)
        {
            if (!isReady)
                return;

            lock (clientLock)
            {
                currentUrl = null;
                writeMessage(string.Format("START PID {0} 0", pid));
            }
        }

        public void StartTorrent(string torrentUrl)
        {
            if (!isReady)
                return;

            lock (clientLock)
            {
                currentUrl = null;
                writeMessage(string.Format("START TORRENT {0} 0 0 0 0", torrentUrl));
            }
        }

        public void StartInfoHash(string infoHash)
        {
            if (!isReady)
                return;
            
            lock (clientLock)
            {
                currentUrl = null;
                writeMessage(string.Format("START INFOHASH {0} 0 0 0 0", infoHash));
            }
        }

        public void SendMessage(string message)
        {
            if (!isReady)
                return;

            writeMessage(message);
        }

        public void Stop()
        {
            if (!isConnected)
                return;

            writeMessage("STOP"); //stop current transfer
        }

        public void Close()
        {
            if (!isConnected)
                return;

            writeMessage("SHUTDOWN"); //close socket connection
            socket.CloseAfterWriting();
        }

        void handleMessage(string message)
        {
            string[] messageParams = message.Split(' ');
            if (messageParams.Length < 1)
                return;
            lock (clientLock)
            {
                switch (messageParams[0])
                {
                    case "START":
                        currentUrl = messageParams[1];
                        if (OnPlaybackReady != null)
                            OnPlaybackReady(this, new TSEngineEventArgs(currentUrl));
                        break;
                    case "PAUSE":
                        if (OnPlaybackPause != null)
                            OnPlaybackPause(this, new TSEngineEventArgs());
                        break;
                    case "RESUME":
                        if (OnPlaybackResume != null)
                            OnPlaybackResume(this, new TSEngineEventArgs());
                        break;
                }
            }
        }

        void writeMessage(string message)
        {
            log("Sending message: {0}", message);
            socket.Write(Encoding.ASCII.GetBytes(message + "\r\n"), 5000, 0);
        }

        void log(string format, params object[] args)
        {
            if (OnMessage != null)
                OnMessage(this, new TSEngineEventArgs(string.Format(format, args)));
        }

        #region Events

        public event TSEngineHandler OnConnected;
        public event TSEngineHandler OnDisconnected;
        public event TSEngineHandler OnPlaybackReady;
        public event TSEngineHandler OnPlaybackPause;
        public event TSEngineHandler OnPlaybackResume;
        public event TSEngineHandler OnMessage;

        #endregion

        #region Socket Events
        void socket_DidWrite(AsyncSocket sender, long tag)
        {
            log("Message sent");
        }

        void socket_DidRead(AsyncSocket sender, byte[] data, long tag)
        {
            string msg = Encoding.UTF8.GetString(data);
            log("Message received: {0}", msg);
            sender.Read(AsyncSocket.CRLFData, -1, 0);
            handleMessage(msg);
        }

        void socket_DidClose(AsyncSocket sender)
        {
            log("Socket closed");
            lock (clientLock)
            {
                isConnected = false;
                isReady = false;
                if (OnDisconnected != null)
                    OnDisconnected(this, new TSEngineEventArgs());
            }
        }

        void socket_WillClose(AsyncSocket sender, Exception e)
        {

        }

        void socket_DidConnect(AsyncSocket sender, System.Net.IPAddress address, ushort port)
        {
            log("Socket connected: {0}:{1}", address, port);
            sender.Read(AsyncSocket.CRLFData, -1, 0);
            writeMessage("HELLOBG");
            writeMessage("READY");
            lock (clientLock)
                isReady = true;

            if (OnConnected != null)
                OnConnected(this, new TSEngineEventArgs());
        }

        static bool socket_WillConnect(AsyncSocket sender, Socket socket)
        {
            return true;
        }
        #endregion
    }

    public class TSEngineEventArgs : EventArgs
    {
        public TSEngineEventArgs(string message = null)
        {
            Message = message;
        }
        public string Message { get; protected set; }
    }
}
