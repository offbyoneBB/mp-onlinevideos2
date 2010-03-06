using System;
using System.Collections.Generic;
using System.Text;

namespace OnlineVideos.Sites.Cornerstone.CustomTypes
{
  public class StringList: DynamicList<string>, IStringSourcedObject {

    public StringList() {
    }

    public StringList(string createStr) {
      LoadFromString(createStr);
    }

    public void LoadFromString(string strList) {
      int startIndex = 0;
      while (startIndex < strList.Length) {
        // find the start index of this token
        while (startIndex < strList.Length && strList[startIndex] == '|')
          startIndex++;

        // figure it's length
        int len = 0;
        while (startIndex + len < strList.Length && strList[startIndex + len] != '|')
          len++;

        // store the token
        string token = strList.Substring(startIndex, len).Trim();
        if (startIndex < strList.Length && token.Length > 0)
          Add(token);

        startIndex += len;
      }
    }

    public override string ToString() {
      string rtn = "";

      if (this.Count > 0)
        rtn += "|";

      foreach (string currItem in this) {
        rtn += currItem;
        rtn += "|";
      }
            
      return rtn;
    }

    public string ToPrettyString() {
      return ToPrettyString(this.Count);
    }

    public string ToPrettyString(int max) {
      if (this.Count == 0)
        return "";

      StringBuilder prettyStr = new StringBuilder("");
            
      int limit = max;
      if (limit > this.Count)
        limit = this.Count;

      for (int i = 0; i < limit; i++) {
        prettyStr.Append(this[i] + ", ");
      }
      prettyStr.Remove(prettyStr.Length - 2, 2);

      return prettyStr.ToString();
    }

    public override bool Equals(System.Object obj) {
      if (obj == null) {
        return false;
      }

      StringList p = obj as StringList;
      if ((System.Object)p == null) {
        return false;
      }

      return p.ToString() == ToString();
    }

    public bool Equals(StringList p) {
      if ((object)p == null) {
        return false;
      }
      else {
        return p.ToString() == ToString();
      }
    }

    public override int GetHashCode() {
      return ToString().GetHashCode();
    }


  }
}