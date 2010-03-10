using System;
using System.Collections.Generic;
using System.Xml;
using System.Text;

namespace YahooMusicEngine
{
  public class Service
  {
    private string _id;
    public string Id
    {
      get { return _id; }
      set { _id = value; }
    }

    private int _rating;

    public int Rating
    {
      get { return _rating; }
      set { _rating = value; }
    }

    private string name;

    public string Name
    {
      get { return name; }
      set { name = value; }
    }

    public virtual void ParseXml(XmlNode node)
    {

      Id = ParseXmlAttributeAsString(node, "id", string.Empty);
      Rating = ParseXmlAttributeAsInt(node, "rating", -1);
      Name = ParseXmlAttributeAsString(node, "name", string.Empty);
    }

    /// <summary>
    /// Parses the XML attribute as int.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="attribute">The attribute.</param>
    /// <param name="defvalue">The defvalue.</param>
    /// <returns></returns>
    public int ParseXmlAttributeAsInt(XmlNode node, string attribute, int defvalue)
    {
      if (node.Attributes[attribute] != null)
      {
        int i = defvalue;
        int.TryParse(node.Attributes[attribute].Value, out i);
        return i;
      }
      else
        return defvalue;
    }

    /// <summary>
    /// Parses the XML attribute as string.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="attribute">The attribute.</param>
    /// <param name="defvalue">The defvalue.</param>
    /// <returns></returns>
    public string ParseXmlAttributeAsString(XmlNode node, string attribute, string defvalue)
    {
      if (node == null)
        return defvalue;
      if (node.Attributes[attribute] != null)
      {
        return node.Attributes[attribute].Value;
      }
      else
        return defvalue;
    }

    /// <summary>
    /// Parses the XML attribute as bool.
    /// </summary>
    /// <param name="node">The node.</param>
    /// <param name="attribute">The attribute.</param>
    /// <param name="defvalue">if set to <c>true</c> [defvalue].</param>
    /// <returns></returns>
    public bool ParseXmlAttributeAsBool(XmlNode node, string attribute, bool defvalue)
    {
      if (node.Attributes[attribute] != null)
      {
        if (node.Attributes[attribute].Value == "1")
          return true;
        else
          return false;
      }
      else
        return defvalue;
    }

  }
}
