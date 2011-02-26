using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Threading;

namespace OnlineVideos.Sites.Cornerstone.Nodes
{
    [ScraperNode("set")]
    public class SetNode: ScraperNode {
        #region Properties

        public string Value {
            get { return value; }
        } protected String value;

        #endregion

        #region Methods

        public SetNode(XmlNode xmlNode, bool debugMode)
            : base(xmlNode, debugMode) {

            if (DebugMode) logger.Debug("executing set: " + xmlNode.OuterXml);

            // Load attributes
            foreach (XmlAttribute attr in xmlNode.Attributes) {
                switch (attr.Name) {
                    case "value":
                        value = attr.Value;
                        break;
                }
            }

            // get the innervalue
            string innerValue = xmlNode.InnerText.Trim();

            // Validate TEST attribute
            if (value == null) {
                value = innerValue;
                if (innerValue.Equals(String.Empty)) {
                    logger.Error("Missing VALUE attribute on: " + xmlNode.OuterXml);
                    loadSuccess = false;
                    return;
                }
            } else if (!innerValue.Equals(String.Empty)) {
                logger.Error("Ambiguous assignment on: " + xmlNode.OuterXml);
                loadSuccess = false;
                return;
            }

        }

        public override void Execute(Dictionary<string, string> variables) {
            setVariable(variables, parseString(variables, Name), parseString(variables, value));
        }

        #endregion
    }
}
