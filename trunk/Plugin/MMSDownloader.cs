#region Copyright
/******************************************************************************
	Copyright 2001-2005 Mehmet F. YUCE
	DownUtube is free software; you can redistribute it and/or modify
	it under the terms of the Lesser GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	DownUtube is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	Lesser GNU General Public License for more details.

	You should have received a copy of the Lesser GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
/*******************************************************************************/
#endregion

using Microsoft.VisualBasic;
using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.IO;
using System.Collections.ObjectModel;
using System.Runtime.Remoting.Messaging;

namespace OnlineVideos
{
    public static class MMSDownloadHelper
    {
        public static Exception Download(DownloadInfo downloadInfo)
        {
            try
            {
                MMSDownloader mmsDL = new MMSDownloader();
                mmsDL.Start(downloadInfo.Url, null);
                using (System.IO.FileStream fs = new System.IO.FileStream(downloadInfo.LocalFile, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    int buffSize = 16384;
                    byte[] buffer = new byte[buffSize];
                    long readSize;
                    do
                    {
                        readSize = mmsDL.Read(buffer, 0, buffSize);
                        fs.Write(buffer, 0, (int)readSize);
                    }
                    while (readSize > 0);
                    mmsDL.Close();
                    return null;
                }
            }
            catch (Exception ex)
            {
                return ex;                
            }
        }
    }
    public class MMSStreamProgressChangedEventArgs
    {
        public MMSStreamProgressChangedEventArgs()
        {
        }

        private long bytesReceived = 0;

        public long BytesReceived
        {
            get { return bytesReceived; }
            set { bytesReceived = value; }
        }
        private int progressPercentage = 0;

        public int ProgressPercentage
        {
            get { return progressPercentage; }
            set { progressPercentage = value; }
        }
        private long totalBytesToReceive = 0;

        public long TotalBytesToReceive
        {
            get { return totalBytesToReceive; }
            set { totalBytesToReceive = value; }
        }
    }
    public class MMSDownloader : Stream, IDisposable
    {
        //Thread downloadThread = null;
        //public AsyncCompletedEventHandler DownloadFileComleted;
        public delegate void FileInfoGotHandler(object sender, MMSStreamProgressChangedEventArgs e);
        public FileInfoGotHandler FileInfoGot;

        //private void OnDownloadCompleted(AsyncCompletedEventArgs e)
        //{
        //    if (DownloadFileComleted != null)
        //    {
        //        DownloadFileComleted(this, e);
        //    }
        //}

        private void OnFileInfoGot(MMSStreamProgressChangedEventArgs e)
        {
            if (FileInfoGot != null)
            {
                FileInfoGot(this, e);
            }
        }

        //private long fileSize = 0;

        //public long FileSize
        //{
        //    get { return fileSize; }
        //    set { fileSize = value; }
        //}

        private string status = null;
        private bool _bCancel = false;

        public bool Cancel
        {
            get { return _bCancel; }
            set
            {
                _bCancel = value;
                if (_bCancel)
                {
                    _del.EndInvoke(null);
                }

            }
        }
        //private bool _FileInfoGot = false;

        //public bool FileInfoGot
        //{
        //    get { return _FileInfoGot; }
        //    set { _FileInfoGot = value; }
        //}

        private delegate void ReadThreadDelegate();
        private bool _OnlyInfo;

        public bool OnlyInfo
        {
            get { return _OnlyInfo; }
            set { _OnlyInfo = value; }
        }


        Collection<byte[]> _a_a_bytes = new Collection<byte[]>();
        //FileStream _file = null;
        ReadThreadDelegate _del = null;
        //public CustomStream(string _strFile)
        //{
        //    _file = new FileStream(_strFile, FileMode.Open);
        //    _del = new ReadThreadDelegate(this.ReadThread);
        //    AsyncCallback callBack = new AsyncCallback(this.Ended);
        //    IAsyncResult result = _del.BeginInvoke(callBack, null);
        //}

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public override void Flush()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override long Length
        {
            get { throw new Exception("The method or operation is not implemented."); }
        }

        public override long Position
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        private bool _bCompleted = false;

        public bool Completed
        {
            get { return _bCompleted; }
            set { _bCompleted = value; }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if ((_a_a_bytes.Count == 0 && _bCompleted) || _bCancel)
                return 0;
            else
            {
                while (_a_a_bytes.Count == 0)
                {
                    Thread.Sleep(1);
                    if (_bCancel)
                        return 0;
                    if (_bCompleted && _a_a_bytes.Count == 0)
                        return 0;
                }
                byte[] _btyestoret = _a_a_bytes[0];
                _a_a_bytes.RemoveAt(0);
                if (count < _btyestoret.Length)
                {
                    using (MemoryStream mem = new MemoryStream(buffer))
                    {
                        mem.Write(_btyestoret, 0, count);
                    }
                    byte[] _bytestoaddagain = new byte[_btyestoret.Length - count];
                    using (MemoryStream memwrite = new MemoryStream(_bytestoaddagain))
                    {
                        memwrite.Write(buffer, count - 1, _btyestoret.Length);
                    }
                    _a_a_bytes.Insert(0, _bytestoaddagain);
                    return count;
                }
                else
                {
                    _btyestoret.CopyTo(buffer, 0);
                    return _btyestoret.Length;
                }
            }
        }
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void SetLength(long value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new Exception("The method or operation is not implemented.");
        }
        public void Ended(IAsyncResult result)
        {
            ReadThreadDelegate factorDelegate = (ReadThreadDelegate)((AsyncResult)result).AsyncDelegate;
            // Obtain the result.
            try
            {
                factorDelegate.EndInvoke(result);
            }
            catch 
            {
                
            }

        }
        #region IDisposable Members

        void IDisposable.Dispose()
        {
            //throw new Exception("The method or operation is not implemented.");
            _a_a_bytes.Clear();
        }

        #endregion

        //private string status = "";

        //public string Status
        //{
        //    get { return status; }
        //    set { status = value; DownUtube.Classes.CommonFunctions.SetStatus(value, 0); }
        //}
        private string info = "";

        public string Info
        {
            get { return info; }
            set { }
        }

        private string realurl = "";

        public string RealUrl
        {
            get { return realurl; }
            set { realurl = value; }
        }
        private string realpath = "";

        public string RealPath
        {
            get { return realpath; }
            set { realpath = value; }
        }

        public System.Net.Sockets.Socket sd = null;
        public System.Net.IPEndPoint dip = null;
        public System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
        public string s;
        public string s1;
        public static string StringHereToHere(string here, string here1, string here2)
        {
            string stringHereToHereReturn = null;
            stringHereToHereReturn = here.Substring(here.IndexOf(here1) + here1.Length, here.IndexOf(here2, here.IndexOf(here1) + here1.Length) - here.IndexOf(here1) - here1.Length);
            return stringHereToHereReturn;
        }

        public static string StringEndToEnd(string here, string here1, string here2)
        {
            string stringEndToEndReturn = null;
            stringEndToEndReturn = here.Substring(here.LastIndexOf(here1) + here1.Length, here.LastIndexOf(here2) - here.LastIndexOf(here1) - here1.Length);
            return stringEndToEndReturn;
        }

        public static string PathEt(string str)
        {
            string transTemp0 = @"\\";
            string transTemp1 = @"\";
            return (str + @"\").Replace(transTemp0, transTemp1);// Strings.Replace(str + @"\", transTemp0, transTemp1, 1, -1, (Microsoft.VisualBasic.CompareMethod)(0));
        }

        public static string HexString(byte[] b, int l, int offset)
        {
            int i;
            if (l == 0)
            {
                l = b.Length;
            }

            string str = "";
            for (i = offset; i <= l - 1 + offset; i++)
            {
                //str = str + " " +  Conversion.Hex(b[i]);
                str = str + " " + ConverToHex(b[i]);
            }
            return str;
        }

        public void InitiateSession(System.Net.Sockets.Socket sd, string baseIdent)
        {
            string Command = "NSPlayer/9.0.0.2980; {" + Guid.NewGuid().ToString() + "}; Host: " + baseIdent;
            byte[] P1B1 = { (0XF0), (0XF0), (0XF0), (0XF0), (0XB), (0X0), (0X4), (0X0), (0X1C), (0X0), (0X3), (0X0) };
            byte[] P1B2 = Pad0(enc.GetBytes(Command), 6);
            sd.Send(HPacket(0X1, P1B1, P1B2, null));
        }

        public void SendTimingTest(System.Net.Sockets.Socket sd)
        {
            byte[] P2B1 = { (0XF1), (0XF0), (0XF0), (0XF0), (0XB), (0X0), (0X4), (0X0) };
            sd.Send(HPacket(0X18, P2B1, null, null));
        }

        public void RequestConnection(System.Net.Sockets.Socket sd)
        {
            string transTemp2 = sd.LocalEndPoint.ToString();
            string transTemp3 = ":";
            string transTemp4 = @"\TCP\";
            //string Command3 = @"\\" + Strings.Replace(transTemp2, transTemp3, transTemp4, 1, -1, (Microsoft.VisualBasic.CompareMethod)(0));
            string Command3 = @"\\" + transTemp2.Replace(transTemp3, transTemp4);
            Command3 = @"\\" + StringHereToHere(Command3, @"\\", @"\TCP\") + @"\TCP\1755";
            byte[] P3B1 = { ( 0XF1 ), ( 0XF0 ), ( 0XF0 ), ( 0XF0 ), ( 0XFF ), ( 0XFF ), ( 0XFF ), ( 0XFF ), ( 0X0 ), ( 0X0 ), ( 0X0 ), ( 0X0 ), ( 0X0 ), ( 0X0 ), ( 0XA0 ), ( 0X0 )
            , ( 0X2 ), ( 0X0 ), ( 0X0 ), ( 0X0 ) };
            byte[] P3B2 = Pad0(enc.GetBytes(Command3), 2);
            sd.Send(HPacket(0X2, P3B1, P3B2, null));
        }

        public void RequestFile(System.Net.Sockets.Socket sd, string rest)
        {
            byte[] P4B1 = { (0X1), (0X0), (0X0), (0X0), (0XFF), (0XFF), (0XFF), (0XFF), (0X0), (0X0), (0X0), (0X0), (0X0), (0X0), (0X0), (0X0) };
            byte[] P4B2 = Pad0(enc.GetBytes(rest), 0);
            sd.Send(HPacket(0X5, P4B1, P4B2, null));
        }

        public void Start(string Url, string path)
        {
            realpath = path;
            realurl = Url;
            _del = new ReadThreadDelegate(this.MMSDownload);
            AsyncCallback callBack = new AsyncCallback(this.Ended);
            IAsyncResult result = _del.BeginInvoke(callBack, null);
        }
        public void NewComing(byte[] _bytes, int _iReadCount)
        {
            byte[] _a_readbytes = new byte[_iReadCount];
            using (MemoryStream _mem = new MemoryStream(_a_readbytes))
            {
                _mem.Write(_bytes, 0, _iReadCount);
            }
            _a_a_bytes.Add(_a_readbytes);
            Thread.Sleep(1);
        }
        public void MMSDownload()
        {
            string Url = realurl;
            string path = realpath;
            //bool two = false;
            //again:
            string baseIdent = null;
            string rest = null;
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            string transTemp5 = "mms://";
            string transTemp6 = "";
            baseIdent = Url.Replace(transTemp5, transTemp6);
            baseIdent = baseIdent.Substring(0, baseIdent.IndexOf("/"));
            string transTemp7 = "";
            rest = Url.Replace("mms://" + baseIdent + "/", transTemp7);
            string transTemp8 = " ";
            string transTemp9 = "%20";
            rest = rest.Replace(transTemp8, transTemp9);
            status = "Resolving IP address...";
            sd = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            dip = new System.Net.IPEndPoint(System.Net.Dns.GetHostEntry(baseIdent).AddressList[0], 1755);
            status = "Connecting...";
            sd.Connect(dip);
            status = "Connected.";
            status = "Initializing Session...";
            InitiateSession(sd, baseIdent);
            CommandCheck(sd, 0X1);
            status = "Session Established. Sending 1st Network Timing Test...";
            SendTimingTest(sd);
            CommandCheck(sd, 0X15);
            status = "Session Established. Sending 2nd Network Timing Test...";
            SendTimingTest(sd);
            CommandCheck(sd, 0X15);
            status = "Session Established. Sending 3rd Network Timing Test...";
            SendTimingTest(sd);
            CommandCheck(sd, 0X15);
            status = "Network Timing Test Successful. Requesting TCP Connection...";
            RequestConnection(sd);
            CommandCheck(sd, 0X2);
            status = "TCP Connection Accepted. Requesting File " + rest;
            RequestFile(sd, rest);
            byte[] b = CommandCheck(sd, 0X6);
            status = "File Request Accepted.";
            string t = HexM(b[71]) + HexM(b[70]) + HexM(b[69]) + HexM(b[68]) + HexM(b[67]) + HexM(b[66]) + HexM(b[65]) + HexM(b[64]);
            int psize = b[92] + b[93] * 256 + b[94] * 4096;
            int nps = b[96] + b[97] * 256 + b[98] * 4096;
            int hsize = b[108] + b[109] * 256 + b[110] * 4096;
            int fsize = psize * nps + hsize;
            decimal time = (DoublePercisionHex(t) - ((decimal)3.6));
            //fileSize = fsize;
            info = "File Size: " + fsize + "Bytes" + "\r\n" +
                    "Media Length: " + time + "Seconds" + "\r\n" +
                    "Packet Size: " + psize +
                    "Bytes" + "\r\n" +
                    "Header Size: " + hsize +
                    "Bytes" + "\r\n" +
                    "Number of Packets: " + nps +
                    "Packets";
            WriteStream(sd, path, time, hsize);
            sd.Close();
            status = "Done!";
        }

        public void HTTPMMSDownload(string Url, string path)
        {
            string baseIdent = null;
            string transTemp10 = "mms://";
            string transTemp11 = "";
            baseIdent = Url.Replace(transTemp10, transTemp11);
            string transTemp12 = "/";
            baseIdent = baseIdent.Substring(0, baseIdent.IndexOf(transTemp12) + 1 - 1);
            System.Net.Sockets.Socket s = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            System.Net.IPEndPoint ip = new System.Net.IPEndPoint(System.Net.Dns.GetHostEntry(baseIdent).AddressList[0], 80);
            status = "Connecting...";
            s.Connect(ip);
            status = "Connected. Sending Get Request...";
            string getfile = null;
            string transTemp13 = "";
            getfile = Url.Replace("mms://" + baseIdent, transTemp13);
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            getfile = "GET " + getfile + " HTTP/1.1" + "\r\n" + "Accept: */*" + "\r\n" + "User-Agent: NSPlayer/9.0.0.2980" + "\r\n" + "Host: " + baseIdent + "\r\n" + "Pragma: no-cache,rate=1.000000,stream-time=0" + "\r\n" + "Pragma: xPlayStrm=1" + "\r\n" + "Pragma: xClientGUID={" + Guid.NewGuid().ToString() + "}" + "\r\n" + "\r\n";
            byte[] bytes = enc.GetBytes(getfile);
            s.Send(bytes, bytes.Length, System.Net.Sockets.SocketFlags.None);
            int n = 0;
            byte[] recbytes = new byte[1024];
            string hinfo = "";
            int rbytes = 0;
            status = "Recieving Command Header...";
            do
            {
                n = s.Receive(recbytes, 1, System.Net.Sockets.SocketFlags.None);
                rbytes = rbytes + n;
                hinfo = hinfo + enc.GetString(recbytes, 0, n);
                status = "Recieving Command Header..." + rbytes + "Bytes Recieved...";
                if (hinfo.Contains("\r\n" + "\r\n") == true)
                {
                    break;
                }

            }
            while (true);
            byte[] tbytes = new byte[10001];
            if (hinfo.Contains("Busy") == true)
            {
                status = "Session Not Ready." + "\r\n" + "Retry in 10 seconds...";
                return;
            }
            status = "Header Recieved...Recieving Data...";
            int i = 0;
            int n1 = 0;
            int cur = 0;
            System.IO.FileStream fs = new System.IO.FileStream(path, System.IO.FileMode.Create);
            byte[] bs = new byte[8];
            int p = 0;
            int np = 0;
            int rp = 0;
            Guid y = new Guid("75B22630-668E-11CF-A6D9-00AA0062CE6C");
            n1 = 0;
            do
            {
                n = s.Receive(tbytes, n1, ((System.Net.Sockets.SocketFlags)(16)));
                n1 = n1 + n;
                status = "Sorting Header..." + n1 + "Bytes Recieved...";
            }
            while (!(Find(y.ToByteArray(), tbytes) > 0));
            Array[] x = (System.Array[])SortOutHeader(tbytes, n1);
            fs.Write((byte[])x[0], 0, x[0].Length);
            bs = ((System.Byte[])(x[1]));
            hinfo = HexString(bs, 0, 0);
            i = bs[6] + bs[7] * (256) - 8 - x[0].Length;
            cur = cur + i + x[0].Length;
        more:
            status = "Header Size: " + cur + "Bytes";
            byte[] header = new byte[i - 1 + 1];
            n1 = 0;
            while (!(n1 == i))
            {
                n = s.Receive(header, n1, ((System.Net.Sockets.SocketFlags)(i - n1)));
                n1 = n1 + n;
            }
            if (p == 0)
            {
                p = GetPacketLength(header);
            }

            if (np == 0)
            {
                np = GetNumberOfPackets(header);
            }

            fs.Write(header, 0, header.Length);
            if (bs[5] == 0X4 | bs[5] == 0X8)
            {
                n1 = 0;
                while (!(n1 == 8))
                {
                    n = s.Receive(bs, n1, ((System.Net.Sockets.SocketFlags)(8 - n1)));
                    n1 = n1 + n;
                }
                hinfo = HexString(bs, 0, 0);
                i = bs[6] + bs[7] * (256) - 8;
                cur = cur + i;
                goto more;
            }
            bs = new byte[11];
            n1 = 0;
            while (!(n1 == 12))
            {
                n = s.Receive(bs, n1, ((System.Net.Sockets.SocketFlags)(12 - n1)));
                n1 = n1 + n;
            }
            i = bs[10] + bs[11] * (256) - 8;
            rp = 0;
            do
            {
                rp = rp + 1;
                byte[] buffer = new byte[p - 1 + 1];
                n1 = 0;
                while (!(n1 == i))
                {
                    n = s.Receive(buffer, n1, ((System.Net.Sockets.SocketFlags)(i - n1)));
                    n1 = n1 + n;
                }
                fs.Write(buffer, 0, p);
                cur = cur + p;
                status = "Recieving Packets. Packet Size Is " + p + "." + "\r\n" + "Recieved " + rp + " Packets Out Of " + np + "." + "\r\n" + "Downloaded So Far " + cur + "Bytes.";
                n1 = 0;
                while (!(n1 == 12))
                {
                    n = s.Receive(bs, n1, ((System.Net.Sockets.SocketFlags)(12 - n1)));
                    n1 = n1 + n;
                }
                // MsgBox(HexString(bs, 0, 0))
                // MsgBox(Convert(bs))
                i = bs[10] + bs[11] * (256) - 8;
                // MsgBox(i)
                // n1 = 0
                // Do Until n1 = 8
                //     n = s.Receive(bs, n1, 8 - n1, 0)
                //     n1 = n1 + n
                // Loop
                // MsgBox(HexString(bs, 0, 0))
            }
            while (!(n == 0));
            n = s.Receive(bs, 1, ((System.Net.Sockets.SocketFlags)(0)));
            if (n > 0)
            {
                MessageBox.Show("MMS::MORE!");
            }

        }

        //public void Cancel()
        //{
        //    if (downloadThread != null)
        //        downloadThread.Abort();
        //}
        //System.IO.Stream fs;
        public void WriteStream(System.Net.Sockets.Socket sd, string path, decimal time, int hsize)
        {
            int p = 0;
            int np = 0;
            int rp = 0;

            //bool fe = false;
            //using (fs = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                //fs = new System.IO.MemoryStream();
                byte[] P5B1 = { 0X1, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X80, 0X0, 0X0, 0XFF, 0XFF, 0XFF, 0XFF, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X20, 0XAC, 0X40, 0X2, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0 };
                //byte[] rb = null;
                //if (fe == true)
                //{
                //    rb = new byte[fs.Length - 1];
                //    fs.Read(rb, 0, rb.Length);
                //    p = GetPacketLength(rb);
                //    np = GetNumberOfPackets(rb);
                //    rp = ((int)fs.Length - hsize) / p;
                //    decimal rtime = 0;
                //    rtime = System.Convert.ToDecimal(((rp) / np) * time);
                //    byte[] br = HexDoublePercision(rtime);
                //    Array.ConstrainedCopy(br, 0, P5B1, 8, 8);
                //}
                status = "Requesting Media Header...";
                sd.Send(HPacket(0X15, P5B1, null, null));
                int n = 0;
                int n1 = 0;
                byte[] bs = new byte[8];
                int i = 0;
                int cur = 0;
                //int sp = 0;
                byte[] b = ReturnB(sd);
                //if (fe == true)
                //{
                //    fs.Position = 0;
                //}

                if (b[36] == 0X11)
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
                    s = HexString(bs, 0, 0);
                    cur = cur + i;
                    status = "Header Size: " + cur + "Bytes";
                    byte[] header = new byte[i - 1 + 1];
                    n1 = 0;
                    while (!(n1 == i))
                    {
                        n = sd.Receive(header, n1, i - n1, 0);
                        n1 = n1 + n;
                    }
                    //if (fe == true)
                    //{
                    //    if (Find(header, rb) == -1)
                    //    {
                    //        Interaction.MsgBox("ERROR! Files Dont Match!", (Microsoft.VisualBasic.MsgBoxStyle)(0), null);
                    //    }
                    //    //Exit
                    //}
                    if (p == 0)
                    {
                        p = GetPacketLength(header);
                    }

                    if (np == 0)
                    {
                        np = GetNumberOfPackets(header);
                    }

                    //fs.Write(header, 0, header.Length);
                    NewComing(header, header.Length);
                    if (bs[5] == 0X4 | bs[5] == 0X8)
                    {
                        goto more;
                    }
                }
                //if (fe == true)
                //{
                //    fs.Position = fs.Length - 1;
                //}

                status = "Header Recieved And Written...Requesting Media...";
                byte[] P6B1 = { 0X1, 0X0, 0X0, 0X0, 0XFF, 0XFF, 0X1, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0XFF, 0XFF, 0XFF, 0XFF, 0XFF, 0XFF, 0XFF, 0XFF, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0 };
                sd.Send(HPacket(0X7, P6B1, null, null));
                byte[] b2 = ReturnB(sd);
                cur = 0;
                if (b2[36] == 0X21)
                {
                    byte[] b3 = ((System.Byte[])(ReturnB(sd)));
                    if (b3[36] == 0X5)
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
                        byte[] buffer = new byte[p - 1 + 1];
                        do
                        {
                            n1 = 0;
                            while (!(n1 == i))
                            {
                                n = sd.Receive(buffer, n1, i - n1, 0);
                                n1 = n1 + n;
                            }
                            //if (fe == true)
                            //{
                            //    if (Find(buffer, rb) > -1)
                            //    {
                            //        sp = sp + 1;
                            //        Interaction.MsgBox("skipped: " + sp, (Microsoft.VisualBasic.MsgBoxStyle)(0), null);
                            //        goto skip;
                            //    }
                            //    else
                            //    {
                            //        fe = false;
                            //    }
                            //}
                            s = HexString(buffer, 0, 0);
                            if (s.Contains("1B 0 4 0") == true)
                            {
                                MessageBox.Show("MMS::!!!!");
                            }

                            //fs.Write(buffer, 0, p);
                            NewComing(buffer, p);
                            cur = cur + p;
                            status = "Recieving Packets. Packet Size Is " + p + "." + "\r\n" + "Recieved " + rp + " Packets Out Of " + np + "." + "\r\n" + "Downloaded So Far " + cur + "Bytes.";

                            MMSStreamProgressChangedEventArgs arg = new MMSStreamProgressChangedEventArgs();
                            arg.BytesReceived = cur;
                            arg.ProgressPercentage = (int)((rp / (double)np) * 100);
                            if (arg.ProgressPercentage > 100)
                                arg.ProgressPercentage = 100;
                            arg.TotalBytesToReceive = p * np;
                            //arg.TotalBytesToReceive = rp == 0 ? 0 : (long)(cur * (np / (double)rp));
                            OnFileInfoGot(arg);
                            if (_bCancel)
                                return;
                        skip:
                            n1 = 0;
                            int b1 = 0;
                            while (!(n1 == 8))
                            {
                                if (_bCancel)
                                {
                                    return;
                                }
                                n = sd.Receive(bs, n1, 8 - n1, 0);
                                if (n == 0)
                                {
                                    b1 = b1 + 1;
                                }

                                if (b1 > 0)
                                {
                                    status = b1.ToString();
                                }

                                n1 = n1 + n;
                                //Application.DoEvents();
                            }

                            s1 = HexString(bs, 0, 0);
                            if (s1.Contains("CE FA B B0") == true)
                            {
                                do
                                {
                                    int x = ReturnB2(sd);
                                    if (x == 1)
                                    {
                                        status = "Download Is Complete!";
                                        //OnDownloadCompleted(new AsyncCompletedEventArgs(null, false, null));
                                        _bCompleted = true;
                                        return;
                                    }

                                    if (x == 2)
                                    {
                                        status = "Sending Network Timing Test..";
                                        TTTest(sd);
                                        n1 = 0;
                                        while (!(n1 == 8))
                                        {
                                            n = sd.Receive(bs, n1, 8 - n1, 0);
                                            n1 = n1 + n;
                                        }
                                        //Application.DoEvents();
                                    }
                                }
                                while (!(HexString(bs, 8, 0).Contains("CE FA B B0") == false));
                            }
                            i = bs[6] + bs[7] * (256) - 8;
                            rp = rp + 1;
                        }
                        while (true);
                    }
                }
            }
        }

        public static string ConverToHex(int Number)
        {
            return Number.ToString("X");
        }

        public static string ConverToHex(byte Number)
        {
            return ConverToHex((int)Number);
        }
        public static string ConverToHex(decimal Number)
        {
            return ConverToHex((int)Number);
        }

        public static string HexM(int i)
        {
            string str = ConverToHex(i);
            if (str.Length < 2)
            {
                str = "0" + str;
            }

            return str;
        }

        public static decimal DoublePercisionHex(string str)
        {
            string strFull = HexToBinary64BitCalculate(str);
            string S = strFull.Substring(0, 1);
            string E = strFull.Substring(1, 11);
            string F = strFull.Substring(12, 52);
            if (S + E + F != strFull)
            {
                MessageBox.Show("MMS::Calculation Error!");
            }

            F = "1." + F;
            int exp = (int)BinaryCalculate(E) - 1023;
            if (exp > 2047 | exp < 0)
            {
                MessageBox.Show("MMS::Calculation Error!");
            }

            decimal d = BinaryCalculate(F);
            exp = (int)Math.Pow(2, exp);
            return exp * d;
        }

        public static byte[] HexDoublePercision(decimal d)
        {
            string str = "";
            string s = "0";
            int exp = 0;
            while (!(System.Convert.ToDecimal(Math.Pow(2, (exp + 1))) > d))
            {
                exp = exp + 1;
            }
            string e = ZM(BinaryConvert((exp + 1023).ToString()), 11);
            decimal df = 0;
            df = System.Convert.ToDecimal((System.Convert.ToDouble(d) / (Math.Pow(2, exp))) - 1);
            string f = BinaryConvert(df.ToString());
            f = f.Substring(f.IndexOf(".") + 1, 52);
            str = s + e + f;
            string hs = ConverToHex(BinaryICalculate(str));
            while (!(hs.Length == 16))
            {
                hs = "0" + hs;
            }
            byte[] bs = new byte[8];
            bs[0] = System.Convert.ToByte(ConvertHexToInt(hs.Substring(14, 2)));
            bs[1] = System.Convert.ToByte(ConvertHexToInt(hs.Substring(12, 2)));
            bs[2] = System.Convert.ToByte(ConvertHexToInt(hs.Substring(10, 2)));
            bs[3] = System.Convert.ToByte(ConvertHexToInt(hs.Substring(8, 2)));
            bs[4] = System.Convert.ToByte(ConvertHexToInt(hs.Substring(6, 2)));
            bs[5] = System.Convert.ToByte(ConvertHexToInt(hs.Substring(4, 2)));
            bs[6] = System.Convert.ToByte(ConvertHexToInt(hs.Substring(2, 2)));
            bs[7] = System.Convert.ToByte(ConvertHexToInt(hs.Substring(0, 2)));
            return bs;


        }
        public static int ConvertHexToInt(string Number)
        {
            return int.Parse(Number, System.Globalization.NumberStyles.AllowHexSpecifier);
        }
        private static string HexToBinary64BitCalculate(string str)
        {
            int n;
            Int64 r = 0;
            Int64 i = 0;
            i = Int64.Parse(str, System.Globalization.NumberStyles.AllowHexSpecifier);
            string outIdent = "0";
            n = 63;
            while (!(n == 0))
            {
                n = n - 1;
                r = System.Convert.ToInt64(Math.Pow(2, n));
                if (i - r >= 0)
                {
                    outIdent = outIdent + "1";


                    i = i - r;
                    continue;
                } if (i - r < 0)
                {
                    outIdent = outIdent + "0";
                }

            }
            return outIdent;
        }

        private static decimal BinaryCalculate(string str)
        {
            decimal i = 0;
            Int64 n = 0;
            string strp = str;
            string strn = "";
            if (str.Contains("."))
            {
                strp = str.Substring(0, str.IndexOf("."));
            }

            strn = str;
            if (str.Contains("."))
            {
                strn = strn.Substring(str.IndexOf(".") + 1);
            }

            while (!(strp.Length == 0))
            {
                i = i + System.Convert.ToDecimal((double.Parse(strp.Substring(strp.Length - 1)) * Math.Pow(2, n)));
                n = n + 1;
                strp = strp.Substring(0, strp.Length - 1);
            }
            n = 0;
            while (!(strn.Length == 0))
            {
                n = n - 1;
                i = i + System.Convert.ToDecimal((double.Parse(strn.Substring(0, 1)) * Math.Pow(2, n)));
                strn = strn.Substring(1);
            }
            return i;
        }

        public static Int64 BinaryICalculate(string str)
        {
            Int64 i = 0;
            int n;
            Int64 mp = 1;
            string strp = str;
            for (n = str.Length - 1; n >= 1; n += -1)
            {
                if (str.ToCharArray()[n] == '1')
                {
                    i = i + mp;
                }

                if (n == 1)
                {
                    break;
                }

                mp = mp * 2;
            }
            return i;
        }

        private static string ZM(string str, int n)
        {
            str = str.Substring(0, str.IndexOf("."));
            if (str.Length > n)
            {
                MessageBox.Show("MMS::!!!!111");
            }

            while (!(str.Length == n))
            {
                str = "0" + str;
            }
            return str;
        }

        private static string BinaryConvert(string dec)
        {
            string strp = dec;
            string strn = "";
            if (dec.Contains("."))
            {
                strp = dec.Substring(0, dec.IndexOf("."));
            }

            strn = dec;
            if (dec.Contains("."))
            {
                strn = strn.Substring(dec.IndexOf(".") + 1);
            }

            int pi = int.Parse(strp);
            decimal ni = decimal.Parse("0." + strn);
            int n = 63;
            strp = "";
            strn = ".";
            do
            {
                if (Math.Pow(2, n) > pi)
                {
                    n = n - 1;
                }

                strp = strp + "0";
                continue;
                //pi = (int)(pi - Math.Pow(2, n));
                //strp = strp + "1";
                //n = n - 1;
            }
            while (!(n == -1));
            while (!(n == -63))
            {
                if (System.Convert.ToDecimal(Math.Pow(2, n)) > ni)
                {
                    n = n - 1;
                }

                strn = strn + "0";
                continue;
                //ni = ni - System.Convert.ToDecimal(Math.Pow(2, n));
                //strn = strn + "1";
                //n = n - 1;
            }
            string str = strp + strn;
            while (!(str[0] != char.Parse("0")))
            {
                str = str.Substring(1);
            }
            return str;
        }

        public static bool CheckQueryAtOffset(byte[] Query, byte[] Pool, int PoolOffset)
        {
            byte[] b1 = Query;
            byte[] b2 = new byte[b1.Length - 1 + 1];
            Array.ConstrainedCopy(Pool, PoolOffset, b2, 0, b1.Length);
            int i;
            for (i = 0; i <= b1.Length - 1; i++)
            {
                if (b1[i] != b2[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static int Find(byte[] Query, byte[] Pool)
        {
            int i;
            for (i = 0; i <= Pool.Length - Query.Length - 1; i++)
            {
                if (Pool[i] == Query[0])
                {
                    if (CheckQueryAtOffset(Query, Pool, i) == true)
                    {
                        return i;
                    }

                    return 0;
                }
            }
            return -1;
        }

        public static Array[] SortOutHeader(byte[] b, int n1)
        {
            Array[] x = new Array[2];
            Guid y = new Guid("75B22630-668E-11CF-A6D9-00AA0062CE6C");
            int pos = Find(y.ToByteArray(), b);
            byte[] c = new byte[n1 - pos - 1 + 1];
            Array.ConstrainedCopy(b, pos, c, 0, n1 - pos);
            x[0] = c;
            byte[] bs = new byte[8];
            Array.ConstrainedCopy(b, pos - 8, bs, 0, 8);
            x[1] = bs;
            return x;
        }

        public static int GetPacketLength(byte[] header)
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
                        pos = i;
                    }

                    break;
                }
            }
            pos = pos + 92;
            int psize = header[pos] + header[pos + 1] * 256 + header[pos + 2] * 4096;
            return psize;
        }

        public static int GetNumberOfPackets(byte[] header)
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
                        pos = i;
                    }

                    break;
                }
            }
            pos = pos + 56;
            int npackets = header[pos] + header[pos + 1] * 256 + header[pos + 2] * 4096;
            return npackets;
        }

        public static bool CheckByteArrays(byte[] byte1, byte[] byte2, int offset2)
        {
            byte[] b1 = byte1;
            byte[] b2 = new byte[b1.Length - 1 + 1];
            Array.ConstrainedCopy(byte2, offset2, b2, 0, b1.Length);
            int i;
            for (i = 0; i <= b1.Length - 1; i++)
            {
                if (b1[i] != b2[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static byte[] ReturnB(System.Net.Sockets.Socket sd)
        {
            int n = 0;
            byte[] b = new byte[10001];
            byte[] bs = new byte[8];
            int i = 0;
            int n1 = 0;
            while (!(n1 == 8))
            {
                n = sd.Receive(bs, n1, 8 - n1, System.Net.Sockets.SocketFlags.None);
                n1 = n1 + n;
            }
            Array.ConstrainedCopy(bs, 0, b, 0, n);
            n1 = 0;
            while (!(n1 == 8))
            {
                n = sd.Receive(bs, n1, 8 - n1, System.Net.Sockets.SocketFlags.None);
                n1 = n1 + n;
            }
            Array.ConstrainedCopy(bs, 0, b, 8, n);
            i = bs[0] + bs[1] * (256);
            n1 = 0;
            while (!(n1 == i))
            {
                n = sd.Receive(b, 16 + n1, i - n1, ((System.Net.Sockets.SocketFlags)(0)));
                n1 = n1 + n;
            }
            byte[] c = new byte[16 + i - 1 + 1];
            Array.ConstrainedCopy(b, 0, c, 0, 16 + i);
            return c;
        }

        public int ReturnB2(System.Net.Sockets.Socket sd)
        {
            int n = 0;
            byte[] bs = new byte[8];
            int i = 0;
            int n1 = 0;
            while (!(n1 == 8))
            {
                n = sd.Receive(bs, n1, 8 - n1, System.Net.Sockets.SocketFlags.None);
                n1 = n1 + n;
            }
            i = (int)(bs[0] + bs[1] * 256 + bs[2] * 4096 + bs[3] * (Math.Pow(16, 4)));
            byte[] b = new byte[i + 8 - 1 + 1];
            Array.ConstrainedCopy(bs, 0, b, 0, n);
            n = sd.Receive(b, 8, i, 0);
            s = HexString(b, 8 + n, 0);
            if (s.Contains("1E 0 4 0"))
            {
                return 1;
            }

            if (s.Contains("1B 0 4 0"))
            {
                return 2;
            }

            return 0;
        }

        public byte[] CommandCheck(System.Net.Sockets.Socket sd, int comm)
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
            s = HexString(b, 16 + n, 0);
            if (s.Contains("1b 0 4 0") == true)
            {


                TTTest(sd);
                return CommandCheck(sd, comm);
                // Unreachable code detected and removed 
            }

            if (b[36] != comm)
            {
                if (b[36] == 0X1B)
                {
                    status = "Performing Network Timing Test";
                }

                TTTest(sd);
                if (b[36] == 0X15)
                {
                    status = "Validating Network Connection...";
                }

                return CommandCheck(sd, comm);
                //  Unreachable code detected and removed 
            }
            return b;
            //  Unreachable code detected and removed 
        }

        public static byte[] Pad0(byte[] array, int extra)
        {
            byte[] narray = new byte[array.Length * 2 - 1 + extra + 1];
            int i;
            for (i = 0; i <= array.Length - 1; i++)
            {
                narray[2 * i] = array[i];
                narray[2 * i + 1] = 0X0;
            }
            for (i = 1; i <= extra; i++)
            {
                narray[narray.Length - i] = 0X0;
            }
            return narray;
        }

        public byte[] HPacket(int comm, byte[] b1, byte[] b2, [System.Runtime.InteropServices.Optional] byte[] b3)
        {
            int tot;
            if (b2 == null)
            {
                tot = b1.Length + 40;
            }
            else
            {
                tot = b1.Length + b2.Length + 40;
            }
            if (!(b3 == null))
            {
                tot = tot + b3.Length;
            }
            int x;
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
            byte[] h = { 0X1, 0X0, 0X0, 0X0, 0XCE, 0XFA, 0XB, 0XB0, (byte)x, (byte)((tot - 16) / 256), 0X0, 0X0, 0X4D, 0X4D, 0X53, 0X20, (byte)((tot - 16) / 8), 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, 0X0, (byte)((tot - 32) / 8), 0X0, 0X0, 0X0, (byte)comm, 0X0, 0X3, 0X0 };
            byte[] pack = new byte[tot - 1 + 1];
            Array.ConstrainedCopy(h, 0, pack, 0, 40);
            Array.ConstrainedCopy(b1, 0, pack, 40, b1.Length);
            if (!(b2 == null))
            {
                Array.ConstrainedCopy(b2, 0, pack, b1.Length + 40, b2.Length);
            }

            if (!(b3 == null))
            {
                Array.ConstrainedCopy(b3, 0, pack, tot - b3.Length, b3.Length);
            }

            s = HexString(pack, pack.Length, 0);
            return pack;
        }

        public void TTTest(System.Net.Sockets.Socket sd)
        {
            byte[] HB = { System.Convert.ToByte(0X1), System.Convert.ToByte(0X0), System.Convert.ToByte(0X0), System.Convert.ToByte(0X0), System.Convert.ToByte(0XFF), System.Convert.ToByte(0XFF), System.Convert.ToByte(0X1), System.Convert.ToByte(0X0) };
            sd.Send(HPacket(0X1B, HB, null, null));
        }

    }
}
