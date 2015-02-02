using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using System.Text.RegularExpressions;
using OnlineVideos.Sites.Utils.NaviX.Processor;

namespace OnlineVideos.Sites.Utils.NaviX
{
    class NaviXProcessor
    {
        public NaviXProcessor(string processorUrl, string itemUrl, double version, string nxId = null, string platform = "xbmc")
        {
            this.processorUrl = processorUrl;
            this.itemUrl = itemUrl;
            this.version = version;
            this.nxId = nxId;
            this.platform = platform;
        }

        public string Data { get; protected set; }
        public string LastError { get; protected set; }

        string processorUrl;
        string itemUrl;
        double version;
        string platform;
        string nxId;
        string processorText = null;

        //Special dictionary to store processor variables, accessing unassigned variables will just return an empty string
        NaviXVars vars = null;
        //Stores any assigned 's_headers' to use for the next scrape
        Dictionary<string, string> headers = null;
        //Stores any key/values to use when the report command is specified
        Dictionary<string, string> rep = null;
        //Stack to keep track of nested if blocks
        Stack<NaviXIfBlock> ifStack = null;

        //Regex
        Regex lParse = new Regex(@"^([^ =]+)([ =])(.+)$", RegexOptions.Compiled); //command and variable assignement match
        Regex dotVarParse = new Regex(@"^(nookies|s_headers)\.(.+)$", RegexOptions.Compiled); //special variables match (nookies or s_headers)
        Regex multiIfTest = new Regex(@"^\(", RegexOptions.Compiled); //multi statement if block
        Regex conditionExtract = new Regex(@"\(\s*([^\(\)\u0010\u0011]+)\s*\)", RegexOptions.Compiled); //individual if statement match
        Regex ifParse = new Regex(@"^([^<>=!]+)\s*([!<>=]+)\s*(.+)$", RegexOptions.Compiled); //variable names and operator match for if statement

        string getProcessorText()
        {
            if (string.IsNullOrEmpty(processorUrl) || itemUrl == null)
                return null;

            string url = string.Format("{0}?url={1}", processorUrl, HttpUtility.UrlEncode(itemUrl));
            CookieCollection ccollection = new CookieCollection();
            ccollection.Add(new Cookie("version", version.ToString()));
            ccollection.Add(new Cookie("platform", platform));
            if (processorUrl.StartsWith("http://www.navixtreme.com") && !string.IsNullOrEmpty(nxId))
                ccollection.Add(new Cookie("nxid", nxId));

            CookieContainer cc = new CookieContainer();
            cc.Add(new Uri(url), ccollection);
            return WebCache.Instance.GetWebData(url, cookies: cc);
        }

        public bool Process()
        {
            LastError = null;
            processorText = getProcessorText();
            if(string.IsNullOrEmpty(processorText))
                return false;

            //string cacheKey = OnlineVideos.Utils.EncryptLine(
            //    string.Format("{0}{1}{2}{3}",
            //        processorUrl,
            //        itemUrl,
            //        version,
            //        platform)
            //    );

            //string cacheResult = NaviXProcessorCache.Instance[cacheKey];
            //if (cacheResult != null)
            //{
            //    Data = cacheResult;
            //    logInfo("complete (from cache): final url {0}", cacheResult);
            //    return true;
            //}

            if (!processorText.StartsWith("v2"))
            {
                //TODO handle v1
                return false;
            }
            //Remove version line
            processorText = processorText.Substring(2);
            string procArgs = "";
            string instPrev = "";

            vars = new NaviXVars();
            headers = new Dictionary<string, string>();
            rep = new Dictionary<string, string>();
            ifStack = new Stack<NaviXIfBlock>();

            logDebug("nookies: ");
            foreach(NaviXNookie nookie in NaviXNookie.GetNookies(processorUrl))
            {
                string key = "nookies." + nookie.Name;
                vars[key] = nookie.Value;
                logDebug("{0}: {1}", key, nookie.Value);
            }

            int phase = 0;
            bool exitFlag = false;
            while (!exitFlag)
            {
                phase++;
                logInfo("phase {0}", phase);
                int scrape = 0;

                //reset default variables, leave user variables alone
                vars.Reset();
                rep.Clear();
                ifStack.Clear();

                //if processor args have been specified, reload processor using args
                if (!string.IsNullOrEmpty(procArgs))
                {
                    logInfo("phase {0} learn", phase);
                    processorText = WebCache.Instance.GetWebData(processorUrl + "?" + procArgs);
                    procArgs = "";
                }
                else //else set s_url to media url
                    vars["s_url"] = itemUrl;

                //reloaded processor is same as previous, endless loop
                if (processorText == instPrev)
                {
                    logError("endless loop detected");
                    LastError = "endless loop detected";
                    return false;
                }

                //keep reference to current text
                instPrev = processorText;
                vars["NIPL"] = processorText;

                //split text into individual lines
                string[] lines = processorText.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length < 1)
                {
                    logError("processor has no content"); //no content
                    LastError = "processor has no content";
                    return false;
                }

                //loop through each line and process commands
                for (int x = 0; x < lines.Length; x++)
                {
                    //remove leading whitespace
                    string line = lines[x].TrimStart();
                    //check if line empty or a comment
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
                        continue;

                    if (ifStack.Count > 0 && line != "endif" && !line.StartsWith("if "))
                    {
                        //if we are waiting for endif, continue
                        if (ifStack.Peek().IfEnd)
                            continue;
                        //else if we are waiting for next else block, continue
                        if (ifStack.Peek().IfNext && !line.StartsWith("elseif") && line != "else")
                            continue;
                    }

                    //start of else block
                    if (line == "else" && ifStack.Count > 0)
                    {
                        //if last if has been satisfied continue to next endif
                        if (ifStack.Peek().IfSatisified)
                            ifStack.Peek().IfEnd = true;
                        else //else process block contents
                            ifStack.Peek().IfNext = false;
                        continue;
                    }
                    //end of if/else blocks
                    else if (line == "endif" && ifStack.Count > 0)
                    {
                        //remove block from stack
                        ifStack.Pop();
                        continue;
                    }

                    //retrieve web data using s_url, s_cookies and any s_headers
                    //store the response and response headers and cookies
                    if (line == "scrape")
                    {
                        scrape++;
                        logDebug("Scrape {0}:", scrape);
                        if (!doScrape())
                            return false;
                    }

                    //play command, final url should be determined - stop processing and rerturn new url
                    else if (line == "play")
                    {
                        logDebug("play");
                        exitFlag = true;
                        break;
                    }

                    //report command - reload processor with specified key/values
                    else if (line == "report")
                    {
                        rep["phase"] = phase.ToString();
                        procArgs = "";
                        bool firstKey = true;
                        logDebug("report:");
                        //for each key/value
                        foreach (KeyValuePair<string, string> keyVal in rep)
                        {
                            logDebug("\t {0}: {1}", keyVal.Key, keyVal.Value);
                            string and;
                            if (!firstKey)
                                and = "&";
                            else
                            {
                                firstKey = false;
                                and = "";
                            }
                            //add arg string to proccessor args
                            if (!string.IsNullOrEmpty(keyVal.Value))
                                procArgs += string.Format("{0}{1}={2}", and, HttpUtility.UrlEncode(keyVal.Key), HttpUtility.UrlEncode(keyVal.Value));
                        }
                        break;
                    }

                    //Parse line for commmand
                    else 
                    {
                        //check if line is in recognised format
                        Match m = lParse.Match(line);
                        if (!m.Success)
                        {
                            logError("syntax error: {0}", line);
                            return false;
                        }
                        //command or variable being assigned
                        string subj = m.Groups[1].Value;
                        //args or value to assign
                        string arg = m.Groups[3].Value;

                        //start of if/elseif block
                        if (subj == "if" || subj == "elseif")
                        {
                            if (!handleIfBlock(subj, arg))
                            {
                                logError("error evaluating if statement: {0}", line);
                                return false;
                            }
                        }

                        //variable assignment
                        else if (m.Groups[2].Value == "=")
                        {
                            assignVariable(subj, arg);
                        }
                        else if (!handleCommand(line, subj, arg))
                        {
                            return false;
                        }
                    }
                }                
            }

            string url = vars["url"];            
            if (!string.IsNullOrEmpty(vars["playpath"]) || !string.IsNullOrEmpty(vars["swfplayer"]))
            {
                url += string.Format(" tcUrl={0}", vars["url"]);
                if (!string.IsNullOrEmpty(vars["app"]))
                    url += string.Format(" app={0}", vars["app"]);
                if (!string.IsNullOrEmpty(vars["playpath"]))
                    url += string.Format(" playpath={0}", vars["playpath"]);
                if (!string.IsNullOrEmpty(vars["swfplayer"]))
                    url += string.Format(" swfUrl={0}", vars["swfplayer"]);
                if (!string.IsNullOrEmpty(vars["pageurl"]))
                    url += string.Format(" pageUrl={0}", vars["pageurl"]);
                if (!string.IsNullOrEmpty(vars["swfVfy"]))
                    url += string.Format(" swfVfy={0}", vars["swfVfy"]);
            }
            
            Data = url;
            //NaviXProcessorCache.Instance[cacheKey] = url;
            logInfo("complete: final url {0}", url);
            return true;
        }

        bool handleIfBlock(string subj, string arg)
        {
            bool? ifEval = null;
            //if start of elseif and previous if was satisfied, skip to next endif
            if (ifStack.Count > 0 && subj == "elseif" && ifStack.Peek().IfSatisified)
            {
                ifStack.Peek().IfEnd = true;
                return true;
            }
            else
            {
                //start of new if block
                if (subj == "if")
                {
                    NaviXIfBlock ifBlock = new NaviXIfBlock();
                    NaviXIfBlock previousBlock = ifStack.Count > 0 ? ifStack.Peek() : null;
                    //add to stack
                    ifStack.Push(ifBlock);
                    //if we are nested in an if block that we are set to skip, skip to next endif
                    if (previousBlock != null && (!previousBlock.IfSatisified || previousBlock.IfEnd))
                    {
                        ifBlock.IfEnd = true;
                        return true;
                    }
                }
                //attempt to evaluate if block
                ifEval = evaluateIfBlock(arg, vars);
                if (ifEval == null)
                {
                    return false;
                }
            }

            //if is true
            if (ifEval == true)
            {
                //process block
                ifStack.Peek().IfSatisified = true;
                ifStack.Peek().IfNext = false;
            }
            //if is false, wait for next else/endif
            else
                ifStack.Peek().IfNext = true;

            logDebug("{0} ({1}) => {2}", subj, arg, ifEval == true);
            return true;
        }

        bool doScrape()
        {
            logDebug("\t scrape action: {0}", vars["s_action"]);
            //check if we have a url
            if (string.IsNullOrEmpty(vars["s_url"]))
            {
                logError("no scrape URL defined");
                return false;
            }
            //setup web request
            NaviXWebRequest webData = new NaviXWebRequest(vars["s_url"])
            {
                Action = vars["s_action"],
                Referer = vars["s_referer"],
                RequestCookies = vars["s_cookie"],
                Method = vars["s_method"],
                UserAgent = vars["s_agent"],
                PostData = vars["s_postdata"],
                RequestHeaders = headers
            };
            //retrieve request
            webData.GetWebData();
            //store response text
            vars["htmRaw"] = webData.Content;
            //get final url of request
            vars["geturl"] = webData.GetURL;
            //if scrape action is get url, set match as final url
            if (vars["s_action"] == "geturl")
                vars["v1"] = webData.GetURL;

            //Copy response headers
            logDebug("Response headers");
            foreach (string key in webData.ResponseHeaders.Keys)
            {
                string hKey = "headers." + key;
                string hVal = webData.ResponseHeaders[key];
                vars[hKey] = hVal;
                logDebug("\t {0}: {1}", hKey, hVal);
            }

            //Copy response cookies
            logDebug("Response cookies");
            foreach (string key in webData.ResponseCookies.Keys)
            {
                string cKey = "cookies." + key;
                string cVal = webData.ResponseCookies[key];
                vars[cKey] = cVal;
                logDebug("\t {0}: {1}", cKey, cVal);
            }

            //if we're set to read and we have a response and a regex pattern
            if (vars["s_action"] == "read" && !string.IsNullOrEmpty(vars["regex"]) && !string.IsNullOrEmpty(vars["htmRaw"]))
            {
                //create regex
                logDebug("Scrape regex: {0}", vars["regex"]);
                Regex reg;
                try
                {
                    reg = new Regex(vars["regex"]);
                }
                catch (Exception ex)
                {
                    logError("error creating regex with pattern " + vars["regex"] + " - " + ex.Message);
                    return false;
                }
                //create regex vars
                vars["nomatch"] = "";
                rep["nomatch"] = "";
                for (int i = 1; i < 12; i++)
                {
                    string ke = "v" + i.ToString();
                    vars[ke] = "";
                    rep[ke] = "";
                }
                //match against scrape response
                Match m = reg.Match(vars["htmRaw"]);
                if (m.Success)
                {
                    logDebug("Scrape matches:");
                    for (int i = 1; i < m.Groups.Count; i++)
                    {
                        //create vars for each match group, v1,v2,v3 etc
                        string val = m.Groups[i].Value;
                        if (val == null)
                            val = "";
                        string key = "v" + i.ToString();
                        vars[key] = val;
                        rep[key] = val;
                        logDebug("\t {0}={1}", key, val.Replace("\r\n", " "));
                    }
                }
                else //no match
                {
                    logDebug("Scrape regex: no match");
                    vars["nomatch"] = "1";
                    rep["nomatch"] = "1";
                }
            }
            //reset scrape vars for next scrape
            vars.Reset(true);
            return true;
        }

        void assignVariable(string subj, string arg)
        {
            string argType = arg.StartsWith("'") ? "string literal" : arg;
            string val = getValue(arg, vars);

            Match m = dotVarParse.Match(subj);
            if (m.Success)
            {
                string subType = m.Groups[1].Value;
                string subKey = m.Groups[2].Value;
                if (subType == "nookies")
                {
                    NaviXNookie.AddNookie(processorUrl, subKey, val, vars["nookie_expires"]);
                    vars[subj] = val;
                    logDebug("Nookie {0} set to {1} - {2}", subKey, argType, val);
                }
                else if (subType == "s_headers")
                {
                    headers[subKey] = val;
                    logDebug("Header {0} set to {1} - {2}", subKey, argType, val);
                }
            }
            else
            {
                vars[subj] = val;
                logDebug("Variable {0} set to {1} - {2}", subj, argType, val);
            }
        }

        bool handleCommand(string line, string subj, string arg)
        {
            bool result = false;
            switch (subj)
            {
                case "verbose":
                    result = true;
                    break;
                case "error":
                    string errorStr = getValue(arg, vars);
                    logError(errorStr);
                    LastError = errorStr;
                    return false;
                case "report_val":
                    result = reportValue(arg);
                    break;
                case "concat":
                    result = concatValue(arg);
                    break;
                case "match":
                    result = matchValue(arg);
                    break;
                case "replace":
                    result = replaceValue(arg);
                    break;
                case "unescape":
                    result = unescape(arg);
                    break;
                case "escape":
                    result = escape(arg);
                    break;
                case "debug":
                    result = debugValue(arg);
                    break;
                case "print":
                    result = printValue(arg);
                    break;
                case "countdown":
                    result = doCountdown(arg);
                    break;
                case "show_playlist":
                    string plUrl = getValue(arg, vars);
                    logInfo("redirecting to playlist {0} *****NOT SUPPORTED*****", plUrl);
                    Data = plUrl;
                    return false;
                default:
                    logError("unrecognised method {0}", subj);
                    return false;
            }

            if (!result)
                logError("syntax error: {0}", line);
            return result;
        }

        bool reportValue(string arg)
        {
            Match m = lParse.Match(arg);
            if (!m.Success)
                return false;
            
            string key = m.Groups[1].Value;
            string val = m.Groups[3].Value;
            if (val.StartsWith("'"))
            {
                rep[key] = val.Substring(1);
                logDebug("report value: {0} set to string literal\n {1]", key, val.Substring(1));
            }
            else
            {
                rep[key] = vars[val];
                logDebug("report value: {0} set to {1}\n {2]", key, val, vars[val]);
            }
            return true;
        }

        bool concatValue(string arg)
        {
            Match m = lParse.Match(arg);
            if (!m.Success)
                return false;
            
            string key = m.Groups[1].Value;
            string val = m.Groups[3].Value;
            string oldVal = vars[key];
            string concatVal = getValue(val, vars);
            vars[key] = oldVal + concatVal;
            logDebug("concat: {0} + {1} = {2}", oldVal, concatVal, vars[key]);
            return true;
        }

        bool matchValue(string arg)
        {
            logDebug("regex:\r\n\t pattern: " + vars["regex"] + "\r\n\t source: " + vars[arg].Replace("\r\n", " "));
            vars["nomatch"] = "";
            rep["nomatch"] = "";
            for (int i = 1; i < 12; i++)
            {
                string key = "v" + i.ToString();
                vars[key] = "";
                rep[key] = "";
            }
            Match m = null;
            try
            {
                Regex reg = new Regex(vars["regex"]);
                m = reg.Match(vars[arg]);
            }
            catch
            {
                logError("error creating regex: " + vars["regex"]);
                vars["nomatch"] = "1";
            }

            if (m != null && m.Success)
            {
                for (int i = 1; i < m.Groups.Count; i++)
                {
                    string val = m.Groups[i].Value;
                    if (val == null)
                        val = "";
                    string key = "v" + i.ToString();
                    logDebug("regex: {0}={1}", key, val.Replace("\r\n", " "));
                    vars[key] = val;
                }
            }
            else
            {
                logDebug("regex: no match");
                vars["nomatch"] = "1";
            }
            return true;
        }

        bool replaceValue(string arg)
        {
            Match m = lParse.Match(arg);
            if (!m.Success)
                return false;
            
            string key = m.Groups[1].Value;
            string val = getValue(m.Groups[3].Value, vars);
            string oldVal = vars[key];
            logDebug("replace:\r\n\t source: " + oldVal + "\r\n\t pattern: " + vars["regex"] + "\r\n\t replacement: " + val);
            try
            {
                string result = new Regex(vars["regex"]).Replace(oldVal, val);
                vars[key] = result;
                logDebug("replace: result: {0}", result);
            }
            catch
            {
                logError("replace: error creating regex: {0}", vars["regex"]);
            }
            return true;
        }

        bool escape(string arg)
        {
            string oldVal = vars[arg];
            vars[arg] = HttpUtility.UrlEncode(oldVal);
            logDebug("escape: {0}:\r\n\t new: {1}", oldVal, vars[arg]);
            return true;
        }

        bool unescape(string arg)
        {
            string oldVal = vars[arg];
            vars[arg] = HttpUtility.UrlDecode(oldVal);
            logDebug("unescape: {0}:\r\n\t new: {1}", oldVal, vars[arg]);
            return true;
        }

        bool debugValue(string arg)
        {
            string info;
            if (vars[arg] != null)
                info = ":  " + vars[arg];
            else
                info = ": does not exist";
            logDebug("debug: {0}{1}", arg, info);
            return true;
        }

        bool printValue(string arg)
        {
            string info;
            if (arg.StartsWith("'"))
                info = arg.Substring(1);
            else
                info = string.Format("{0}: {1}", arg, vars[arg].Replace("\n", " "));
            logInfo("info: {0}", info);
            return true;
        }

        bool doCountdown(string arg)
        {
            string secsStr = getValue(arg, vars);
            logError("countdown: {0} seconds *****NOT SUPPORTED - Ignoring*****", secsStr);

            //int secs;
            //if (int.TryParse(secsStr, out secs))
            //{
            //    System.Threading.Thread.Sleep(secs * 1000);
            //}
            //else
            //{
            //    logError("countdown: error parsing seconds {0}", secsStr);
            //}
            return true;
        }

        private bool? evaluateIfBlock(string arg, NaviXVars vars)
        {
            Match m = multiIfTest.Match(arg);
            if (!m.Success)
                return conditionEval(arg, vars);

            bool mFlag = true;
            while (mFlag)
            {
                m = conditionExtract.Match(arg);
                if (m.Success)
                {
                    string cond = m.Groups[1].Value;
                    bool? boolObj = conditionEval(cond, vars);
                    if (boolObj == null)
                    {
                        logError("error evaluating condition {0}", cond);
                        return null;
                    }
                    if (boolObj == true)
                        arg = arg.Replace(cond, "\u0010");
                    else
                        arg = arg.Replace(cond, "\u0011");
                }
                else
                    mFlag = false;
            }
            arg = arg.Replace("\u0010", "True");
            arg = arg.Replace("\u0011", "False");
            bool result;
            if (bool.TryParse(arg, out result))
                return result;
            else
            {
                logError("error evaluating result {0}", arg);
                return null;
            }
        }

        private bool? conditionEval(string arg, NaviXVars vars)
        {
            bool? result = null;
            Match m = ifParse.Match(arg);
            if (m.Success)
            {
                string lKey = m.Groups[1].Value;
                string oper = m.Groups[2].Value;
                string rraw = m.Groups[3].Value;
                if (oper == "=")
                    oper = "==";

                string rside = getValue(rraw, vars);
                result = NaviXStringEvaluator.Eval(vars[lKey], rside, oper);
            }
            else
                result = !string.IsNullOrEmpty(vars[arg]);

            return result;
        }

        string getValue(string arg, NaviXVars vars)
        {
            if (arg.StartsWith("'"))
                return arg.Substring(1);
            else
                return vars[arg];
        }

        void logInfo(string format, params object[] args)
        {
            Log.Info("NaviX: Processor: " + getLogTxt(format, args));
        }
        void logDebug(string format, params object[] args)
        {
            Log.Debug("NaviX: Processor: " + getLogTxt(format, args));
        }
        void logError(string format, params object[] args)
        {
            Log.Warn("NaviX: Processor Error: " + getLogTxt(format, args));
        }
        string getLogTxt(string format, params object[] args)
        {
            if (args.Length < 1)
                return format;
            try
            {
                return string.Format(format, args);
            }
            catch (Exception ex)
            {
                //Log.Warn("NaviX: Processor: error creating log text - {0} \n{1}", ex.Message, ex.StackTrace);
                return format;
            }
        }
    }
}
