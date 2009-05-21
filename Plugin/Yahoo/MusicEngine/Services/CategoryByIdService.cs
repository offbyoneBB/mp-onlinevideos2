using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using YahooMusicEngine.Entities;

namespace YahooMusicEngine.Services
{
  public class CategoryByIdService : IService
  {

    #region IService Members

    string IService.ServiceName
    {
      get { return "category"; }
    }

    string IService.Resource
    {
      get { return string.Format("item/{0}", ID); }
    }

    public Dictionary<string, string> Params
    {
      get
      {
        Dictionary<string, string> _params = new Dictionary<string, string>(); ;
        return _params;
      }
      set
      {

      }
    }

    public void Parse(System.Xml.XmlDocument doc)
    {
      items.Clear();
      XmlNodeList bodynodes = doc.SelectNodes("Categories/CategoryType/Category");
      foreach (XmlNode node in bodynodes)
      {
        ParseXmlNode(node, Items, null);
      }
    }

    private void ParseXmlNode(XmlNode node, List<CategoryEntity> catlis, CategoryEntity parent)
    {
      CategoryEntity cat = new CategoryEntity(node);
      cat.Parent = parent;
      XmlNodeList childs = node.SelectNodes("Category");
      foreach (XmlNode chnode in childs)
      {
        ParseXmlNode(chnode, cat.Childs, cat);
      }
      catlis.Add(cat);
    }
    #endregion

    private string iD;
    /// <summary>
    /// 	Category ID that should serve as the root of the tree.
    /// </summary>
    /// <value>The root ID.</value>
    public string ID
    {
      get { return iD; }
      set { iD = value; }
    }

    private CategoryTreeTypes type;

    /// <summary>
    /// Type of category to return.
    /// </summary>
    /// <value>The type.</value>
    public CategoryTreeTypes Type
    {
      get { return type; }
      set { type = value; }
    }

    private List<CategoryEntity> items;

    public List<CategoryEntity> Items
    {
      get { return items; }
      set { items = value; }
    }

    public CategoryByIdService()
    {
      Type = CategoryTreeTypes.genre;
      Items = new List<CategoryEntity>();
      ID = string.Empty;
    }

  }
}
