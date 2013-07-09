using System;
using System.IO;

namespace OnlineVideos
{
    public class MMSDownloader : MarshalByRefObject, IDownloader
    {
        #region MarshalByRefObject overrides
        public override object InitializeLifetimeService()
        {
            // In order to have the lease across appdomains live forever, we return null.
            return null;
        }
        #endregion

        bool connectionEstablished = false;
        DownloadInfo downloadInfo;
        System.Threading.Thread downloadThread;

        long TotalFileSize
        {
            set
            {
                if (downloadInfo != null) downloadInfo.DownloadProgressCallback(value, 0);
            }
        }

        long CurrentBytesDownloaded
        {
            set
            {
                if (downloadInfo != null) downloadInfo.DownloadProgressCallback(0, value);
            }
        }

        byte PercentDownloaded
        {
            set
            {
                if (downloadInfo != null) downloadInfo.DownloadProgressCallback(value);
            }
        }

        public bool Cancelled { get; private set; }

        public void CancelAsync() { Cancelled = true; }

        public void Abort()
        {
            Cancelled = true;
            if (downloadThread != null) downloadThread.Abort();
        }

        public Exception Download(DownloadInfo downloadInfo)
        {
            downloadThread = System.Threading.Thread.CurrentThread;
            this.downloadInfo = downloadInfo;
            try
            {
                MMSDownload(downloadInfo.Url, downloadInfo.LocalFile);
            }
            catch (Exception ex)
            {
                if (!connectionEstablished && !Cancelled)
                {
                    try
                    {
                        HTTPMMSDownload(downloadInfo.Url, downloadInfo.LocalFile);
                    }
                    catch (Exception ex2)
                    {
                        return ex2;
                    }
                }
                else
                {
                    return ex;
                }
            }
            return null;
        }

        string StringHereToHere(string here, string here1, string here2)
        {
            return here.Substring(here.IndexOf(here1) + here1.Length, here.IndexOf(here2, here.IndexOf(here1) + here1.Length) - here.IndexOf(here1) - here1.Length);
        }

        string StringEndToEnd(string here, string here1, string here2)
        {
            return here.Substring(here.LastIndexOf(here1) + here1.Length, here.LastIndexOf(here2) - here.LastIndexOf(here1) - here1.Length);
        }

        string HexString(byte[] b, int l, int offset)
        {
            int i = 0;
            if (l == 0)
                l = b.Length;
            string str = "";
            for (i = offset; i <= l - 1 + offset; i++)
            {
                str = str + " " + Convert.ToString(b[i], 16);
            }
            return str;
        }

        void InitiateSession(System.Net.Sockets.Socket sd, string @base)
        {
            string Command = "NSPlayer/9.0.0.2980; {" + Guid.NewGuid().ToString() + "}; Host: " + @base;
            byte[] P1B1 = { 0xf0, 0xf0, 0xf0, 0xf0, 0xb, 0x0, 0x4, 0x0, 0x1c, 0x0, 0x3, 0x0 };
            byte[] P1B2 = Pad0(System.Text.ASCIIEncoding.ASCII.GetBytes(Command), 6);
            sd.Send(HPacket(0x1, P1B1, P1B2));
        }

        void SendTimingTest(System.Net.Sockets.Socket sd)
        {
            byte[] P2B1 = { 0xf1, 0xf0, 0xf0, 0xf0, 0xb, 0x0, 0x4, 0x0 };
            sd.Send(HPacket(0x18, P2B1, null));
        }

        void RequestConnection(System.Net.Sockets.Socket sd)
        {
            string Command3 = "\\\\" + sd.LocalEndPoint.ToString().Replace(":", "\\TCP\\");
            Command3 = "\\\\" + StringHereToHere(Command3, "\\\\", "\\TCP\\") + "\\TCP\\1755";
            byte[] P3B1 = { 0xf1, 0xf0, 0xf0, 0xf0, 0xff, 0xff, 0xff, 0xff, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0xa0, 0x0, 0x2, 0x0, 0x0, 0x0 };
            byte[] P3B2 = Pad0(System.Text.ASCIIEncoding.ASCII.GetBytes(Command3), 2);
            sd.Send(HPacket(0x2, P3B1, P3B2));
        }

        void RequestFile(System.Net.Sockets.Socket sd, string rest)
        {
            byte[] P4B1 = { 0x1, 0x0, 0x0, 0x0, 0xff, 0xff, 0xff, 0xff, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };
            byte[] P4B2 = Pad0(System.Text.ASCIIEncoding.ASCII.GetBytes(rest), 0);
            sd.Send(HPacket(0x5, P4B1, P4B2));
        }

        void MMSDownload(string Url, string path)
        {
            string @base = null;
            string rest = null;
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            @base = Url.Replace("mms://", "");
            @base = @base.Substring(0, @base.IndexOf("/"));
            rest = Url.Replace("mms://" + @base + "/", "");
            rest = rest.Replace(" ", "%20");
            Log.Debug("MMSDownloader : Resolving IP address...");
            System.Net.Sockets.Socket sd = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            System.Net.IPEndPoint dip = new System.Net.IPEndPoint(System.Net.Dns.GetHostEntry(@base).AddressList[0], 1755);
            Log.Debug("MMSDownloader : Connecting...");
            sd.Connect(dip);
            Log.Debug("MMSDownloader : Connected.");
            Log.Debug("MMSDownloader : Initializing Session...");
            InitiateSession(sd, @base);
            CommandCheck(sd, 0x1);
            Log.Debug("MMSDownloader : Session Established. Sending 1st Network Timing Test...");
            SendTimingTest(sd);
            CommandCheck(sd, 0x15);
            Log.Debug("MMSDownloader : Session Established. Sending 2nd Network Timing Test...");
            SendTimingTest(sd);
            CommandCheck(sd, 0x15);
            Log.Debug("MMSDownloader : Session Established. Sending 3rd Network Timing Test...");
            SendTimingTest(sd);
            CommandCheck(sd, 0x15);
            Log.Debug("MMSDownloader : Network Timing Test Successful. Requesting TCP Connection...");
            RequestConnection(sd);
            CommandCheck(sd, 0x2);
            Log.Debug("MMSDownloader : TCP Connection Accepted. Requesting File " + rest);
            RequestFile(sd, rest);
            byte[] b = CommandCheck(sd, 0x6);
            Log.Debug("MMSDownloader : File Request Accepted.");
            string t = HexM(b[71]) + HexM(b[70]) + HexM(b[69]) + HexM(b[68]) + HexM(b[67]) + HexM(b[66]) + HexM(b[65]) + HexM(b[64]);
            int psize = b[92] + b[93] * 256 + b[94] * 4096;
            int nps = b[96] + b[97] * 256 + b[98] * 4096;
            int hsize = b[108] + b[109] * 256 + b[110] * 4096;
            int fsize = psize * nps + hsize;
            decimal time = (DoublePercisionHex(t) - 3.6M);
            connectionEstablished = true;
            TotalFileSize = fsize;
            Log.Debug("MMSDownloader : File Size: " + fsize + "Bytes" + Environment.NewLine + "Media Length: " + time + "Seconds" + Environment.NewLine + "Packet Size: " + psize + "Bytes" + Environment.NewLine + "Header Size: " + hsize + "Bytes" + Environment.NewLine + "Number of Packets: " + nps + "Packets");
            WriteStream(sd, path, time, hsize);
            Log.Debug("MMSDownloader : Done!");
        }

        void HTTPMMSDownload(string Url, string path)
        {
            string @base = null;
            @base = Url.Replace("mms://", "");
            @base = @base.Substring(0, @base.IndexOf("/"));
            System.Net.Sockets.Socket s = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            System.Net.IPEndPoint ip = new System.Net.IPEndPoint(System.Net.Dns.GetHostEntry(@base).AddressList[0], 80);
            Log.Debug("MMSDownloader : Connecting...");
            s.Connect(ip);
            Log.Debug("MMSDownloader : Connected. Sending Get Request...");
            string getfile = null;
            getfile = Url.Replace("mms://" + @base, "");
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            getfile = "GET " + getfile + " HTTP/1.1" + Environment.NewLine + "Accept: */*" + Environment.NewLine + "User-Agent: NSPlayer/9.0.0.2980" + Environment.NewLine + "Host: " + @base + Environment.NewLine + "Pragma: no-cache,rate=1.000000,stream-time=0" + Environment.NewLine + "Pragma: xPlayStrm=1" + Environment.NewLine + "Pragma: xClientGUID={" + Guid.NewGuid().ToString() + "}" + Environment.NewLine + Environment.NewLine;
            byte[] bytes = enc.GetBytes(getfile);
            s.Send(bytes, bytes.Length, System.Net.Sockets.SocketFlags.None);
            int n = 0;
            byte[] recbytes = new byte[1024];
            string hinfo = "";
            int rbytes = 0;
            Log.Debug("MMSDownloader : Recieving Command Header...");
            do
            {
                n = s.Receive(recbytes, 1, System.Net.Sockets.SocketFlags.None);
                rbytes = rbytes + n;
                hinfo = hinfo + enc.GetString(recbytes, 0, n);
                //Log.Debug("MMSDownloader : Recieving Command Header..." + rbytes + "Bytes Recieved...");
                if (hinfo.Contains(Environment.NewLine + Environment.NewLine) == true)
                    break;
            } while (true);
            byte[] tbytes = new byte[10001];
            if (hinfo.Contains("Busy") == true)
            {
                Log.Debug("MMSDownloader : Session Not Ready. Retry in 10 seconds..."); return;
            }
            Log.Debug("MMSDownloader : Header Recieved...Recieving Data...");
            int i = 0;
            int n1 = 0;
            int cur = 0;
            using (System.IO.FileStream fs = new System.IO.FileStream(path, System.IO.FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bs = new byte[8];
                int p = 0;
                int np = 0;
                int rp = 0;
                Guid y = new Guid("75B22630-668E-11CF-A6D9-00AA0062CE6C");
                n1 = 0;
                do
                {
                    n = s.Receive(tbytes, n1, 16, System.Net.Sockets.SocketFlags.None);
                    n1 = n1 + n;
                    //Log.Debug("MMSDownloader : Sorting Header..." + n1 + "Bytes Recieved...");
                } while (!(Find(y.ToByteArray(), tbytes) > 0));
                Array[] x = SortOutHeader(tbytes, n1);
                connectionEstablished = true;
                fs.Write((byte[])x[0], 0, ((byte[])x[0]).Length);
                bs = (byte[])x[1];
                hinfo = HexString(bs, 0, 0);
                i = bs[6] + bs[7] * (256) - 8 - x[0].Length;
                cur = cur + i + x[0].Length;
            more:
                Log.Debug("MMSDownloader : Header Size: " + cur + " Bytes");
                byte[] header = new byte[i];
                n1 = 0;
                while (!(n1 == i))
                {
                    n = s.Receive(header, n1, i - n1, 0);
                    n1 = n1 + n;
                }
                if (p == 0)
                    p = GetPacketLength(header);
				Log.Debug("MMSDownloader : Packet Size: " + p + " Bytes");
                if (np == 0)
                    np = GetNumberOfPackets(header);
				Log.Debug("MMSDownloader : Total Packets to retrieve: " + np);
                fs.Write(header, 0, header.Length);
                if (bs[5] == 0x4 | bs[5] == 0x8)
                {
                    n1 = 0;
                    while (!(n1 == 8))
                    {
                        n = s.Receive(bs, n1, 8 - n1, 0);
                        n1 = n1 + n;
                    }
                    hinfo = HexString(bs, 0, 0);
                    i = bs[6] + bs[7] * (256) - 8;
                    cur = cur + i;
                    goto more;
                }
                bs = new byte[12];
                n1 = 0;
                while (!(n1 == 12))
                {
                    n = s.Receive(bs, n1, 12 - n1, 0);
                    n1 = n1 + n;
                }
                i = bs[10] + bs[11] * (256) - 8;
                rp = 0;
                do
                {
                    rp = rp + 1;
                    byte[] buffer = new byte[p];
                    n1 = 0;
                    while (!(n1 == i))
                    {
                        n = s.Receive(buffer, n1, i - n1, 0);
                        n1 = n1 + n;
                    }
                    fs.Write(buffer, 0, p);
                    cur = cur + p;
                    PercentDownloaded = (byte)((float)rp / np * 100f);
                    CurrentBytesDownloaded = cur;
                    //Log.Debug("MMSDownloader : Recieving Packets. Packet Size Is " + p + "." + Environment.NewLine + "Recieved " + rp + " Packets Out Of " + np + "." + Environment.NewLine + "Downloaded So Far " + cur + "Bytes.");
                    n1 = 0;
                    while (n1 != 12)
                    {
                        n = s.Receive(bs, n1, 12 - n1, 0);
                        if ((n == 0 && rp >= np) || Cancelled) break;
                        n1 = n1 + n;
                    }
                    //Console.WriteLine(HexString(bs, 0, 0))
                    //Console.WriteLine(Convert(bs))
                    i = bs[10] + bs[11] * (256) - 8;
                    //Console.WriteLine(i)
                    //n1 = 0
                    //Do Until n1 = 8
                    //    n = s.Receive(bs, n1, 8 - n1, 0)
                    //    n1 = n1 + n
                    //Loop
                    //Console.WriteLine(HexString(bs, 0, 0))
                } while (n != 0 && !Cancelled);
                n = s.Receive(bs, 1, 0);
                if (n > 0)
                    Log.Debug("MMSDownloader : MORE!");
            }
        }

        void WriteStream(System.Net.Sockets.Socket sd, string path, decimal time, int hsize)
        {
            int p = 0;
            int np = 0;
            long rp = 0;
            System.IO.FileStream fs = null;
            bool fe = System.IO.File.Exists(path);
            fs = new System.IO.FileStream(path, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite, FileShare.None);
            byte[] P5B1 = { 0x1, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x80, 0x0, 0x0, 0xff, 0xff, 0xff, 0xff, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x20, 0xac, 0x40, 0x2, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };
            byte[] rb = null;
            if (fe == true)
            {
                rb = new byte[fs.Length];
                fs.Read(rb, 0, rb.Length);
                p = GetPacketLength(rb);
                np = GetNumberOfPackets(rb);
                rp = (fs.Length - hsize) / p;
                decimal rtime = default(decimal);
                rtime = ((rp) / np) * time;
                byte[] br = HexDoublePercision(rtime);
                Array.ConstrainedCopy(br, 0, P5B1, 8, 8);
            }
            Log.Debug("MMSDownloader : Requesting Media Header...");
            sd.Send(HPacket(0x15, P5B1, null));
            int n = 0;
            int n1 = 0;
            byte[] bs = new byte[8];
            int i = 0;
            int cur = 0;
            int sp = 0;
            byte[] b = ReturnB(sd);
            if (fe == true)
                fs.Position = 0;
            if (b[36] == 0x11)
            {
            more:
                n1 = 0;
                while (!(n1 == 8))
                {
                    n = sd.Receive(bs, n1, 8 - n1, 0);
                    n1 = n1 + n;
                }
                Array.ConstrainedCopy(bs, 0, b, 0, n);
                i = bs[6] + bs[7] * (256) - 8;
                string s = HexString(bs, 0, 0);
                cur = cur + i;
                Log.Debug("MMSDownloader : Header Size: " + cur + "Bytes");
                byte[] header = new byte[i];
                n1 = 0;
                while (!(n1 == i))
                {
                    n = sd.Receive(header, n1, i - n1, 0);
                    n1 = n1 + n;
                }
                if (fe == true)
                {
                    if (Find(header, rb) == -1)
                        Log.Debug("MMSDownloader : ERROR! Files Dont Match!");
                    //: Exit Sub
                }
                if (p == 0)
                    p = GetPacketLength(header);
                if (np == 0)
                    np = GetNumberOfPackets(header);
                fs.Write(header, 0, header.Length);
                if (bs[5] == 0x4 | bs[5] == 0x8)
                {
                    goto more;
                }
            }
            if (fe == true)
                fs.Position = fs.Length - 1;
            Log.Debug("MMSDownloader : Header Recieved And Written...Requesting Media...");
            byte[] P6B1 = { 0x1, 0x0, 0x0, 0x0, 0xff, 0xff, 0x1, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };
            sd.Send(HPacket(0x7, P6B1, null));
            byte[] b2 = ReturnB(sd);
            cur = 0;
            if (b2[36] == 0x21)
            {
                byte[] b3 = ReturnB(sd);
                if (b3[36] == 0x5)
                {
                    n1 = 0;
                    while (!(n1 == 8))
                    {
                        n = sd.Receive(bs, n1, 8 - n1, 0);
                        n1 = n1 + n;
                    }
                    i = bs[6] + bs[7] * (256);
                    i = i - 8;
                    string s = null;
                    s = HexString(bs, 8, 0);
                    byte[] buffer = new byte[p];
                    do
                    {
                        n1 = 0;
                        while (!(n1 == i))
                        {
                            n = sd.Receive(buffer, n1, i - n1, 0);
                            n1 = n1 + n;
                        }
                        if (fe == true)
                        {
                            if (Find(buffer, rb) > -1)
                            {
                                sp = sp + 1;
                                Log.Debug("MMSDownloader : skipped: " + sp);
                                goto skip;
                            }
                            else
                            {
                                fe = false;
                            }
                        }
                        s = HexString(buffer, 0, 0);
                        if (s.Contains("1B 0 4 0") == true)
                            Log.Debug("MMSDownloader : !!!!");
                        fs.Write(buffer, 0, p);
                        cur = cur + p;
                        PercentDownloaded = (byte)((float)rp / np * 100f);
                        CurrentBytesDownloaded = cur;
                        Log.Debug("MMSDownloader : Recieving Packets. Packet Size Is " + p + "." + Environment.NewLine + "Recieved " + rp + " Packets Out Of " + np + "." + Environment.NewLine + "Downloaded So Far " + cur + "Bytes.");
                    skip:
                        n1 = 0;
                        int b1 = 0;
                        while (!(n1 == 8))
                        {
                            n = sd.Receive(bs, n1, 8 - n1, 0);
                            if (n == 0)
                                b1 = b1 + 1;
                            if (b1 > 0)
                                Log.Debug("MMSDownloader : " + b1);
                            n1 = n1 + n;
                        }
                        string s1 = HexString(bs, 0, 0);
                        if (s1.Contains("CE FA B B0") == true)
                        {
                            do
                            {
                                int x = ReturnB2(sd);
                                if (x == 1)
                                {
                                    Log.Debug("MMSDownloader : Download Is Complete!"); return;
                                }
                                if (x == 2)
                                {
                                    Log.Debug("MMSDownloader : Sending Network Timing Test..");
                                    TTTest(sd);
                                    n1 = 0;
                                    while (!(n1 == 8))
                                    {
                                        n = sd.Receive(bs, n1, 8 - n1, 0);
                                        n1 = n1 + n;
                                    }
                                }
                            } while (!(HexString(bs, 8, 0).Contains("CE FA B B0") == false));
                        }
                        i = bs[6] + bs[7] * (256) - 8;
                        rp = rp + 1;
                    } while (true);
                }
            }
        }

        string HexM(int i)
        {
            string str = Convert.ToString(i, 16);
            if (str.Length < 2)
                str = "0" + str;
            return str;
        }

        decimal DoublePercisionHex(string str)
        {
            string strFull = HexToBinary64BitCalculate(str);
            string S = strFull.Substring(0, 1);
            string E = strFull.Substring(1, 11);
            string F = strFull.Substring(12, 52);
            if (S + E + F != strFull)
                Log.Debug("MMSDownloader : Calculation Error!");
            F = "1." + F;
            int exp = (int)BinaryCalculate(E) - 1023;
            if (exp > 2047 | exp < 0)
                Log.Debug("MMSDownloader : Calculation Error!");
            decimal d = BinaryCalculate(F);
            exp = (int)Math.Pow(2, exp);
            return exp * d;
        }

        byte[] HexDoublePercision(decimal d)
        {
            string str = "";
            string s = "0";
            int exp = 0;
            while (!((decimal)Math.Pow(2, (exp + 1)) > d))
            {
                exp = exp + 1;
            }
            string e = ZM(BinaryConvert((exp + 1023).ToString()), 11);
            decimal df = default(decimal);
            df = (d / ((decimal)Math.Pow(2, exp))) - 1;
            string f = BinaryConvert(df.ToString());
            f = f.Substring(f.IndexOf(".") + 1, 52);
            str = s + e + f;
            string hs = Convert.ToString(BinaryICalculate(str), 16);
            while (!(hs.Length == 16))
            {
                hs = "0" + hs;
            }
            byte[] bs = new byte[8];
            bs[0] = Convert.ToByte("0x" + hs.Substring(14, 2), 16);
            bs[1] = Convert.ToByte("0x" + hs.Substring(12, 2), 16);
            bs[2] = Convert.ToByte("0x" + hs.Substring(10, 2), 16);
            bs[3] = Convert.ToByte("0x" + hs.Substring(8, 2), 16);
            bs[4] = Convert.ToByte("0x" + hs.Substring(6, 2), 16);
            bs[5] = Convert.ToByte("0x" + hs.Substring(4, 2), 16);
            bs[6] = Convert.ToByte("0x" + hs.Substring(2, 2), 16);
            bs[7] = Convert.ToByte("0x" + hs.Substring(0, 2), 16);
            return bs;
        }

        private string HexToBinary64BitCalculate(string str)
        {
            int n = 0;
            Int64 r = 0;
            Int64 i = 0;
            i = Convert.ToInt64("&h" + str);
            string @out = "0";
            n = 63;
            while (!(n == 0))
            {
                n = n - 1;
                r = Convert.ToInt64(Math.Pow(2, n));
                if (i - r >= 0) { @out = @out + "1"; i = i - r; continue; }
                if (i - r < 0)
                    @out = @out + "0";
            }
            return @out;
        }

        private decimal BinaryCalculate(string str)
        {
            decimal i = 0;
            Int64 n = 0;
            string strp = str;
            string strn = "";
            if (str.Contains(".")) { strp = str.Substring(0, str.IndexOf(".")); strn = str; }
            if (str.Contains("."))
                strn = strn.Substring(str.IndexOf(".") + 1);
            while (!(strp.Length == 0))
            {
                i = i + (decimal.Parse(strp.Substring(strp.Length - 1), System.Globalization.CultureInfo.InvariantCulture) * (decimal)Math.Pow(2, n));
                n = n + 1;
                strp = strp.Substring(0, strp.Length - 1);
            }
            n = 0;
            while (!(strn.Length == 0))
            {
                n = n - 1;
                i = i + (decimal.Parse(strn.Substring(0, 1), System.Globalization.CultureInfo.InvariantCulture) * (decimal)Math.Pow(2, n));
                strn = strn.Substring(1);
            }
            return i;
        }

        Int64 BinaryICalculate(string str)
        {
            Int64 i = 0;
            int n = 0;
            Int64 mp = 1;
            string strp = str;
            for (n = str.Length - 1; n >= 1; n += -1)
            {
                if (str[n] == '1')
                    i = i + mp;
                if (n == 1)
                    break;
                mp = mp * 2;
            }
            return i;
        }

        private string ZM(string str, int n)
        {
            str = str.Substring(0, str.IndexOf("."));
            if (str.Length > n)
                Log.Debug("MMSDownloader : !!!!111");
            while (!(str.Length == n))
            {
                str = "0" + str;
            }
            return str;
        }

        private string BinaryConvert(string dec)
        {
            string strp = dec;
            string strn = "";
            if (dec.Contains(".")) { strp = dec.Substring(0, dec.IndexOf(".")); strn = dec; }
            if (dec.Contains("."))
                strn = strn.Substring(dec.IndexOf(".") + 1);
            int pi = int.Parse(strp);
            decimal ni = decimal.Parse("0." + strn, System.Globalization.CultureInfo.InvariantCulture);
            int n = 63;
            strp = "";
            strn = ".";
            do
            {
                if (Math.Pow(2, n) > pi) { n = n - 1; strp = strp + "0"; continue; }
                pi = pi - (int)Math.Pow(2, n);
                strp = strp + "1";
                n = n - 1;
            } while (!(n == -1));
            while (!(n == -63))
            {
                if ((decimal)Math.Pow(2, n) > ni) { n = n - 1; strn = strn + "0"; continue; }
                ni = ni - (decimal)Math.Pow(2, n);
                strn = strn + "1";
                n = n - 1;
            }
            string str = strp + strn;
            while (str[0] == '0')
            {
                str = str.Substring(1);
            }
            return str;
        }

        bool CheckQueryAtOffset(byte[] Query, byte[] Pool, int PoolOffset)
        {
            byte[] b1 = Query;
            byte[] b2 = new byte[b1.Length];
            Array.ConstrainedCopy(Pool, PoolOffset, b2, 0, b1.Length);
            int i = 0;
            for (i = 0; i <= b1.Length - 1; i++)
            {
                if (b1[i] != b2[i]) return false;
            }
            return true;
        }

        int Find(byte[] Query, byte[] Pool)
        {
            int i = 0;
            for (i = 0; i <= Pool.Length - Query.Length - 1; i++)
            {
                if (Pool[i] == Query[0])
                {
                    if (CheckQueryAtOffset(Query, Pool, i) == true) return i;
                }
            }
            return -1;
        }

        Array[] SortOutHeader(byte[] b, int n1)
        {
            Array[] x = new Array[2];
            Guid y = new Guid("75B22630-668E-11CF-A6D9-00AA0062CE6C");
            int pos = Find(y.ToByteArray(), b);
            byte[] c = new byte[n1 - pos];
            Array.ConstrainedCopy(b, pos, c, 0, n1 - pos);
            x[0] = c;
            byte[] bs = new byte[8];
            Array.ConstrainedCopy(b, pos - 8, bs, 0, 8);
            x[1] = bs;
            return x;
        }

        int GetPacketLength(byte[] header)
        {
            Guid y = new Guid("8CABDCA1-A947-11CF-8EE4-00C00C205365");
            byte[] b = y.ToByteArray();
            int i = 0;
            int pos = 0;
            for (i = 0; i <= header.Length - 17; i++)
            {
                if (header[i] == b[0])
                {
                    if (CheckByteArrays(b, header, i) == true)
                    {
                        pos = i; break;
                    }
                }
            }
            pos = pos + 92;
            int psize = header[pos] + header[pos + 1] * 256 + header[pos + 2] * 4096;
            return psize;
        }

        int GetNumberOfPackets(byte[] header)
        {
            Guid y = new Guid("8CABDCA1-A947-11CF-8EE4-00C00C205365");
            byte[] b = y.ToByteArray();
            int i = 0;
            int pos = 0;
            for (i = 0; i <= header.Length - 17; i++)
            {
                if (header[i] == b[0])
                {
                    if (CheckByteArrays(b, header, i) == true)
                    {
                        pos = i; break;
                    }
                }
            }
            pos = pos + 56;
            int npackets = header[pos] + header[pos + 1] * 256 + header[pos + 2] * 4096;
            return npackets;
        }

        bool CheckByteArrays(byte[] byte1, byte[] byte2, int offset2)
        {
            byte[] b1 = byte1;
            byte[] b2 = new byte[b1.Length];
            Array.ConstrainedCopy(byte2, offset2, b2, 0, b1.Length);
            int i = 0;
            for (i = 0; i <= b1.Length - 1; i++)
            {
                if (b1[i] != b2[i]) return false;
            }
            return true;
        }

        byte[] ReturnB(System.Net.Sockets.Socket sd)
        {
            int n = 0;
            byte[] b = new byte[10001];
            byte[] bs = new byte[8];
            int i = 0;
            int n1 = 0;
            while (!(n1 == 8))
            {
                n = sd.Receive(bs, n1, 8 - n1, 0);
                n1 = n1 + n;
            }
            Array.ConstrainedCopy(bs, 0, b, 0, n);
            n1 = 0;
            while (!(n1 == 8))
            {
                n = sd.Receive(bs, n1, 8 - n1, 0);
                n1 = n1 + n;
            }
            Array.ConstrainedCopy(bs, 0, b, 8, n);
            i = bs[0] + bs[1] * (256);
            n1 = 0;
            while (!(n1 == i))
            {
                n = sd.Receive(b, 16 + n1, i - n1, 0);
                n1 = n1 + n;
            }
            byte[] c = new byte[16 + i];
            Array.ConstrainedCopy(b, 0, c, 0, 16 + i);
            return c;
        }

        int ReturnB2(System.Net.Sockets.Socket sd)
        {
            int n = 0;
            byte[] bs = new byte[8];
            int i = 0;
            int n1 = 0;
            while (!(n1 == 8))
            {
                n = sd.Receive(bs, n1, 8 - n1, 0);
                n1 = n1 + n;
            }
            i = (int)(bs[0] + bs[1] * 256 + bs[2] * 4096 + bs[3] * (Math.Pow(16, 4)));
            byte[] b = new byte[i + 8];
            Array.ConstrainedCopy(bs, 0, b, 0, n);
            n = sd.Receive(b, 8, i, 0);
            string s = HexString(b, 8 + n, 0);
            if (s.Contains("1E 0 4 0"))
                return 1;
            if (s.Contains("1B 0 4 0"))
                return 2;
            throw new Exception("Error");
        }

        byte[] CommandCheck(System.Net.Sockets.Socket sd, int comm)
        {
            int n1 = 0;
            int n = 0;
            byte[] b = new byte[10001];
            byte[] bs = new byte[8];
            int i = 0;
            while (!(n1 == 8))
            {
                n = sd.Receive(bs, n1, 8 - n1, 0);
                n1 = n1 + n;
            }
            Array.ConstrainedCopy(bs, 0, b, 0, n);
            n1 = 0;
            while (!(n1 == 8))
            {
                n = sd.Receive(bs, n1, 8 - n1, 0);
                n1 = n1 + n;
            }
            Array.ConstrainedCopy(bs, 0, b, 8, n);
            i = bs[0] + bs[1] * (256);
            n1 = 0;
            while (!(n1 == i))
            {
                n = sd.Receive(b, 16 + n1, i - n1, 0);
                n1 = n1 + n;
            }
            string s = HexString(b, 16 + n, 0);
            if (s.Contains("1b 0 4 0") == true) { TTTest(sd); return CommandCheck(sd, comm); }
            if (b[36] != comm)
            {
                if (b[36] == 0x1b) { Log.Debug("MMSDownloader : Performing Network Timing Test"); TTTest(sd); }
                if (b[36] == 0x15) { Log.Debug("MMSDownloader : Validating Network Connection..."); return CommandCheck(sd, comm); }
            }
            return b;
            /*if ((n > i) == true)
            {
                Console.WriteLine("");
                int z = 0;
                while (!(n == 0))
                {
                    n = sd.Receive(b, 16 + i + 1 + z, 2, 0);
                    z = z + n;
                }
                s = HexString(b, 16 + i + 1 + n, 0);
                Console.WriteLine(16 + i + 1 + z);
            }
            if (i * 4 < 40)
            {
                Console.WriteLine(i);
                while (!(n == 0))
                {
                    n = sd.Receive(b, 16 + i, i, 0);
                    if (n > 0)
                        Console.WriteLine("HUH?");
                }
            }*/
        }

        byte[] Pad0(byte[] array, int extra)
        {
            byte[] narray = new byte[array.Length * 2 - 1 + extra + 1];
            int i = 0;
            for (i = 0; i <= array.Length - 1; i++)
            {
                narray[2 * i] = array[i];
                narray[2 * i + 1] = 0x0;
            }
            for (i = 1; i <= extra; i++)
            {
                narray[narray.Length - i] = 0x0;
            }
            return narray;
        }

        byte[] HPacket(int comm, byte[] b1, byte[] b2, byte[] b3 = null)
        {
            int tot = 0;
            if (b2 == null)
            {
                tot = b1.Length + 40;
            }
            else
            {
                tot = b1.Length + b2.Length + 40;
            }
            if ((b3 != null))
            {
                tot = tot + b3.Length;
            }
            int x = 0;
            tot = tot + 8;
            tot = (tot - 16) / 8;
            tot = tot * 8 + 16;
            if (tot - 16 > 255)
            {
                x = tot - 256 - 16;
            }
            else
            {
                x = tot - 16;
            }
            byte[] h = { 0x1, 0x0, 0x0, 0x0, 0xce, 0xfa, 0xb, 0xb0, Convert.ToByte(x), Convert.ToByte((tot - 16) / 256), 0x0, 0x0, 0x4d, 0x4d, 0x53, 0x20, Convert.ToByte((tot - 16) / 8), 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, Convert.ToByte((tot - 32) / 8), 0x0, 0x0, 0x0, Convert.ToByte(comm), 0x0, 0x3, 0x0 };
            byte[] pack = new byte[tot];
            Array.ConstrainedCopy(h, 0, pack, 0, 40);
            Array.ConstrainedCopy(b1, 0, pack, 40, b1.Length);
            if ((b2 != null))
                Array.ConstrainedCopy(b2, 0, pack, b1.Length + 40, b2.Length);
            if ((b3 != null))
                Array.ConstrainedCopy(b3, 0, pack, tot - b3.Length, b3.Length);
            string s = HexString(pack, pack.Length, 0);
            return pack;
        }

        void TTTest(System.Net.Sockets.Socket sd)
        {
            byte[] HB = { 0x1, 0x0, 0x0, 0x0, 0xff, 0xff, 0x1, 0x0 };
            sd.Send(HPacket(0x1b, HB, null));
        }
    }
}
