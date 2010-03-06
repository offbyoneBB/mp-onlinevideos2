using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineVideos.Sites.Cornerstone.CustomTypes
{
  public class ByteArray: IStringSourcedObject {

    public byte[] Data {
      get {
        if (_data.Length == 0)
          return null;
        else 
          return _data; 
      }
      set { _data = value; }
    }
    private byte[] _data = null;

    public ByteArray() {
    }

    public ByteArray(byte[] data) {
      _data = data;
    }

    #region IStringSourcedObject Members

    public void LoadFromString(string createStr) {
      List<byte> byteList = new List<byte>();
            
      int startIndex = 0;
      while (startIndex < createStr.Length) {
        // find the start index of this token
        while (startIndex < createStr.Length && createStr[startIndex] == '|')
          startIndex++;

        // figure it's length
        int len = 0;
        while (startIndex + len < createStr.Length && createStr[startIndex + len] != '|')
          len++;

        // store the token
        string token = createStr.Substring(startIndex, len).Trim();
        if (startIndex < createStr.Length && token.Length > 0)
          byteList.Add(Byte.Parse(token));

        startIndex += len;
      }

      _data = byteList.ToArray();
    }

    public override string ToString() {
      if (_data == null)
        return "";
            
      string rtn = "";

      if (_data.Length > 0)
        rtn += "|";

      foreach(byte currByte in _data) {
        rtn += currByte.ToString();
        rtn += "|";
      }

      return rtn;
    }

    #endregion
  }
}