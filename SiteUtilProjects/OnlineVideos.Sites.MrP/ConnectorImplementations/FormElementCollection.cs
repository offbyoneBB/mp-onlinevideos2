using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.WebAutomation.ConnectorImplementations
{
        /// <summary>
        /// Represents a combined list and collection of Form Elements.
        /// </summary>
        public class FormElementCollection : Dictionary<string, string>
        {
            /// <summary>
            /// Constructor. Parses the HtmlDocument to get all form input elements. 
            /// </summary>
            public FormElementCollection(HtmlDocument htmlDoc)
            {
                var inputs = htmlDoc.DocumentNode.Descendants("input");
                foreach (var element in inputs)
                {
                    AddInputElement(element);
                }

                var menus = htmlDoc.DocumentNode.Descendants("select");
                foreach (var element in menus)
                {
                    AddMenuElement(element);
                }

                var textareas = htmlDoc.DocumentNode.Descendants("textarea");
                foreach (var element in textareas)
                {
                    AddTextareaElement(element);
                }
            }
private void AddInputElement(HtmlNode element)
{
    string name = element.GetAttributeValue("name", "");
    string value = element.GetAttributeValue("value", "");
    string type = element.GetAttributeValue("type", "");            

    if (string.IsNullOrEmpty(name)) return;

    switch (type.ToLower())
    {
        case "checkbox": 
        case "radio":
            if (!ContainsKey(name)) Add(name, "");
            string isChecked = element.GetAttributeValue("checked", "unchecked"); 
            if (!isChecked.Equals("unchecked")) this[name] = value;
            break; 
        default: 
            Add(name, value); 
            break;
    }            
}
private void AddMenuElement(HtmlNode element)
{
    string name = element.GetAttributeValue("name", "");
    var options = element.Descendants("option");

    if (string.IsNullOrEmpty(name)) return;

    // choose the first option as default
    var firstOp = options.First();
    string defaultValue = firstOp.GetAttributeValue("value", firstOp.NextSibling.InnerText); 

    Add(name, defaultValue); 

    // check if any option is selected
    foreach (var option in options)
    {
        string selected = option.GetAttributeValue("selected", "notSelected");
        if (!selected.Equals("notSelected"))
        {
            string selectedValue = option.GetAttributeValue("value", option.NextSibling.InnerText);
            this[name] = selectedValue;
        }
    }
}
private void AddTextareaElement(HtmlNode element)
{
    string name = element.GetAttributeValue("name", "");
    if (string.IsNullOrEmpty(name)) return;
    Add(name, element.InnerText);
}
            /// <summary>
            /// Assembles all form elements and values to POST. Also html encodes the values.  
            /// </summary>
            public string AssemblePostPayload()
            {
                StringBuilder sb = new StringBuilder();
                foreach (var element in this)
                {
                    string value = Uri.EscapeDataString(element.Value);
                    sb.Append("&" + element.Key + "=" + value);
                }
                return sb.ToString().Substring(1);
            }
        }
}
