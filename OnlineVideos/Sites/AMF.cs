using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace OnlineVideos.AMF
{
    enum AMF0Type
    {
        NumberMarker = 0x00,
        BooleanMarker = 0x01,
        StringMarker = 0x02,
        ObjectMarker = 0x03,
        MovieClipMarker = 0x04,
        NullMarker = 0x05,
        UndefinedMarker = 0x06,
        ReferenceMarker = 0x07,
        EcmaArrayMarker = 0x08,
        ObjectEndMarker = 0x09,
        StrictArrayMarker = 0x0A,
        DateMarker = 0x0B,
        LongStringMMarker = 0x0C,
        UnsupportedMarker = 0x0D,
        RecordsetMarker = 0x0E,
        XmlDocumentMarker = 0x0F,
        TypedObjectMarker = 0x10,
        AvmplusObjectMarker = 0x11
    };

    enum AMF3Type
    {
        UndefinedMarker = 0x00,
        NullMarker = 0x01,
        FalseMarker = 0x02,
        TrueMarker = 0x03,
        IntegerMarker = 0x04,
        DoubleMarker = 0x05,
        StringMarker = 0x06,
        XmlDocMarker = 0x07,
        DateMarker = 0x08,
        ArrayMarker = 0x09,
        ObjectMarker = 0x0A,
        XmlMarker = 0x0B,
        ByteArrayMarker = 0x0C
    };

    public class AMFObject
    {
        public string Name;
        public Dictionary<string, object> Properties;
        private List<string> keys = new List<string>();

        public AMFObject(string objName)
        {
            Name = objName;
            Properties = new Dictionary<string, object>();
        }

        public AMFObject(string objName, Dictionary<string, object> properties)
        {
            Name = objName;
            this.Properties = properties;
        }

        public AMFObject Clone()
        {
            AMFObject res = new AMFObject(Name);
            res.Properties = new Dictionary<string, object>();
            for (int i = 0; i < keys.Count; i++)
                res.keys.Add(keys[i]);
            return res;
        }

        public void Add(string key, object value)
        {
            Properties.Add(key, value);
        }

        public override string ToString()
        {
            return string.Format("[AMFObject Name={0}, Properties={1}]",
                                 Name,
                                 Properties.Aggregate(new StringBuilder(), (sb, kvp) => sb.AppendFormat("{0}='{1}' ", kvp.Key, kvp.Value)).ToString());
        }

        public AMFObject GetObject(string key)
        {
            if (Properties.ContainsKey(key))
                return Properties[key] as AMFObject;
            return null;
        }

        public AMFArray GetArray(string key)
        {
            if (Properties.ContainsKey(key))
                return Properties[key] as AMFArray;
            return null;
        }

        public string GetStringProperty(string key)
        {
            if (Properties.ContainsKey(key))
                return Properties[key] as string;
            return null;
        }

        public int GetIntProperty(string key)
        {
            if (Properties.ContainsKey(key))
            {
                object obj = Properties[key];
                if (obj is int)
                    return (int)obj;
                return -1;
            }
            return -1;
        }

        public double GetDoubleProperty(string key)
        {
            if (Properties.ContainsKey(key))
            {
                object obj = Properties[key];
                if (obj is double)
                    return (double)obj;
                return Double.NaN;
            }
            return Double.NaN;
        }

        public void AddKey(string key)
        {
            keys.Add(key);
        }

        public int KeyCount
        {
            get
            {
                return keys.Count;
            }
        }

        public string Key(int i)
        {
            return keys[i];
        }

        public static AMFObject GetResponse(string url, byte[] postData)
        {
            Log.Debug("get webdata from {0}", url);

            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            if (request == null) return null;
            request.Method = "POST";
            request.ContentType = "application/x-amf";
            request.UserAgent = OnlineVideoSettings.Instance.UserAgent;
            request.Timeout = 15000;
            request.ContentLength = postData.Length;
            request.ProtocolVersion = HttpVersion.Version10;
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(postData, 0, postData.Length);
            requestStream.Close();
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                Stream responseStream;
                if (response.ContentEncoding.ToLower().Contains("gzip"))
                    responseStream = new System.IO.Compression.GZipStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else if (response.ContentEncoding.ToLower().Contains("deflate"))
                    responseStream = new System.IO.Compression.DeflateStream(response.GetResponseStream(), System.IO.Compression.CompressionMode.Decompress);
                else
                    responseStream = response.GetResponseStream();


                AMFDeserializer des = new AMFDeserializer(responseStream);
                AMFObject obj = des.Deserialize();
                return obj;
            }

        }

    }

    public class AMFArray : IEnumerable<AMFObject>
    {
        private List<object> objs;
        private Dictionary<string, object> strs;

        public AMFArray()
        {
            objs = new List<object>();
        }

        public AMFArray(List<object> objs)
        {
            this.objs = objs;
        }

        public AMFArray(Dictionary<string, object> strs)
        {
            this.strs = strs;
        }

        public void Add(AMFObject obj)
        {
            objs.Add(obj);
        }

        public AMFObject GetObject(int index)
        {
            return objs[index] as AMFObject;
        }

        public AMFObject GetObject(string key)
        {
            if (strs.ContainsKey(key))
                return strs[key] as AMFObject;
            return null;
        }

        public int Count
        {
            get
            {
                return objs.Count;
            }
        }

        public override string ToString()
        {
            return string.Format("[AMFArray Objs={0}, Strs={1}]",
                                 objs != null ? objs.Aggregate(new StringBuilder(), (sb, item) => sb.AppendFormat("{0}, ", item)).ToString() : "",
                                 strs != null ? strs.Aggregate(new StringBuilder(), (sb, kvp) => sb.AppendFormat("{0}='{1}' ", kvp.Key, kvp.Value)).ToString() : "");
        }

        public IEnumerator<AMFObject> GetEnumerator()
        {
            foreach (AMFObject o in objs)
            {
                yield return o;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class AMFDeserializer
    {
        private BinaryReader reader;
        private List<AMFObject> classDefinitions = new List<AMFObject>();
        private List<string> strings = new List<string>();
        private bool isInAMF3 = false;

        public AMFDeserializer(Stream stream)
        {
            this.reader = new BinaryReader(stream);
        }

        public AMFObject Deserialize()
        {
            classDefinitions.Clear();
            strings.Clear();

            short version = ReadShort();//3
            if (version == 0)
                return DeserializeVersion0();
            int i = ReadShort();
            i = ReadShort();
            string s = ReadString();
            i = ReadShort();
            i = ReadShort();
            i = ReadShort();
            i = reader.ReadByte();//0x11
            if (i == (byte)AMF0Type.AvmplusObjectMarker)
                isInAMF3 = true;

            object res = ReadParamValue();
            return res as AMFObject;
        }

        private AMFObject DeserializeVersion0()
        {
            int i = ReadShort();
            i = ReadShort();
            string s = ReadString();
            s = ReadString();
            i = ReadShort();
            i = ReadShort();
            object res = ReadParamValue();
            return res as AMFObject;
        }

        private int ReadUInt29()
        {
            int result = 0;
            int n = 0;
            bool cont;
            do
            {
                byte b = reader.ReadByte();
                n++;
                if (n == 4)
                    result = result << 8;
                else
                    result = result << 7;
                cont = n < 4 && b >= 0x80;
                if (cont)
                    b &= 0x7F;
                result |= b;
            } while (cont);

            return result;
        }

        private short ReadShort()
        {
            byte[] bytes = new byte[2];
            reader.Read(bytes, 0, 2);
            Array.Reverse(bytes);
            return BitConverter.ToInt16(bytes, 0);
        }

        private Int32 ReadInt32()
        {
            byte[] bytes = reader.ReadBytes(4);
            Array.Reverse(bytes);
            return BitConverter.ToInt32(bytes, 0);
        }

        private double ReadDouble()
        {
            byte[] bytes = reader.ReadBytes(8);
            Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }

        private bool ReadBoolean()
        {
            byte b = reader.ReadByte();
            return b != 0;
        }

        private string ReadString()
        {
            short len = ReadShort();
            byte[] bytes = new byte[len];
            reader.Read(bytes, 0, len);
            return Encoding.UTF8.GetString(bytes);
        }

        private DateTime ReadAmf3Date()
        {
            reader.ReadByte();
            double seconds = ReadDouble() / 1000;
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            DateTime res = origin.AddSeconds(seconds);
            return res;
        }

        private DateTime ReadAmf0Date()
        {
            double seconds = ReadDouble() / 1000;
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            DateTime res = origin.AddSeconds(seconds);
            int dummy = ReadShort();
            return res;
        }

        private string ReadParamName()
        {
            int l = ReadUInt29();
            bool referenced = (l & 1) == 0;
            l = (l >> 1);
            if (referenced) return strings[l];// "Referenced string nr. " + l.ToString() + "(=" + strings[l] + ")";
            if (l == 0)
                return String.Empty;
            byte[] bytes = new byte[l];
            reader.Read(bytes, 0, l);
            string s = Encoding.UTF8.GetString(bytes);
            strings.Add(s);
            return s;

        }

        private AMFArray ReadAmf3Array()
        {
            int v = ReadUInt29();
            string key = ReadParamName();
            AMFArray result;
            if (key == String.Empty)
            {
                List<object> objs = new List<object>();
                result = new AMFArray(objs);
                for (int i = 0; i < v >> 1; i++)
                    objs.Add(ReadParamValue());
            }
            else
            {
                Dictionary<string, object> strs = new Dictionary<string, object>();
                result = new AMFArray(strs);
                while (key != String.Empty)
                {
                    strs.Add(key, ReadParamValue());
                    key = ReadParamName();
                }

            }
            return result;
        }


        private AMFObject ReadAmf3Object()
        {
            AMFObject result;
            int v = ReadUInt29();
            if ((v & 3) == 1)
            {
                result = classDefinitions[v >> 2].Clone();
            }
            else
            {
                int nkeys = v >> 4;
                string name = ReadParamName();
                result = new AMFObject(name);

                for (int i = 0; i < nkeys; i++)
                    result.AddKey(ReadParamName());

                classDefinitions.Add(result);
            }


            for (int i = 0; i < result.KeyCount; i++)
            {
                object obj = ReadParamValue();
                //Console.WriteLine(result.keys[i] + '=' + (obj == null ? "null" : obj.ToString()));
                result.Properties.Add(result.Key(i), obj);
            }
            return result;
        }

        private AMFObject ReadAmf0AnonymousObject()
        {
            AMFObject result = new AMFObject(String.Empty);
            bool endFound = false;
            do
            {
                string key = this.ReadString();
                object obj = ReadParamValue();
                if (obj is AmfEndOfObject)
                    endFound = true;
                else
                    result.Add(key, obj);
            }
            while (!endFound);
            classDefinitions.Add(result);

            return result;
        }


        private AMFObject ReadAmf0Object()
        {
            string name = ReadString();
            // TODO: check if exists
            AMFObject result = ReadAmf0AnonymousObject();
            result.Name = name;
            return result;
        }

        private AMFArray ReadAmf0StrictArray()
        {
            List<object> objs = new List<object>();
            AMFArray result = new AMFArray(objs);

            int l = ReadInt32();
            for (int i = 0; i < l; i++)
            {
                objs.Add(ReadParamValue());
            }
            return result;

        }

        private object ReadParamValue()
        {
            if (isInAMF3)
            {
                AMF3Type typ = (AMF3Type)reader.ReadByte();
                switch (typ)
                {
                    case AMF3Type.NullMarker: return null;
                    case AMF3Type.ArrayMarker: return ReadAmf3Array();
                    case AMF3Type.StringMarker: return ReadParamName();
                    case AMF3Type.DoubleMarker: return ReadDouble();
                    case AMF3Type.ObjectMarker: return ReadAmf3Object();
                    case AMF3Type.FalseMarker: return false;
                    case AMF3Type.TrueMarker: return true;
                    case AMF3Type.IntegerMarker: return ReadUInt29();
                    case AMF3Type.DateMarker: return ReadAmf3Date();
                    default:
                        throw new NotImplementedException();

                }
            }
            else
            {
                AMF0Type typ = (AMF0Type)reader.ReadByte();
                switch (typ)
                {
                    case AMF0Type.NullMarker: return null;
                    case AMF0Type.StrictArrayMarker: return ReadAmf0StrictArray();
                    case AMF0Type.StringMarker: return ReadString();
                    case AMF0Type.NumberMarker: return ReadDouble();
                    case AMF0Type.TypedObjectMarker: return ReadAmf0Object();
                    case AMF0Type.ObjectEndMarker: return new AmfEndOfObject();
                    case AMF0Type.ObjectMarker: return ReadAmf0AnonymousObject();
                    case AMF0Type.BooleanMarker: return ReadBoolean();
                    case AMF0Type.DateMarker: return ReadAmf0Date();
                    default:
                        throw new NotImplementedException();

                }
            }
        }

        private class AmfEndOfObject
        {
        }
    }

    public class AMFSerializer
    {
        private List<byte> output;

        public AMFSerializer()
        {
            output = new List<byte>();
        }

        public byte[] Serialize(AMFObject obj, string hash)
        {
            output.Clear();

            OutShort(3); //version
            OutShort(0); //headercount
            OutShort(1); //responsecount
            OutString("com.brightcove.experience.ExperienceRuntimeFacade.getDataForExperience");
            OutString("/1");
            OutShort(0); //??

            int lengthpos = output.Count;
            OutShort(0xA00); //array
            OutShort(0); //??
            OutShort(0x202); //??
            OutString(hash);

            output.Add((byte)AMF0Type.AvmplusObjectMarker); //switch to AMF3?
            OutParamValue(obj);
            OutShort((short)(output.Count - lengthpos), lengthpos);

            return output.ToArray();
        }

        public byte[] Serialize2(string target, object[] values)
        {
            output.Clear();

            OutShort(3); //version
            OutShort(0); //headercount
            OutShort(1); //responsecount
            OutString(target);
            OutString("/1");
            OutShort(0); //??

            int lengthpos = output.Count;
            output.Add((byte)AMF0Type.StrictArrayMarker);
            OutInt32(values.Length);
            foreach (object obj in values)
            {
                if (obj is String)
                {
                    output.Add((byte)AMF0Type.StringMarker);
                    OutString((String)obj);
                }
                else if (obj is double)
                {
                    output.Add((byte)AMF0Type.NumberMarker);
                    byte[] bytes;
                    if (double.IsNaN((double)obj))
                        bytes = new byte[8] { 0x7F, 0xFF, 0xFF, 0xFF, 0xE0, 0, 0, 0 };
                    else
                    {
                        bytes = BitConverter.GetBytes((double)obj);
                        Array.Reverse(bytes);
                    }
                    output.AddRange(bytes);
                }
                else if (obj == null)
                    output.Add((byte)AMF0Type.NullMarker);
                else
                    throw new NotImplementedException();
            }
            OutShort((short)(output.Count - lengthpos), lengthpos);

            return output.ToArray();
        }

        private void OutInt32(Int32 value, int atpos = -1)
        {
            byte[] bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
            if (atpos == -1)
                output.AddRange(bytes);
            else
                output.InsertRange(atpos, bytes);
        }

        private void OutShort(short value, int atpos = -1)
        {
            byte[] bytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(value));
            if (atpos == -1)
                output.AddRange(bytes);
            else
                output.InsertRange(atpos, bytes);
        }

        private void OutString(string value)
        {
            OutShort((short)value.Length);
            output.AddRange(Encoding.UTF8.GetBytes(value));
        }

        private void OutParamValue(AMFObject obj)
        {
            output.Add((byte)AMF3Type.ObjectMarker);
            int v = obj.Properties.Count << 4 | 3;
            OutUInt29(v);
            OutParamName(obj.Name);
            OutDict(obj.Properties);
        }

        private void OutUInt29(int paramValue)
        {
            if (paramValue < 0 || paramValue >= 0x40000000) throw new ArgumentOutOfRangeException();
            if (paramValue < 0x80)
                output.Add((byte)paramValue);
            else
                if (paramValue < 0x4000)
                {
                    output.Add((byte)((paramValue >> 7) | 0x80));
                    output.Add((byte)(paramValue & 0x7F));
                }
                else
                    if (paramValue < 0x200000)
                    {
                        output.Add((byte)((paramValue >> 14) | 0x80));
                        output.Add((byte)((paramValue >> 7) & 0x7F | 0x80));
                        output.Add((byte)(paramValue & 0x7F));
                    }
                    else
                    {
                        output.Add((byte)((paramValue >> 22) | 0x80));
                        output.Add((byte)((paramValue >> 15) & 0x7F | 0x80));
                        output.Add((byte)(paramValue >> 8));
                        output.Add((byte)(paramValue & 0x7F));
                    }
        }

        private void OutParamValue(AMFArray obj)
        {
            output.Add((byte)AMF3Type.ArrayMarker);
            int v = obj.Count << 1 | 1;
            OutUInt29(v);
            output.Add(0x01);
            OutParamValue(obj.GetObject(0));
        }


        private void OutParamName(string paramName)
        {
            int v = paramName.Length << 1 | 1;
            OutUInt29(v);
            output.AddRange(Encoding.UTF8.GetBytes(paramName));
        }

        private void OutParamValue(string paramValue)
        {
            output.Add((byte)AMF3Type.StringMarker);
            OutParamName(paramValue);
        }

        private void OutParamValue(double paramValue)
        {
            output.Add((byte)AMF3Type.DoubleMarker);
            byte[] bytes;
            if (double.IsNaN(paramValue))
                bytes = new byte[8] { 0x7F, 0xFF, 0xFF, 0xFF, 0xE0, 0, 0, 0 };
            else
            {
                bytes = BitConverter.GetBytes(paramValue);
                Array.Reverse(bytes);
            }
            output.AddRange(bytes);
        }

        private void OutParamValue(int paramValue)
        {
            output.Add((byte)AMF3Type.IntegerMarker);
            OutUInt29(paramValue);
        }

        private void OutDict(Dictionary<string, object> properties)
        {
            foreach (KeyValuePair<string, object> kv in properties)
                OutParamName(kv.Key);

            foreach (KeyValuePair<string, object> kv in properties)
            {
                if (kv.Value is String)
                    OutParamValue((String)kv.Value);
                else if (kv.Value is double)
                    OutParamValue((double)kv.Value);
                else if (kv.Value is AMFArray)
                    OutParamValue((AMFArray)kv.Value);
                else if (kv.Value == null)
                    output.Add((byte)AMF3Type.NullMarker);
                else if (kv.Value is int)
                    OutParamValue((int)kv.Value);
                else
                    throw new NotImplementedException();
            }
        }
    }
}
