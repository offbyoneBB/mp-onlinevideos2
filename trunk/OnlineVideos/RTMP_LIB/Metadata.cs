using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RTMP_LIB
{
	public class Metadata
	{
		/// <summary>
		/// Duration of stream in seconds returned by Metadata
		/// </summary>
		public double Duration { get; protected set; }

		/// <summary>
		/// Sum of bytes of all tracks in the stream (if Metadata was received and held that information)
		/// </summary>
		public long CombinedTracksLength { get; protected set; } // number of bytes

		/// <summary>
		/// sum of bitrates of all tracks in the stream (if Metadata was received and held that information)
		/// </summary>
		public long CombinedBitrates { get; protected set; }

		public long EstimateBytes(int rtmpInChunkSize)
		{
			long EstimatedLength = 0;

			if (CombinedTracksLength > 0)
			{
				EstimatedLength = CombinedTracksLength + (CombinedTracksLength / rtmpInChunkSize) * 11;
			}
			else if (CombinedBitrates > 0)
			{
				EstimatedLength = (long)(CombinedBitrates * 1000 / 8 * (Duration <= 0 ? 10800 : Duration)); // set 3h if no duration in metadata
			}
			else
			{
				// nothing was in the metadata -> just use duration and a bitrate of 2000
				EstimatedLength = (long)(2000 * 1000 / 8 * (Duration <= 0 ? 10800 : Duration)); // set 3h if no duration in metadata
			}

			EstimatedLength = (long)((double)EstimatedLength * 1.5d);

			if (EstimatedLength > 0x7fffffff) EstimatedLength = 0x7fffffff; // honor 2GB size limit

			return EstimatedLength;
		}

		public void DecodeFromPacketBody(byte[] buffer, int offset, int size, bool? Playing)
		{
			AMFObject obj = new AMFObject();
			int nRes = obj.Decode(buffer, offset, size, false);
			if (nRes < 0)
			{
				//Log(LOGERROR, "%s, error decoding meta data packet", __FUNCTION__);
				return;
			}

			if (Playing == false) obj.Dump(); // if not playing log all metadata packets
			string metastring = obj.GetProperty(0).GetString();

			if (metastring == "onMetaData")
			{
				if (Playing == true) obj.Dump(); // always log onMetaData packets for further analyzing

				List<AMFObjectProperty> props = new List<AMFObjectProperty>();
				obj.FindMatchingProperty("duration", props, 1);
				if (props.Count > 0)
				{
					Duration = props[0].GetNumber();
					Logger.Log(string.Format("Set duration: {0}", Duration));
				}
				props.Clear();
				obj.FindMatchingProperty("audiodatarate", props, 1);
				if (props.Count > 0)
				{
					int audiodatarate = (int)props[0].GetNumber();
					CombinedBitrates += audiodatarate;
					Logger.Log(string.Format("audiodatarate: {0}", audiodatarate));
				}
				props.Clear();
				obj.FindMatchingProperty("videodatarate", props, 1);
				if (props.Count > 0)
				{
					int videodatarate = (int)props[0].GetNumber();
					CombinedBitrates += videodatarate;
					Logger.Log(string.Format("videodatarate: {0}", videodatarate));
				}
				if (CombinedTracksLength == 0)
				{
					props.Clear();
					obj.FindMatchingProperty("filesize", props, int.MaxValue);
					if (props.Count > 0)
					{
						CombinedTracksLength = (int)props[0].GetNumber();
						Logger.Log(string.Format("Set CombinedTracksLength from filesize: {0}", CombinedTracksLength));
					}
				}
				if (CombinedTracksLength == 0)
				{
					props.Clear();
					obj.FindMatchingProperty("datasize", props, int.MaxValue);
					if (props.Count > 0)
					{
						CombinedTracksLength = (int)props[0].GetNumber();
						Logger.Log(string.Format("Set CombinedTracksLength from datasize: {0}", CombinedTracksLength));
					}
				}
			}
		}
	}
}
