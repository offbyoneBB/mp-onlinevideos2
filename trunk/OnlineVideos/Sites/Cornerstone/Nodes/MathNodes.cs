using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Threading;

namespace OnlineVideos.Sites.Cornerstone.Nodes
{

    public abstract class MathNode : ScraperNode {

        public enum ResultTypeEnum { INT, FLOAT }
        
        public string Value1 {
            get { return value1; }
        } protected string value1;

        public string Value2 {
            get { return value2; }
        } protected string value2;

        public ResultTypeEnum ResultType {
            get { return resultType; }
        } protected ResultTypeEnum resultType;


        public MathNode(XmlNode xmlNode, bool debugMode)
            : base(xmlNode, debugMode) {

            // Load attributes
            string resultTypeStr = null;
            foreach (XmlAttribute attr in xmlNode.Attributes) {
                switch (attr.Name) {
                    case "value1":
                        value1 = attr.Value;
                        break;
                    case "value2":
                        value2 = attr.Value;
                        break;
                    case "result_type":
                        resultTypeStr = attr.Value;
                        break;
                }
            }

            // Validate VALUE1 attribute
            if (value1 == null) {
                logger.Error("Missing VALUE1 attribute on: " + xmlNode.OuterXml);
                loadSuccess = false;
                return;
            }

            // Validate VALUE2 attribute
            if (value2 == null) {
                logger.Error("Missing VALUE2 attribute on: " + xmlNode.OuterXml);
                loadSuccess = false;
                return;
            }

            // Validate RESULT_TYPE attribute
            if (resultTypeStr != null && resultTypeStr.ToUpper().Equals("FLOAT"))
                resultType = ResultTypeEnum.FLOAT;
            else
                resultType = ResultTypeEnum.INT;

        }
    }
    
    [ScraperNode("add")]
    public class AddNode : MathNode {

        public AddNode(XmlNode xmlNode, bool debugMode)
            : base(xmlNode, debugMode) {


        }

        public override void Execute(Dictionary<string, string> variables) {
            if (DebugMode) logger.Debug("executing add: " + xmlNode.OuterXml);

            string parsedValue1 = parseString(variables, value1);
            string parsedValue2 = parseString(variables, value2);

            try {
                float val1 = float.Parse(parsedValue1);
                float val2 = float.Parse(parsedValue2);

                if (ResultType == ResultTypeEnum.INT)
                    setVariable(variables, parseString(variables, Name), ((int)val1 + val2).ToString());
                if (ResultType == ResultTypeEnum.FLOAT)
                    setVariable(variables, parseString(variables, Name), (val1 + val2).ToString());
            }
            catch (Exception e) {
                if (e.GetType() == typeof(ThreadAbortException))
                    throw e;

                logger.Error("Error parsing numbers: " + xmlNode.OuterXml);
            }
        }
    }

    [ScraperNode("subtract")]
    public class SubtractNode : MathNode {

        public SubtractNode(XmlNode xmlNode, bool debugMode)
            : base(xmlNode, debugMode) {
        }

        public override void Execute(Dictionary<string, string> variables) {
            if (DebugMode) logger.Debug("executing subtract: " + xmlNode.OuterXml);

            string parsedValue1 = parseString(variables, value1);
            string parsedValue2 = parseString(variables, value2);

            try {
                float val1 = float.Parse(parsedValue1);
                float val2 = float.Parse(parsedValue2);

                if (ResultType == ResultTypeEnum.INT)
                    setVariable(variables, parseString(variables, Name), ((int)val1 - val2).ToString());
                if (ResultType == ResultTypeEnum.FLOAT)
                    setVariable(variables, parseString(variables, Name), (val1 - val2).ToString());
            }
            catch (Exception e) {
                if (e.GetType() == typeof(ThreadAbortException))
                    throw e;

                logger.Error("Error parsing numbers: " + xmlNode.OuterXml);
            }
        }
    }

    [ScraperNode("multiply")]
    public class MultiplyNode : MathNode {

        public MultiplyNode(XmlNode xmlNode, bool debugMode)
            : base(xmlNode, debugMode) {
        }

        public override void Execute(Dictionary<string, string> variables) {
            if (DebugMode) logger.Debug("executing multiply: " + xmlNode.OuterXml);
            string parsedValue1 = parseString(variables, value1);
            string parsedValue2 = parseString(variables, value2);

            try {
                float val1 = float.Parse(parsedValue1);
                float val2 = float.Parse(parsedValue2);

                if (ResultType == ResultTypeEnum.INT)
                    setVariable(variables, parseString(variables, Name), ((int)val1 * val2).ToString());
                if (ResultType == ResultTypeEnum.FLOAT)
                    setVariable(variables, parseString(variables, Name), (val1 * val2).ToString());
            }
            catch (Exception e) {
                if (e.GetType() == typeof(ThreadAbortException))
                    throw e;

                logger.Error("Error parsing numbers: " + xmlNode.OuterXml);
            }
        }
    }

    [ScraperNode("divide")]
    public class DivideNode : MathNode {

        public DivideNode(XmlNode xmlNode, bool debugMode)
            : base(xmlNode, debugMode) {
        }

        public override void Execute(Dictionary<string, string> variables) {
            if (DebugMode) logger.Debug("executing divide: " + xmlNode.OuterXml);
            string parsedValue1 = parseString(variables, value1);
            string parsedValue2 = parseString(variables, value2);

            try {
                float val1 = float.Parse(parsedValue1);
                float val2 = float.Parse(parsedValue2);

                if (ResultType == ResultTypeEnum.INT)
                    setVariable(variables, parseString(variables, Name), ((int)(val1 / val2)).ToString());
                if (ResultType == ResultTypeEnum.FLOAT)
                    setVariable(variables, parseString(variables, Name), (val1 / val2).ToString());
            }
            catch (Exception e) {
                if (e.GetType() == typeof(ThreadAbortException))
                    throw e;

                logger.Error("Error parsing numbers: " + xmlNode.OuterXml);
            }
        }
    }



}
