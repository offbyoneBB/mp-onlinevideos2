using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;
using System.Threading;

namespace OnlineVideos.Sites.Cornerstone.Nodes
{
  [ScraperNode("replace")]
  public class ReplaceNode : ScraperNode {
    #region Properties

    public string Input {
      get { return input; }
    } protected String input;

    public string Pattern {
      get { return pattern; }
    } protected String pattern;

    public string With {
      get { return replacement; }
    } protected String replacement;

    #endregion

    #region Methods
    public ReplaceNode(XmlNode xmlNode, bool debugMode)
      : base(xmlNode, debugMode) {

        // Load attributes
        foreach (XmlAttribute attr in xmlNode.Attributes) {
            switch (attr.Name) {
                case "input":
                    input = attr.Value;
                    break;
                case "pattern":
                    pattern = attr.Value;
                    break;
                case "with":
                    replacement = attr.Value;
                    break;
            }
        }

        // Validate INPUT attribute
        if (input == null) {
            logger.Error("Missing INPUT attribute on: " + xmlNode.OuterXml);
            loadSuccess = false;
            return;
        }

        // Validate PATTERN attribute
        if (pattern == null) {
            logger.Error("Missing PATTERN attribute on: " + xmlNode.OuterXml);
            loadSuccess = false;
            return;
        }

        // Validate WITH attribute
        if (replacement == null) {
            logger.Error("Missing WITH attribute on: " + xmlNode.OuterXml);
            loadSuccess = false;
            return;
        }

    }

    public override void Execute(Dictionary<string, string> variables) {
      if (DebugMode) logger.Debug("executing replace: " + xmlNode.OuterXml);
      string output = string.Empty;
      try { 
        output = Regex.Replace(parseString(variables, input), parseString(variables, pattern), parseString(variables, replacement));
      }
      catch (Exception e) {
        if (e.GetType() == typeof(ThreadAbortException))
          throw e;
        logger.Error("An error occured while executing replace.");
        return;
      } 
      setVariable(variables, parseString(variables, Name), output);
    }

    #endregion Methods
  }
}
