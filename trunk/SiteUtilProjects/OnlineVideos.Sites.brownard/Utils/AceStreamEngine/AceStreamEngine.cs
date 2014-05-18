using Deusty.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace OnlineVideos.Sites.Utils.Brownard.AceStream
{
    #region AceStreamEventArgs

    public class AceStreamEventArgs : EventArgs
    {
        public AceStreamEventArgs(string message = null)
        {
            Message = message;
        }
        public string Message { get; protected set; }
    }

    #endregion

    #region AceStreamMessage

    public class AceStreamMessage
    {
        public static AceStreamMessage Create(string message)
        {
            string[] messageSplit = message.Split(' ');
            List<string> arguments = new List<string>();
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            for (int x = 1; x < messageSplit.Length; x++)
            {
                string[] paramSplit = messageSplit[x].Split('=');
                if (paramSplit.Length == 1)
                    arguments.Add(paramSplit[0]);
                else
                    parameters[paramSplit[0]] = paramSplit[1];
            }
            return new AceStreamMessage(messageSplit[0], arguments, parameters);
        }

        string message;
        List<string> arguments;
        Dictionary<string, string> parameters;
        private AceStreamMessage(string message, List<string> arguments, Dictionary<string, string> parameters)
        {
            this.message = message;
            this.arguments = arguments;
            this.parameters = parameters;
        }

        public string Message { get { return message; } }

        public string this[int index]
        {
            get
            {
                if (arguments.Count > index)
                    return arguments[index];
                return null;
            }
        }

        public string this[string key]
        {
            get
            {
                string value;
                if (!parameters.TryGetValue(key, out value))
                    value = null;
                return value;
            }
        }
    }

    #endregion

    class AceStreamEngine
    {
        #region Consts

        const string API_KEY = "n51LvQoTlJzNGaFxseRK-uvnvX-sD4Vm5Axwmc4UcoD-jruxmKsuJaH0eVgE";
        const int DEFAULT_PORT = 62062; //apparently the default port

        #endregion

        #region Variables

        object clientLock = new object();
        AsyncSocket socket = null;
        string installDir;
        string enginePath;

        bool isClosed;
        ManualResetEvent engineReadyEvent;
        ManualResetEvent urlReadyEvent;

        #endregion

        #region Events

        public event EventHandler<AceStreamEventArgs> OnConnected;
        public event EventHandler<AceStreamEventArgs> OnDisconnected;
        public event EventHandler<AceStreamEventArgs> OnPlaybackReady;
        public event EventHandler<AceStreamEventArgs> OnPlaybackPause;
        public event EventHandler<AceStreamEventArgs> OnPlaybackResume;
        public event EventHandler<AceStreamEventArgs> OnMessage;

        #endregion

        #region Properties

        string host;
        public string Host
        {
            get { return host; }
        }

        int port = DEFAULT_PORT;
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

        #endregion

        #region Ctor

        public AceStreamEngine(string host = "127.0.0.1")
        {
            this.host = host;

            engineReadyEvent = new ManualResetEvent(false);
            urlReadyEvent = new ManualResetEvent(false);

            socket = new AsyncSocket();
            socket.AllowMultithreadedCallbacks = true;
            socket.WillConnect += new AsyncSocket.SocketWillConnect(socket_WillConnect);
            socket.DidConnect += new AsyncSocket.SocketDidConnect(socket_DidConnect);
            socket.WillClose += new AsyncSocket.SocketWillClose(socket_WillClose);
            socket.DidClose += new AsyncSocket.SocketDidClose(socket_DidClose);
            socket.DidRead += new AsyncSocket.SocketDidRead(socket_DidRead);
            socket.DidWrite += new AsyncSocket.SocketDidWrite(socket_DidWrite);
        }

        #endregion

        #region Socket Event Handlers

        void socket_DidConnect(AsyncSocket sender, System.Net.IPAddress address, ushort port)
        {
            log("Socket connected: {0}:{1}", address, port);
            sender.Read(AsyncSocket.CRLFData, -1, 0);
            writeMessage("HELLOBG");
        }

        void socket_DidWrite(AsyncSocket sender, long tag)
        {
            log("Message sent");
        }

        void socket_DidRead(AsyncSocket sender, byte[] data, long tag)
        {
            lock (clientLock)
            {
                if (isClosed)
                    return;

                string msg = Encoding.UTF8.GetString(data).Trim();
                log("Message received: {0}", msg);
                AceStreamMessage tsMessage = AceStreamMessage.Create(msg);
                sender.Read(AsyncSocket.CRLFData, -1, 0);
                handleMessage(tsMessage);
            }
        }

        void socket_DidClose(AsyncSocket sender)
        {
            log("Socket closed");
            lock (clientLock)
            {
                isConnected = false;
                isReady = false;
                if (OnDisconnected != null)
                    OnDisconnected(this, new AceStreamEventArgs());
            }
        }

        void socket_WillClose(AsyncSocket sender, Exception e)
        {

        }

        static bool socket_WillConnect(AsyncSocket sender, Socket socket)
        {
            return true;
        }

        #endregion

        #region Public Methods

        public bool Connect(int timeout = 5000)
        {
            if (!isConnected)
            {
                lock (clientLock)
                {
                    setInstallationDirectory();
                    if (string.IsNullOrEmpty(enginePath))
                        return false;

                    if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(enginePath)).Length == 0)
                    {
                        try
                        {
                            using (Process p = Process.Start(enginePath)) { }
                        }
                        catch (Exception ex)
                        {
                            log("Failed to start '{0}' - {1}", enginePath, ex.Message);
                            return false;
                        }
                    }
                    else
                    {
                        log("AceStream already running");
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
                        log("Using port {0}", port);
                    }
                    catch (Exception ex)
                    {
                        log("Failed to get port from '{0}' - {1}", portFile, ex.Message);
                        log("Using default port {0}", DEFAULT_PORT);
                        port = DEFAULT_PORT; //try default port
                    }

                    if (!socket.Connect(host, (ushort)port))
                    {
                        socket.Close();
                        log("Error connecting to {0}:{1)", host, port);
                        return false;
                    }
                    isConnected = true;
                }
            }
            return true;
        }

        public bool WaitForReady(int timeout = 5000)
        {
            if (!isReady)
                engineReadyEvent.WaitOne(timeout);
            log("Ready: {0}", isReady);
            return isReady;
        }

        public string WaitForUrl(int timeout = 20000)
        {
            if (!isReady)
                return null;

            if (currentUrl == null)
                urlReadyEvent.WaitOne(timeout);
            log("Url: '{0}'", currentUrl);
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
            lock (clientLock)
            {
                if (!isClosed)
                {
                    engineReadyEvent.Close();
                    urlReadyEvent.Close();
                    isClosed = true;
                }
                if (isConnected)
                    socket.Close();

                try
                {
                    Process[] ps = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(enginePath));
                    if (ps.Length > 0)
                        ps[0].Kill();
                }
                catch (Exception ex)
                {
                    log("Error closing AceStreamEngine - {0}", ex.Message);
                }
                try
                {
                    File.Delete(Path.Combine(installDir, "acestream.port"));
                }
                catch (Exception ex)
                {
                    log("Error deleting port file - {0}", ex.Message);
                }
                log("Closed");
            }
        }

        #endregion

        #region Private Methods
        
        void setInstallationDirectory()
        {
            installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ACEStream\\engine");
            enginePath = Path.Combine(installDir, "ace_engine.exe");
            if (File.Exists(enginePath))
            {
                log("Found AceStream installation at '{0}'", installDir);
                return;
            }

            //Old install path
            installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TorrentStream\\engine");
            enginePath = Path.Combine(installDir, "tsengine.exe");
            if (File.Exists(enginePath))
            {
                log("Found AceStream installation at '{0}'", installDir);
                return;
            }

            log("No AceStream installations were found");
            installDir = null;
            enginePath = null;
        }

        void handleMessage(AceStreamMessage message)
        {
            switch (message.Message)
            {
                case "HELLOTS":
                    string key = message["key"];
                    if (!string.IsNullOrEmpty(key))
                    {
                        string responseKey = createResponseKey(message["key"]);
                        writeMessage("READY key=" + responseKey);
                    }
                    else
                    {
                        writeMessage("READY");
                    }
                    break;
                case "READY":
                case "AUTH":
                    isReady = true;
                    engineReadyEvent.Set();
                    if (OnConnected != null)
                        OnConnected(this, new AceStreamEventArgs());
                    break;
                case "START":
                    currentUrl = message[0];
                    urlReadyEvent.Set();
                    if (OnPlaybackReady != null)
                        OnPlaybackReady(this, new AceStreamEventArgs(currentUrl));
                    break;
                case "PAUSE":
                    if (OnPlaybackPause != null)
                        OnPlaybackPause(this, new AceStreamEventArgs());
                    break;
                case "RESUME":
                    if (OnPlaybackResume != null)
                        OnPlaybackResume(this, new AceStreamEventArgs());
                    break;
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
                OnMessage(this, new AceStreamEventArgs("AceStream: " + string.Format(format, args)));
        }

        #endregion

        #region Static Methods

        static string createResponseKey(string requestKey)
        {
            SHA1 sha1 = SHA1.Create();
            byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(requestKey + API_KEY));
            return API_KEY.Split('-')[0] + "-" + hexStringFromBytes(hash);
        }

        static string hexStringFromBytes(byte[] bytes)
        {
            string addressString = "";
            if (bytes == null || bytes.Length == 0)
                return addressString;

            for (int x = 0; x < bytes.Length; x++)
                addressString += bytes[x].ToString("x2");
            return addressString;
        }

        #endregion
    }
}
