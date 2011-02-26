using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Reflection;
using System.Web;
using System.Text.RegularExpressions;
using System.Threading;

namespace OnlineVideos.Sites.Cornerstone{
    public abstract class ScraperNode {
      
      protected static LogProvider logger = new LogProvider();
        
      private static Dictionary<string, Type> nodeTypes;

        protected XmlNode xmlNode;
        protected List<ScraperNode> children;
        protected ScraperNodeAttribute nodeSettings;

        #region Properties
        public string NodeType {
            get {
                foreach (Attribute currAttr in this.GetType().GetCustomAttributes(true))
                    if (currAttr is ScraperNodeAttribute)
                        return ((ScraperNodeAttribute)currAttr).NodeName;
                return null;
            }
        }

        public string Name {
            get { return name; }
        } 
        protected string name;

        public virtual bool LoadSuccess {
            get { return loadSuccess; }
        }
        protected bool loadSuccess;

        public bool DebugMode {
            get { return debugMode; }
        } private bool debugMode;

        #endregion

        #region Methods

        public ScraperNode(XmlNode xmlNode, bool debugMode) {
            this.xmlNode = xmlNode;
            children = new List<ScraperNode>();
            this.debugMode = debugMode;
            loadSuccess = loadChildren();

            // try to load our node attrbute
            foreach(Attribute currAttr in this.GetType().GetCustomAttributes(true)) 
                if (currAttr is ScraperNodeAttribute) {
                    nodeSettings = (ScraperNodeAttribute) currAttr;
                    continue;
                }

            if (nodeSettings.LoadNameAttribute) {

                // Load attributes
                foreach (XmlAttribute attr in xmlNode.Attributes) {
                    switch (attr.Name) {
                        case "name":
                            name = attr.Value;
                            break;
                    }
                }

               // Validate NAME attribute
                if (name == null) {
                    logger.Error("Missing NAME attribute on: " + xmlNode.OuterXml);
                    loadSuccess = false;
                    return;
                }

                // if it's a bad variable name we fail as well
                if (Name.Contains(" ")) {
                    logger.Error("Invalid NAME attribute (no spaces allowed) \"" + Name + "\" for " + xmlNode.OuterXml);
                    loadSuccess = false;
                    return;
                }
            }
        }

        public abstract void Execute(Dictionary<string, string> variables);
        
        protected virtual void setVariable(Dictionary<string, string> variables, string key, string value) {
            variables[key] = value;
            if (value.Length < 500 && DebugMode) logger.Debug("Assigned variable: " + key + " = " + value);
        }

        protected virtual void removeVariable(Dictionary<string, string> variables, string key) {
            variables.Remove(key);
            removeArrayValues(variables, key);
            if (DebugMode) logger.Debug("Removed variable: " + key);
        }

        private void removeArrayValues(Dictionary<string, string> variables, string key) {
            int count = 0;
            while (variables.ContainsKey(key + "[" + count + "]")) {
                removeVariable(variables, key + "[" + count + "]");
                count++;
            }
        }

        protected bool loadChildren() {
            bool success = true;

            children.Clear();
            foreach (XmlNode currXmlNode in xmlNode.ChildNodes) {
                ScraperNode newScraperNode = ScraperNode.Load(currXmlNode, debugMode);
                if (newScraperNode != null && newScraperNode.loadSuccess) 
                    children.Add(newScraperNode);
                
                if (newScraperNode != null && !newScraperNode.loadSuccess)
                    success = false;
            }

            return success;
        }

        protected void executeChildren(Dictionary<string, string> variables) {
            foreach (ScraperNode currChild in children) 
                currChild.Execute(variables);
        }

        // scans the given string and replaces any existing variables with their value
        protected string parseString(Dictionary<string, string> variables, string input) {
            StringBuilder output = new StringBuilder(input);
            int offset = 0;

            Regex variablePattern = new Regex(@"\${([^:{}]+)(?::([^}\(]+))?(?:\(([^\)]+)\))?}");
            MatchCollection matches = variablePattern.Matches(input);
            foreach (Match currMatch in matches) {
                string varName = "";
                string modifier = string.Empty;
                string value = string.Empty;
                string encodingStr = string.Empty;

                // get rid of the escaped variable string
                output.Remove(currMatch.Index + offset, currMatch.Length);

                // grab details for this parse
                varName = currMatch.Groups[1].Value;
                variables.TryGetValue(varName, out value);
                if (currMatch.Groups.Count >= 3)
                    modifier = currMatch.Groups[2].Value.ToLower();
                if (currMatch.Groups.Count >= 4)
                    encodingStr = currMatch.Groups[3].Value.ToLower();

                // if there is no variable for what was passed in we are done
                if (value == string.Empty || value == null) {
                    offset -= currMatch.Length;
                    continue;
                }

                // handle any modifiers
                if (modifier.Equals("safe")) {
                    // if we have an encoding string try to build an encoding object
                    Encoding encoding = null;
                    if (encodingStr != string.Empty) {
                        try { encoding = Encoding.GetEncoding(encodingStr); }
                        catch (ArgumentException) {
                            encoding = null;
                            logger.Error("Scraper script tried to use an invalid encoding for \"safe\" modifier");
                        }
                    }

                    if (encoding != null)
                        value = HttpUtility.UrlEncode(value, encoding).Replace("+", "%20");
                    else
                        value = HttpUtility.UrlEncode(value).Replace("+", "%20");

                }
                else if (modifier.Equals("htmldecode"))
                    value = HttpUtility.HtmlDecode(value);
                else if (modifier.Equals("striptags")) {
                    value = Regex.Replace(value, @"\n", string.Empty); // Remove all linebreaks
                    value = Regex.Replace(value, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase); // Replace HTML breaks with \n
                    value = Regex.Replace(value, @"</p>", "\n\n", RegexOptions.IgnoreCase); // Replace paragraph tags with \n\n
                    value = Regex.Replace(value, @"<.+?>", string.Empty); // Remove all other tags
                    value = Regex.Replace(value, @"\n{3,}", "\n\n"); // Trim newlines
                    value = Regex.Replace(value, @"\t{2,}", " ").Trim(); // Trim whitespace
                }

                output.Insert(currMatch.Index + offset, value);
                offset = offset - currMatch.Length + value.Length;
            }

            // if we did some replacements search again to check forembeded variables
            if (matches.Count > 0)
                return parseString(variables, output.ToString());
            else return output.ToString();
        }

        #endregion

        #region Static Methods

        static ScraperNode() {
            nodeTypes = new Dictionary<string, Type>();
        }

        public static ScraperNode Load(XmlNode xmlNode, bool debugMode) {
          if (xmlNode == null || xmlNode.NodeType == XmlNodeType.Comment || xmlNode.NodeType == XmlNodeType.CDATA)
                return null;
            
            Type nodeType = null;
            string nodeTypeName = xmlNode.Name;


            // try to grab the type from our dictionary
            if (nodeTypes.ContainsKey(nodeTypeName))
                nodeType = nodeTypes[nodeTypeName];

            // if it's not there, search the assembly for the type
            else {
                Type[] typeList = Assembly.GetExecutingAssembly().GetTypes();
                foreach (Type currType in typeList)
                    foreach (Attribute currAttr in currType.GetCustomAttributes(true))
                        if (currAttr.GetType() == typeof(ScraperNodeAttribute) &&
                            nodeTypeName.Equals(((ScraperNodeAttribute)currAttr).NodeName)) {

                            // store our type and put it in our dictionary so we dont have to
                            // look it up again
                            nodeTypes[nodeTypeName] = currType;
                            nodeType = currType;
                            break;
                        }
            }

            // if we couldn't find anything log the unhandled node and exit
            if (nodeType == null) {
                logger.Error("Unsupported node type: " + xmlNode.OuterXml);
                return null;
            }

            
            // try to create a new scraper node object
            try {
                ConstructorInfo constructor = nodeType.GetConstructor(new Type[] { typeof(XmlNode), typeof(bool) });
                ScraperNode newNode = (ScraperNode)constructor.Invoke(new object[] { xmlNode, debugMode });
                return newNode;
            }
            catch (Exception e) {
                if (e.GetType() == typeof(ThreadAbortException))
                    throw e;

                logger.Error("Error instantiating ScraperNode based on: " + xmlNode.OuterXml, e);
                return null;
            }


        }
        
        #endregion
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ScraperNodeAttribute : Attribute {
        private string nodeName;

        public string NodeName {
            get { return nodeName; }
        }

        public bool LoadNameAttribute {
            get { return loadNameAttribute; }
            set { loadNameAttribute = value; }
        } private bool loadNameAttribute = true;

        public ScraperNodeAttribute(string nodeName) {
            this.nodeName = nodeName;
        }
    }
}
