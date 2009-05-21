using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Google.GData.Client
{
    /// <summary>Type of compression to use for the GZipStream. Currently only Decompress is supported.</summary>
    public enum CompressionMode
	{
        /// <summary>Compresses the underlying stream.</summary>
        Compress,
        /// <summary>Decompresses the underlying stream.</summary>
        Decompress,
	}

    /// <summary>Provides methods and properties used to compress and decompress streams.</summary>
    public class GZipStream : Stream
    {
        #region Native const, structs, and defs
        private const string ZLibVersion = "1.2.3";

        private enum ZLibReturnCode
        {
            Ok = 0,
            StreamEnd = 1,
            NeedDictionary = 2,
            Errno = -1,
            StreamError = -2,
            DataError = -3,
            MemoryError = -4,
            BufferError = -5,
            VersionError = -6
        }

        private enum ZLibFlush
        {
            NoFlush = 0,
            PartialFlush = 1,
            SyncFlush = 2,
            FullFlush = 3,
            Finish = 4
        }

        private enum ZLibCompressionLevel
        {
            NoCompression = 0,
            BestSpeed = 1,
            BestCompression = 2,
            DefaultCompression = 3
        }

        private enum ZLibCompressionStrategy
        {
            Filtered = 1,
            HuffmanOnly = 2,
            DefaultStrategy = 0
        }

        private enum ZLibCompressionMethod
        {
            Delated = 8
        }

        private enum ZLibDataType
        {
            Binary = 0,
            Ascii = 1,
            Unknown = 2,
        }

        private enum ZLibOpenType
        {
            ZLib = 15,
            GZip = 15 + 16,
            Both = 15 + 32,
        }


        [StructLayoutAttribute(LayoutKind.Sequential)]
        private struct z_stream
        {
            public IntPtr next_in;  /* next input byte */
            public uint avail_in;  /* number of bytes available at next_in */
            public uint total_in;  /* total nb of input bytes read so far */

            public IntPtr next_out; /* next output byte should be put there */
            public uint avail_out; /* remaining free space at next_out */
            public uint total_out; /* total nb of bytes output so far */

            public IntPtr msg;      /* last error message, NULL if no error */
            public IntPtr state; /* not visible by applications */

            public IntPtr zalloc;  /* used to allocate the internal state */
            public IntPtr zfree;   /* used to free the internal state */
            public IntPtr opaque;  /* private data object passed to zalloc and zfree */

            public ZLibDataType data_type;  /* best guess about the data type: ascii or binary */
            public uint adler;      /* adler32 value of the uncompressed data */
            public uint reserved;   /* reserved for future use */
        };
        #endregion

        #region P/Invoke
#if WindowsCE || PocketPC
        [DllImport("zlib.arm.dll", EntryPoint = "inflateInit2_", CharSet = CharSet.Auto)]
#else
        [DllImport("zlib.x86.dll", EntryPoint = "inflateInit2_", CharSet = CharSet.Ansi)]
#endif
        private static extern ZLibReturnCode    inflateInit2(ref z_stream strm, ZLibOpenType windowBits, string version, int stream_size);

#if WindowsCE || PocketPC
        [DllImport("zlib.arm.dll", CharSet = CharSet.Auto)]
#else
        [DllImport("zlib.x86.dll", CharSet = CharSet.Ansi)]
#endif
        private static extern ZLibReturnCode     inflate(ref z_stream strm, ZLibFlush flush);

#if WindowsCE || PocketPC
        [DllImport("zlib.arm.dll", CharSet = CharSet.Auto)]
#else
        [DllImport("zlib.x86.dll", CharSet = CharSet.Ansi)]
#endif
        private static extern ZLibReturnCode    inflateEnd(ref z_stream strm);
        #endregion

        private const int           BufferSize = 16384;

        private Stream              compressedStream;
        private CompressionMode     mode;

        private z_stream            zstream = new z_stream();

        private byte[]              inputBuffer = new byte[BufferSize];
        private GCHandle            inputBufferHandle;

        /// <summary>Initializes a new instance of the GZipStream class using the specified stream and CompressionMode value.</summary>
        /// <param name="stream">The stream to compress or decompress.</param>
        /// <param name="mode">One of the CompressionMode values that indicates the action to take.</param>
        public GZipStream(Stream stream, CompressionMode mode)
        {
            if (mode != CompressionMode.Decompress)
                throw new NotImplementedException("Compression is not implemented.");

            this.compressedStream = stream;
            this.mode = mode;

            this.zstream.zalloc = IntPtr.Zero;
            this.zstream.zfree = IntPtr.Zero;
            this.zstream.opaque = IntPtr.Zero;

            ZLibReturnCode ret = inflateInit2(ref this.zstream, ZLibOpenType.Both, ZLibVersion, Marshal.SizeOf(typeof(z_stream)));

            if (ret != ZLibReturnCode.Ok)
                throw new ArgumentException("Unable to init ZLib. Return code: " + ret.ToString());

            this.inputBufferHandle = GCHandle.Alloc(inputBuffer, GCHandleType.Pinned);
        }

        /// <summary>GZipStream destructor. Cleans all allocated resources.</summary>
        ~GZipStream()
        {
			Dispose(false);
		}

		//////////////////////////////////////////////////////////////////////
		/// <summary>Handle Dispose since Stream implements IDisposable</summary> 
		/// <param name="disposing">indicates if dispose called it or finalize</param>
		//////////////////////////////////////////////////////////////////////
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (inputBufferHandle.IsAllocated)
			{
				inputBufferHandle.Free();
				inflateEnd(ref this.zstream);
			}
		}


        /// <summary>Reads a number of decompressed bytes into the specified byte array.</summary>
        /// <param name="buffer">The array used to store decompressed bytes.</param>
        /// <param name="offset">The location in the array to begin reading.</param>
        /// <param name="count">The number of bytes decompressed.</param>
        /// <returns>The number of bytes that were decompressed into the byte array. If the end of the stream has been reached, zero or the number of bytes read is returned.</returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.mode == CompressionMode.Compress)
                throw new NotSupportedException("Can't read on a compress stream!");

            bool exitLoop = false;

            byte[] tmpOutputBuffer = new byte[count];
            GCHandle tmpOutpuBufferHandle = GCHandle.Alloc(tmpOutputBuffer, GCHandleType.Pinned);

            this.zstream.next_out = tmpOutpuBufferHandle.AddrOfPinnedObject();
            this.zstream.avail_out = (uint)tmpOutputBuffer.Length;

            try
            {
                while (this.zstream.avail_out > 0 && exitLoop == false)
                {
                    if (this.zstream.avail_in == 0)
                    {
                        int readLength = this.compressedStream.Read(inputBuffer, 0, inputBuffer.Length);
                        this.zstream.avail_in = (uint)readLength;
                        this.zstream.next_in = this.inputBufferHandle.AddrOfPinnedObject();
                    }
                    ZLibReturnCode  result = inflate(ref zstream, ZLibFlush.NoFlush);
                    switch (result)
                    {
                        case ZLibReturnCode.StreamEnd:
                            exitLoop = true;
                            Array.Copy(tmpOutputBuffer, 0, buffer, offset, count - (int)this.zstream.avail_out);
                            break;
                        case ZLibReturnCode.Ok:
                            Array.Copy(tmpOutputBuffer, 0, buffer, offset, count - (int)this.zstream.avail_out);
                            break;
                        case ZLibReturnCode.MemoryError:
                            throw new OutOfMemoryException("ZLib return code: " + result.ToString());
                        default:
                            throw new Exception("ZLib return code: " + result.ToString());
                    }
                }

                return (count - (int)this.zstream.avail_out);
            }
            finally
            {
                tmpOutpuBufferHandle.Free();
            }
        }

        /// <summary>Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.</summary>
        public override void Close()
        {
            this.compressedStream.Close();
            base.Close();
        }

        /// <summary>Gets a value indicating whether the stream supports reading while decompressing a file.</summary>
        public override bool CanRead
        {
            get { return (this.mode == CompressionMode.Decompress ? true : false); }
        }

        /// <summary>Gets a value indicating whether the stream supports writing.</summary>
        public override bool CanWrite
        {
            get { return (this.mode == CompressionMode.Compress ? true : false); }
        }

        /// <summary>Gets a value indicating whether the stream supports seeking.</summary>
        public override bool CanSeek
        {
            get { return (false); }
        }

        /// <summary>Gets a reference to the underlying stream.</summary>
        public Stream BaseStream
        {
            get { return (this.compressedStream); }
        }

        #region Not yet supported
        /// <summary>Flushes the contents of the internal buffer of the current GZipStream object to the underlying stream.</summary>
        public override void Flush()
        {
            throw new NotSupportedException("The method or operation is not implemented.");
        }

        /// <summary>This property is not supported and always throws a NotSupportedException.</summary>
        /// <param name="offset">The location in the stream.</param>
        /// <param name="origin">One of the SeekOrigin values.</param>
        /// <returns>A long value.</returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>This property is not supported and always throws a NotSupportedException.</summary>
        /// <param name="value">The length of the stream.</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>This property is not supported and always throws a NotSupportedException.</summary>
        /// <param name="buffer">The array used to store compressed bytes.</param>
        /// <param name="offset">The location in the array to begin reading.</param>
        /// <param name="count">The number of bytes compressed.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Not yet supported!");
        }

        /// <summary>This property is not supported and always throws a NotSupportedException.</summary>
        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        /// <summary>This property is not supported and always throws a NotSupportedException.</summary>
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
        #endregion
    }
}
