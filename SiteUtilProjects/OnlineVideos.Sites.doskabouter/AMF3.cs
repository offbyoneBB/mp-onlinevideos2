using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;

namespace OnlineVideos.AMF3
{
    enum AMF3Type
    {
        UundefinedMarker = 0x00,
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

    public class AMF3Object
    {
        public string Name;
        public Dictionary<string, object> Properties;
        private List<string> keys = new List<string>();

        public AMF3Object(string objName)
        {
            Name = objName;
            Properties = new Dictionary<string, object>();
        }

        public AMF3Object(string objName, Dictionary<string, object> properties)
        {
            Name = objName;
            this.Properties = properties;
        }

        public AMF3Object Clone()
        {
            AMF3Object res = new AMF3Object(Name);
            res.Properties = new Dictionary<string, object>();
            for (int i = 0; i < keys.Count; i++)
                res.keys.Add(keys[i]);
            return res;
        }

        public void Add(string key, object value)
        {
            Properties.Add(key, value);
        }

        public AMF3Object GetObject(string key)
        {
            if (Properties.ContainsKey(key))
                return Properties[key] as AMF3Object;
            return null;
        }

        public AMF3Array GetArray(string key)
        {
            if (Properties.ContainsKey(key))
                return Properties[key] as AMF3Array;
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
    }

    public class AMF3Array
    {
        private List<object> objs;
        private Dictionary<string, object> strs;

        public AMF3Array()
        {
            objs = new List<object>();
        }

        public AMF3Array(List<object> objs)
        {
            this.objs = objs;
        }

        public AMF3Array(Dictionary<string, object> strs)
        {
            this.strs = strs;
        }

        public void Add(AMF3Object obj)
        {
            objs.Add(obj);
        }

        public AMF3Object GetObject(int index)
        {
            return objs[index] as AMF3Object;
        }

        public AMF3Object GetObject(string key)
        {
            if (strs.ContainsKey(key))
                return strs[key] as AMF3Object;
            return null;
        }

        public int Count
        {
            get
            {
                return objs.Count;
            }
        }
    }

    public class AMF3Deserializer
    {
        private BinaryReader reader;
        private List<AMF3Object> classDefinitions = new List<AMF3Object>();
        private List<string> strings = new List<string>();

        public AMF3Deserializer(Stream stream)
        {
            this.reader = new BinaryReader(stream);
        }

        public AMF3Object Deserialize()
        {
            classDefinitions.Clear();
            strings.Clear();

            short version = ReadShort();//3
            int i = ReadShort();
            i = ReadShort();
            string s = ReadString();
            i = ReadShort();
            i = ReadShort();
            i = ReadShort();
            i = reader.ReadByte();//0x11
            object res = ReadParamValue();
            return res as AMF3Object;
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

        private double ReadDouble()
        {
            byte[] bytes = reader.ReadBytes(8);
            Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }

        private string ReadString()
        {
            short len = ReadShort();
            byte[] bytes = new byte[len];
            reader.Read(bytes, 0, len);
            return Encoding.UTF8.GetString(bytes);
        }

        private DateTime ReadDate()
        {
            reader.ReadByte();
            double seconds = ReadDouble() / 1000;
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            DateTime res = origin.AddSeconds(seconds);
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

        private AMF3Array ReadAmf3Array()
        {
            int v = ReadUInt29();
            string key = ReadParamName();
            AMF3Array result;
            if (key == String.Empty)
            {
                List<object> objs = new List<object>();
                result = new AMF3Array(objs);
                for (int i = 0; i < v >> 1; i++)
                    objs.Add(ReadParamValue());
            }
            else
            {
                Dictionary<string, object> strs = new Dictionary<string, object>();
                result = new AMF3Array(strs);
                while (key != String.Empty)
                {
                    strs.Add(key, ReadParamValue());
                    key = ReadParamName();
                }

            }
            return result;
        }


        private AMF3Object ReadAmf3Object()
        {
            AMF3Object result;
            int v = ReadUInt29();
            if ((v & 3) == 1)
            {
                result = classDefinitions[v >> 2].Clone();
            }
            else
            {
                int nkeys = v >> 4;
                string name = ReadParamName();
                result = new AMF3Object(name);

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

        private object ReadParamValue()
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
                case AMF3Type.DateMarker: return ReadDate();
                default:
                    throw new NotImplementedException();

            }
        }
    }

    public class AMF3Serializer
    {
        private List<byte> output;

        public AMF3Serializer()
        {
            output = new List<byte>();
        }

        public byte[] Serialize(AMF3Object obj, string hash)
        {
            output.Clear();

            OutShort(3); //version
            OutShort(0); //headercount
            OutShort(1); //responsecount
            OutString("com.brightcove.experience.ExperienceRuntimeFacade.getDataForExperience");
            OutString("/1");
            OutShort(0); //??

            int lengthpos = output.Count;
            OutShort(0xA00); //??
            OutShort(0); //??
            OutShort(0x202); //??
            OutString(hash);

            output.Add(0x11); //switch to AMF3?
            OutParamValue(obj);
            OutShort((short)(output.Count - lengthpos), lengthpos);

            return output.ToArray();
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

        private void OutParamValue(AMF3Object obj)
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

        private void OutParamValue(AMF3Array obj)
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
                else if (kv.Value is AMF3Array)
                    OutParamValue((AMF3Array)kv.Value);
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
