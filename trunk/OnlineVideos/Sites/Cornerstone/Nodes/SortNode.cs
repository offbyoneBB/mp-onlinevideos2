using System;
using System.Collections.Generic;
using System.Text;
using Cornerstone.Tools;
using System.Xml;
using System.Threading;

namespace OnlineVideos.Sites.Cornerstone.Nodes
{
    [ScraperNode("sort")]
    public class SortNode : ScraperNode {
        public enum DirectionType { ASCENDING, DESCENDING }

        public DirectionType Direction {
            get { return direction; }
        } protected DirectionType direction;

        public string SortBy {
            get { return sortBy; }
        } protected string sortBy;

        public SortNode(XmlNode xmlNode, bool debugMode)
            : base(xmlNode, debugMode) {

            // Load attributes
            foreach (XmlAttribute attr in xmlNode.Attributes) {
                switch (attr.Name) {
                    case "direction":
                        string dirStr = attr.Value.ToLower().Trim();
                        if (dirStr == "desc" || dirStr == "descending")
                            direction = DirectionType.DESCENDING;
                        else if (dirStr == "asc" || dirStr == "ascending")
                            direction = DirectionType.ASCENDING;
                        else {
                            logger.Error("Invalid sort direction on: " + xmlNode.OuterXml);
                        }
                        break;
                    case "by":
                        sortBy = attr.Value;
                        break;

                }
            }

             // Validate BY attribute
            if (sortBy == null) {
                logger.Error("Missing BY attribute on: " + xmlNode.OuterXml);
                loadSuccess = false;
                return;
            }
        }

        public override void Execute(Dictionary<string, string> variables) {
            if (DebugMode) logger.Debug("executing sort: " + xmlNode.OuterXml);

            // get our initial parsed settings from the script
            string arrayName = parseString(variables, Name);
            string parsedSortBy = parseString(variables, SortBy);

            // build a list of the specified array
            int count = 0;
            List<WeakTypedObject> list = new List<WeakTypedObject>();
            while (variables.ContainsKey(arrayName + "[" + count + "]")) {
                WeakTypedObject newObj = new WeakTypedObject(arrayName + "[" + count + "]", variables);
                newObj.SortKey = parsedSortBy;
                list.Add(newObj);
                count++;
            }

            // sort and rewrite the sorted list to the variables dictionary
            list.Sort();
            count = 0;
            foreach (WeakTypedObject currObj in list) {
                variables[arrayName + "[" + count + "]"] = currObj.BaseValue;
                foreach (KeyValuePair<string, string> currPair in currObj.Members)
                    variables[arrayName + "[" + count + "]" + currPair.Key] = currPair.Value;
                count++;
            }
        }

    }

    public class WeakTypedObject : IComparable {
        public string SortKey;
        public Dictionary<string, string> Members;

        public string BaseName {
            get { return baseName; }
        } protected string baseName;

        public string BaseValue {
            get { return baseValue; }
        } protected string baseValue;

        
        public WeakTypedObject(string baseName, Dictionary<string, string> variables) {
            this.baseName = baseName;
            loadMembers(variables);
        }

        public string SortValue {
            get {
                if (Members != null && Members.ContainsKey("." + SortKey))
                    return Members["." + SortKey];
                else if (Members != null && Members.ContainsKey(SortKey))
                    return Members[SortKey];
                else return null;
            }
        }

        private void loadMembers(Dictionary<string, string> variables) {
            Members = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> currPair in variables) {
                if (currPair.Key.StartsWith(BaseName)) {
                    string memberName = currPair.Key.Replace(BaseName, "");
                    if (memberName.Length == 0) {
                        baseValue = currPair.Value;
                        continue;
                    }
                    else if (memberName[0] == '.') {
                        Members[memberName] = currPair.Value;
                    }
                    else if (memberName[0] == '[') {
                        Members[memberName] = currPair.Value;
                    }
                    else continue;


                }
            }
        }

        public int CompareTo(object obj) {
            if (SortValue == null)
                return 0;

            if (obj == null || obj.GetType() != typeof(WeakTypedObject))
                return 0;

            // grab the other object and assert it is valid
            WeakTypedObject other = (WeakTypedObject)obj;
            if (other.SortValue == null)
                return 0;
            
            // try assuming the values are numeric
            try {
                float thisFloat = float.Parse(SortValue);
                float otherFloat = float.Parse(other.SortValue);

                return thisFloat == otherFloat ? 0 : thisFloat < otherFloat ? -1 : 1;
            }
            catch (Exception e) {
                if (e.GetType() == typeof(ThreadAbortException))
                    throw e;
            }

            // otherwise resort to string based sorting
            return SortValue.CompareTo(other.SortValue);
        }
    }
}
