using System;
using System.Collections.Generic;
using System.Text;

namespace RTMP_LIB
{
    public class AMFObjectProperty
    {
        internal string m_strName = string.Empty;
        internal AMFDataType m_type;
        internal double m_dNumVal;
        internal AMFObject m_objVal;
        internal string m_strVal;
        internal ushort p_UTCoffset;
        internal double p_number;

        public AMFObjectProperty()
        {
            Reset();
        }

        public string GetPropName()
        {
            return m_strName;
        }

        public AMFDataType GetDataType()
        {
            return m_type;
        }

        public double GetNumber()
        {
            return m_dNumVal;
        }

        public bool GetBoolean()
        {
            return m_dNumVal != 0;
        }

        public string GetString()
        {
            return m_strVal;
        }

        public AMFObject GetObject()
        {
            return m_objVal;
        }

        public bool IsValid()
        {
            return (m_type != AMFDataType.AMF_INVALID);
        }

        public int Decode(byte[] pBuffer, int bufferOffset, int nSize, bool bDecodeName)
        {
            int nOriginalSize = nSize;

            if (nSize == 0 || pBuffer == null)
                return -1;

            if (pBuffer[bufferOffset] == 0x05)
            {
                m_type = AMFDataType.AMF_NULL;
                return 1;
            }

            if (bDecodeName && nSize < 4) // at least name (length + at least 1 byte) and 1 byte of data
                return -1;

            if (bDecodeName)
            {
                ushort nNameSize = RTMP.ReadInt16(pBuffer, bufferOffset);
                if (nNameSize > nSize - (short)sizeof(short))
                    return -1;

                m_strName = RTMP.ReadString(pBuffer, bufferOffset);
                nSize -= sizeof(short) + m_strName.Length;
                bufferOffset += sizeof(short) + m_strName.Length;
            }

            if (nSize == 0)
                return -1;

            nSize--;

            switch (pBuffer[bufferOffset])
            {
                case (byte)AMFDataType.AMF_NUMBER:
                    if (nSize < (int)sizeof(double))
                        return -1;
                    m_dNumVal = RTMP.ReadNumber(pBuffer, bufferOffset + 1);
                    nSize -= sizeof(double);
                    m_type = AMFDataType.AMF_NUMBER;
                    break;
                case (byte)AMFDataType.AMF_BOOLEAN:
                    if (nSize < 1)
                        return -1;
                    m_dNumVal = Convert.ToDouble(RTMP.ReadBool(pBuffer, bufferOffset + 1));
                    nSize--;
                    m_type = AMFDataType.AMF_BOOLEAN;
                    break;
                case (byte)AMFDataType.AMF_STRING:
                    {
                        ushort nStringSize = RTMP.ReadInt16(pBuffer, bufferOffset + 1);
                        if (nSize < nStringSize + (int)sizeof(short))
                            return -1;
                        m_strVal = RTMP.ReadString(pBuffer, bufferOffset + 1);
                        nSize -= sizeof(short) + nStringSize;
                        m_type = AMFDataType.AMF_STRING;
                        break;
                    }
                case (byte)AMFDataType.AMF_OBJECT:
                    {
                        m_objVal = new AMFObject();
                        int nRes = m_objVal.Decode(pBuffer, bufferOffset + 1, nSize, true);
                        if (nRes == -1)
                            return -1;
                        nSize -= nRes;
                        m_type = AMFDataType.AMF_OBJECT;
                        break;
                    }
                case (byte)AMFDataType.AMF_MOVIECLIP:
                    {
                        Logger.Log("AMF_MOVIECLIP reserved!");
                        return -1;
                    }
                case (byte)AMFDataType.AMF_NULL:
                case (byte)AMFDataType.AMF_UNDEFINED:
                case (byte)AMFDataType.AMF_UNSUPPORTED:
                    {
                        m_type = AMFDataType.AMF_NULL;
                        break;
                    }
                case (byte)AMFDataType.AMF_REFERENCE:
                    {
                        Logger.Log("AMF_REFERENCE not supported!");
                        return -1;
                    }
                case (byte)AMFDataType.AMF_ECMA_ARRAY:
                    {
                        //int nMaxIndex = RTMP_LIB::CRTMP::ReadInt32(pBuffer+1); // can be zero for unlimited
                        nSize -= 4;

                        // next comes the rest, mixed array has a final 0x000009 mark and names, so its an object
                        m_objVal = new AMFObject();
                        int nRes = m_objVal.Decode(pBuffer, bufferOffset + 5, nSize, true);
                        if (nRes == -1)
                            return -1;
                        nSize -= nRes;
                        m_type = AMFDataType.AMF_OBJECT;
                        break;
                    }
                case (byte)AMFDataType.AMF_OBJECT_END:
                    {
                        return -1;
                    }
                case (byte)AMFDataType.AMF_STRICT_ARRAY:
                    {
                        int nArrayLen = RTMP.ReadInt32(pBuffer, bufferOffset + 1);
                        nSize -= 4;

                        m_objVal = new AMFObject();
                        int nRes = m_objVal.DecodeArray(pBuffer, bufferOffset + 5, nSize, nArrayLen, false);
                        if (nRes == -1)
                            return -1;
                        nSize -= nRes;
                        m_type = AMFDataType.AMF_OBJECT;
                        break;
                    }
                case (byte)AMFDataType.AMF_DATE:
                    {
                        if (nSize < 10) return -1;
                        p_number = RTMP.ReadNumber(pBuffer, bufferOffset + 1);
                        p_UTCoffset = RTMP.ReadInt16(pBuffer, bufferOffset + 9);
                        nSize -= 10;
                        break;
                    }
                case (byte)AMFDataType.AMF_LONG_STRING:
                    {
                        int nStringSize = RTMP.ReadInt32(pBuffer, bufferOffset + 1);
                        if (nSize < nStringSize + 4) return -1;
                        m_strVal = RTMP.ReadLongString(pBuffer, bufferOffset + 1);
                        nSize -= (4 + nStringSize);
                        m_type = AMFDataType.AMF_STRING;
                        break;
                    }
                case (byte)AMFDataType.AMF_RECORDSET:
                    {
                        Logger.Log("AMF_RECORDSET reserved!");
                        return -1;
                    }
                case (byte)AMFDataType.AMF_XML_DOC:
                    {
                        Logger.Log("AMF_XML_DOC not supported!");
                        return -1;
                    }
                case (byte)AMFDataType.AMF_TYPED_OBJECT:
                    {
                        Logger.Log("AMF_TYPED_OBJECT not supported!");
                        return -1;
                    }
                default:
                    Logger.Log(string.Format("unknown datatype {0}", pBuffer[bufferOffset]));
                    return -1;
            }

            return nOriginalSize - nSize;
        }

        public void Dump()
        {
            if (m_type == AMFDataType.AMF_INVALID)
            {
                Logger.Log("Property: INVALID");
                return;
            }

            if (m_type == AMFDataType.AMF_NULL)
            {
                Logger.Log("Property: NULL");
                return;
            }

            if (m_type == AMFDataType.AMF_OBJECT)
            {
                Logger.Log("Property: OBJECT ====>");
                m_objVal.Dump();
                return;
            }

            string strRes = "no-name. ";
            if (m_strName != string.Empty)
                strRes = "Name: " + m_strName + ",  ";

            string strVal;

            switch (m_type)
            {
                case AMFDataType.AMF_NUMBER:
                    strVal = string.Format("NUMBER: {0}", m_dNumVal);
                    break;
                case AMFDataType.AMF_BOOLEAN:
                    strVal = string.Format("BOOLEAN: {0}", m_dNumVal == 1.0 ? "TRUE" : "FALSE");
                    break;
                case AMFDataType.AMF_STRING:
                    strVal = string.Format("STRING: {0}", m_strVal.Length < 256 ? m_strVal : "Length: " + m_strVal.Length.ToString());
                    break;
                default:
                    strVal = string.Format("INVALID TYPE {0}", m_type);
                    break;
            }

            strRes += strVal;
            Logger.Log(string.Format("Property: {0}", strRes));
        }

        public void Encode(List<byte> output)
        {
            if (m_type == AMFDataType.AMF_INVALID)
                return;           

            switch (m_type)
            {
                case AMFDataType.AMF_NUMBER:
                    if (string.IsNullOrEmpty(m_strName))
                        RTMP.EncodeNumber(output, GetNumber());
                    else
                        RTMP.EncodeNumber(output, m_strName, GetNumber());                    
                    break;

                case AMFDataType.AMF_BOOLEAN:
                    if (string.IsNullOrEmpty(m_strName))
                        RTMP.EncodeBoolean(output, GetBoolean());
                    else
                        RTMP.EncodeBoolean(output, m_strName, GetBoolean());
                    break;

                case AMFDataType.AMF_STRING:
                    if (string.IsNullOrEmpty(m_strName))
                        RTMP.EncodeString(output, GetString());
                    else
                        RTMP.EncodeString(output, m_strName, GetString());
                    break;

                case AMFDataType.AMF_NULL:
                    output.Add(0x05);
                    break;

                case AMFDataType.AMF_OBJECT:
                    if (!string.IsNullOrEmpty(m_strName))
                    {
                        short length = System.Net.IPAddress.HostToNetworkOrder((short)m_strName.Length);
                        output.AddRange(BitConverter.GetBytes(length));
                        output.AddRange(Encoding.ASCII.GetBytes(m_strName));
                    }                    
                    GetObject().Encode(output);                    
                    break;

                default:
                    Logger.Log(string.Format("AMFObjectProperty.Encode invalid type: {0}", m_type));
                    break;
            }
        }

        public void Reset()
        {
            m_dNumVal = 0.0;
            m_strVal = "";
            m_objVal = null;
            m_type = AMFDataType.AMF_INVALID;
        }
    }
}
