using System;
using System.Globalization;
using System.Text;
using System.IO;

namespace HybridDSP.Net.HTTP
{
    internal class ChunkedStream : HTTPOutputStream
	{
        public ChunkedStream(HTTPServerSession session) : base(session) { }

		public override void Write(byte[] buffer, int offset, int count)
		{
			byte[] lengthLine = Encoding.UTF8.GetBytes(count.ToString("x", CultureInfo.InvariantCulture) + "\r\n");
			base.Write(lengthLine, 0, lengthLine.Length);
			base.Write(buffer, 0, count);
			base.Write(new byte[] {13, 10}, 0, 2);
			base.Flush();
		}

        public override void Close()
        {
            //Finished chunked data, write footer.            
            byte[] data = System.Text.Encoding.UTF8.GetBytes("0;\r\n\r\n");
            base.Write(data, 0, data.Length);            

            base.Close();
        }
	}
}
