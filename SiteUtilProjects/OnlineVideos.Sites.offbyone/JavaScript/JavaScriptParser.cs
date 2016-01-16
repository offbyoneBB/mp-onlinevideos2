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

        public IList<FunctionData> Parse()
        {            
            IList<FunctionData> functions = new List<FunctionData>();

            string signatureMethodName = FindSignatureFunctionName(javaScript);
            List<string> function = FindFunctionBody(signatureMethodName, javaScript);

            string[] functionParameter = function[0].Replace('\n', ' ').Split(',');
            string[] functionBody = function[1].Replace('\n', ' ').Split(';');

            string[] patterns = {@"[a-zA-Z]+.slice\((?<a>\d+),[a-zA-Z]+\)",
                                 @"[a-zA-Z]+.splice\((?<a>\d+),[a-zA-Z]+\)", 
                                 @"[a-zA-Z].reverse\(\)", 
                                 @"var\s?[a-zA-Z]+=\s?[a-zA-Z]+\[0\]"};

            foreach (string line in functionBody)
            {
                string pattern = string.Format(@"{0}\s?=\s?{0}.split....", functionParameter[0]);
                Match splitMatch = Regex.Match(line, pattern);

                if (splitMatch.Success)
                {
                    //Do something
                    continue;
                }

                pattern = string.Format(@"return\s+{0}.join....", functionParameter[0]);                
                Match returnMatch = Regex.Match(line, pattern);

                if (returnMatch.Success)
                {
                    //Do something
                    continue;
                }

                pattern = @"(?<object_name>[\$a-zA-Z0-9]+)\.(?<function_name>[\$a-zA-Z0-9]+)\((?<parameter>[^)]+)\)";
                Match cipherMatch = Regex.Match(line, pattern);

                if (cipherMatch.Success)
                {
                    string objectName = cipherMatch.Groups["object_name"].Value;
                    string functionName = cipherMatch.Groups["function_name"].Value;
                    string[] parameters = cipherMatch.Groups["parameter"].Value.Split(',');

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        string param = parameters[i].Trim();
                        if (i == 0)
                        {
                            param = "%SIG%";
                        }

                        parameters[i] = param;
                    }

                    FunctionData currentFunction = GetObjectFunction(objectName, functionName, javaScript);

                    if (currentFunction != null)
                    {
                        currentFunction.Parameters = parameters;

                        foreach (FunctionTypes type in Enum.GetValues(typeof(FunctionTypes)))
                        {
                            if ((int)type > 2)
                            {
                                var tmpFunction = MatchFunction(patterns[(int)type - 3], type, currentFunction.Body, currentFunction);

                                if (tmpFunction != null)
                                {
                                    functions.Add(tmpFunction);
                                }
                            }
                        }
                    }                    
                }
            }

            return functions;
        }

        private FunctionData MatchFunction(string pattern, FunctionTypes functionType, string body, FunctionData function)
        {
            Match reverseMatch = Regex.Match(body, pattern);

            if (reverseMatch.Success)
            {
                function.Type = functionType;
                return function;
            }

            return null;
        }

        private FunctionData GetObjectFunction(string objectName, string functionName, string jsContent)
        {
            Dictionary<string, FunctionData> returnFunctions = new Dictionary<string, FunctionData>();

            string objectBody = FindObjectBody(objectName, jsContent);

            string[] splitter = new string[] { "}," };
            string[] objectBodys = objectBody.Split(splitter, StringSplitOptions.RemoveEmptyEntries);

            foreach (string body in objectBodys)
            {
                string tmpBody = body;
                if (!tmpBody.EndsWith("}"))
                {
                    tmpBody += "}";
                }

                tmpBody = tmpBody.Trim();

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(@"(?<name>[^:]*):function\((?<parameter>[^)]*)\)\{(?<body>[^}]+)\}");
                string pattern = stringBuilder.ToString();
                Match match = Regex.Match(tmpBody, pattern);

                if (match.Success)
                {
                    FunctionData functionData = new FunctionData
                    {
                        Name = match.Groups["name"].Value,
                        Body = match.Groups["body"].Value
                    };

                    returnFunctions.Add(functionData.Name, functionData);
                }
            }

            FunctionData returnFunction = null;
            returnFunctions.TryGetValue(functionName, out returnFunction);

            return returnFunction;
        }

        private string FindObjectBody(string objectName, string jsContent)
        {
            string returnString = "";

            StringBuilder test = new StringBuilder();
            test.Append(@"var ");
            test.Append(objectName);
            test.Append(@"={(?<object_body>.*?})};");
            string pattern = test.ToString();

            Match match = Regex.Match(jsContent, pattern, RegexOptions.Singleline);

            if (match.Success)
            {
                returnString = match.Groups["object_body"].Value;
            }

            return returnString;
        }

        private List<string> FindFunctionBody(string signatureMethodName, string jsContent)
        {
            List<string> returnList = new List<string>();

            string pattern = @"\s?" + signatureMethodName + @"=function\((?<parameter>[^)]+)\)\s?\{\s?(?<body>[^}]+)\s?\}";
            Match match = Regex.Match(jsContent, pattern);

            if (match.Success)
            {
                returnList.Add(match.Groups["parameter"].Value);
                returnList.Add(match.Groups["body"].Value);
            }

            return returnList;
        }

        string FindSignatureFunctionName(string jsContent)
        {
            string pattern = @"set..signature..(?<name>[$a-zA-Z]+)\([^)]\)";
            Match match = Regex.Match(jsContent, pattern);

            if (match.Success)
            {
                return match.Groups["name"].Value;
            }

            return String.Empty;
        }
    }
}
