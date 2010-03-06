using System;
using System.Collections.Generic;
using System.Text;
using Cornerstone.Tools;
using System.Xml;
using System.Threading;

namespace OnlineVideos.Sites.Cornerstone.Nodes
{
    [ScraperNode("distance")]
    public class DistanceNode : ScraperNode {
        public string String1 {
            get { return string1; }
        } protected string string1;

        public string String2 {
            get { return string2; }
        } protected string string2;

        public DistanceNode(XmlNode xmlNode, bool debugMode)
            : base(xmlNode, debugMode) {

            // Load attributes
            foreach (XmlAttribute attr in xmlNode.Attributes) {
                switch (attr.Name) {
                    case "string1":
                        string1 = attr.Value;
                        break;
                    case "string2":
                        string2 = attr.Value;
                        break;
                }
            }


            // Validate STRING1 attribute
            if (string1 == null) {
                logger.Error("Missing STRING1 attribute on: " + xmlNode.OuterXml);
                loadSuccess = false;
                return;
            }

            // Validate STRING2 attribute
            if (string2 == null) {
                logger.Error("Missing STRING2 attribute on: " + xmlNode.OuterXml);
                loadSuccess = false;
                return;
            }
        }

        public override void Execute(Dictionary<string, string> variables) {
            if (DebugMode) logger.Debug("executing distance: " + xmlNode.OuterXml);

            string parsedString1 = parseString(variables, string1);
            string parsedString2 = parseString(variables, string2);
            if (DebugMode) logger.Debug("executing distance: " + parsedString1 + " vs. " + parsedString2);

            int distance = AdvancedStringComparer.Levenshtein(parsedString1, parsedString2);

            setVariable(variables, parseString(variables, Name), distance.ToString());
        }

    }
}
