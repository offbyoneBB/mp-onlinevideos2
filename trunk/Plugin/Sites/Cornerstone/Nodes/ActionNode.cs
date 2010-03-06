using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace OnlineVideos.Sites.Cornerstone.Nodes
{
    [ScraperNode("action")]
    public class ActionNode: ScraperNode {


        public ActionNode(XmlNode xmlNode, bool debugMode)
            : base(xmlNode, debugMode) {
        }

        public override void Execute(Dictionary<string, string> variables) {
            executeChildren(variables);
        }


    }
}
