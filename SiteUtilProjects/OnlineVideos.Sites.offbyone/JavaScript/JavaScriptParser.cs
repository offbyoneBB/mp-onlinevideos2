using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.JavaScript
{
    public class JavaScriptParser
    {
        private string javaScript;

        public JavaScriptParser(string javaScript)
        {
            this.javaScript = javaScript;
        }

        public FunctionData Parse()
        {
            string signatureMethodName = FindSignatureFunctionName(javaScript);
            string[] startFunctionString = FindFunctionBody(signatureMethodName, javaScript);
            string[] functionParameter = startFunctionString[1].Replace('\n', ' ').Split(',');
            string[] functionBody = startFunctionString[2].Replace('\n', ' ').Split(';');

            List<string> objectNames = GetObjectNames(functionBody, functionParameter[0]);
            List<string> objectBodies = GetObjectBodies(objectNames);

            objectBodies.Add(startFunctionString[0]);

            FunctionData returnData = new FunctionData
            {
                StartFunctionName = signatureMethodName,
                Bodies = objectBodies
            };

            return returnData;
        }

        private List<string> GetObjectBodies(List<string> objectNames)
        {
            List<string> objectBodies = new List<string>();

            foreach (var objectName in objectNames)
            {
                string objectBody = FindObjectBody(objectName, javaScript);
                objectBodies.Add(objectBody);
            }

            return objectBodies;
        }

        private List<string> GetObjectNames(string[] functionBody, string functionParameter)
        {
            List<string> objectNames = new List<string>();

            foreach (string line in functionBody)
            {
                if (CheckLineForSplitOrJoin(line, functionParameter))
                {
                    continue;
                }

                string pattern = @"(?<object_name>[\$a-zA-Z0-9]+)\.(?<function_name>[\$a-zA-Z0-9]+)\((?<parameter>[^)]+)\)";
                Match cipherMatch = Regex.Match(line, pattern);

                if (cipherMatch.Success && !objectNames.Contains(cipherMatch.Groups["object_name"].Value))
                {
                    objectNames.Add(cipherMatch.Groups["object_name"].Value);
                }
            }

            return objectNames;
        }

        private string FindObjectBody(string objectName, string jsContent)
        {
            string returnString = "";

            StringBuilder test = new StringBuilder();
            test.Append(@"var ");
            test.Append(objectName);
            test.Append(@"={.*?}};");
            string pattern = test.ToString();

            Match match = Regex.Match(jsContent, pattern, RegexOptions.Singleline);

            if (match.Success)
            {
                returnString = match.Groups[0].Value;
            }

            return returnString;
        }

        private bool CheckLineForSplitOrJoin(string line, string functionParameter)
        {
            string pattern = string.Format(@"{0}\s?=\s?{0}.split....", functionParameter);
            Match splitMatch = Regex.Match(line, pattern);

            pattern = string.Format(@"return\s+{0}.join....", functionParameter);
            Match returnMatch = Regex.Match(line, pattern);

            return splitMatch.Success || returnMatch.Success;
        }

        private string[] FindFunctionBody(string signatureMethodName, string jsContent)
        {
            string[] returnList = new string[3];

            if (signatureMethodName.StartsWith("$"))
            {
                signatureMethodName = @"\" + signatureMethodName;
            }

            string pattern = @"\s?" + signatureMethodName + @"=function\((?<parameter>[^)]+)\)\s?\{\s?(?<body>[^}]+)\s?\}";
            Match match = Regex.Match(jsContent, pattern);

            if (match.Success)
            {
                returnList[0] = match.Groups[0].Value;
                returnList[1] = match.Groups["parameter"].Value;
                returnList[2] = match.Groups["body"].Value;
            }

            return returnList;
        }

        string FindSignatureFunctionName(string jsContent)
        {
            string pattern = @"signature.,\s*(?<name>[$a-zA-Z]+)\([^)]*\)";
            Match match = Regex.Match(jsContent, pattern);

            if (match.Success)
            {
                return match.Groups["name"].Value;
            }

            return String.Empty;
        }
    }
}
