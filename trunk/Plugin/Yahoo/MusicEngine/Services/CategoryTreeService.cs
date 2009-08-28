using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using YahooMusicEngine.Entities;

namespace YahooMusicEngine.Services
{
  public class CategoryTreeService : IService
  {

    #region IService Members

    string IService.ServiceName
    {
      get { return "category"; }
    }

    string IService.Resource
    {
      get { return string.Format("tree/{0}",Type.ToString()); }
    }

    public Dictionary<string, string> Params
    {
      get
      {
        Dictionary<string, string> _params = new Dictionary<string, string>(); ;
        if (string.IsNullOrEmpty(RootID))
        {
          return _params;
        }
        else
        {
          _params.Add("rootID", RootID);
          return _params;
        }

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

    private void ParseXmlNode(XmlNode node,List<CategoryEntity> catlis,CategoryEntity parent)
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

    private string rootID;
    /// <summary>
    /// 	Category ID that should serve as the root of the tree.
    /// </summary>
    /// <value>The root ID.</value>
    public string RootID
    {
      get { return rootID; }
      set { rootID = value; }
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
	
    public CategoryTreeService()
    {
      Type = CategoryTreeTypes.genre;
      Items = new List<CategoryEntity>();
      RootID = string.Empty;
    }

    public CategoryEntity Find(string id)
    {
        foreach(CategoryEntity ce in Items)
        {
            if (ce.Id == id) return ce;

            if (ce.Childs != null && ce.Childs.Count > 0)
            {
                foreach (CategoryEntity sub_ce in ce.Childs)
                {
                    if (sub_ce.Id == id) return sub_ce;
                }
            }
        }
        return null;
    }

  }
}
