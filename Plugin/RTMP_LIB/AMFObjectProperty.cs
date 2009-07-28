using System;
using System.Collections.Generic;
using System.Text;

namespace RTMP_LIB
{
    public class AMFObjectProperty
    {
        string m_strName;
        AMFDataType m_type;
        double m_dNumVal;
        AMFObject m_objVal;
        string m_strVal;

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
                short nNameSize = RTMP.ReadInt16(pBuffer, bufferOffset);
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
                case 0x00: //AMF_NUMBER:
                    if (nSize < (int)sizeof(double))
                        return -1;
                    m_dNumVal = RTMP.ReadNumber(pBuffer, bufferOffset + 1);
                    nSize -= sizeof(double);
                    m_type = AMFDataType.AMF_NUMBER;
                    break;
                case 0x01: //AMF_BOOLEAN:
                    if (nSize < 1)
                        return -1;
                    m_dNumVal = Convert.ToDouble(RTMP.ReadBool(pBuffer, bufferOffset + 1));
                    nSize--;
                    m_type = AMFDataType.AMF_BOOLEAN;
                    break;
                case 0x02: //AMF_STRING:
                    {
                        short nStringSize = RTMP.ReadInt16(pBuffer, bufferOffset + 1);
                        if (nSize < nStringSize + (int)sizeof(short))
                            return -1;
                        m_strVal = RTMP.ReadString(pBuffer, bufferOffset + 1);
                        nSize -= sizeof(short) + nStringSize;
                        m_type = AMFDataType.AMF_STRING;
                        break;
                    }
                case 0x03: //AMF_OBJECT:
                    {
                        m_objVal = new AMFObject();
                        int nRes = m_objVal.Decode(pBuffer, bufferOffset + 1, nSize, true);
                        if (nRes == -1)
                            return -1;
                        nSize -= nRes;
                        m_type = AMFDataType.AMF_OBJECT;
                        break;
                    }
                case 0x08: // AMF_MIXEDARRAY
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
                case 0x0A: // AMF_ARRAY
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
                    strVal = string.Format("STRING: {0}", m_strVal);
                    break;
                default:
                    strVal = string.Format("INVALID TYPE {0}", m_type);
                    break;
            }

            strRes += strVal;
            Logger.Log(string.Format("Property: {0}", strRes));
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
