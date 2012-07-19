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
        int returnCode = 1;
        public int ReturnCode { get { return returnCode; } }
        public string Data { get; protected set; }
        string procTxt = null;
        NaviXMediaItem mediaItem = null;

        Regex multiIfTest = null;
        Regex conditionExtract = null;
        Regex ifParse = null;

        public NaviXProcessor(NaviXMediaItem item)
        {
            string url = string.Format("{0}?url={1}", item.Processor, HttpUtility.UrlEncode(item.URL));
            procTxt = getProcessor(url, item.Version.ToString());
            mediaItem = item;
        }

        string getProcessor(string url, string itemVersion)
        {
            CookieCollection ccollection = new CookieCollection();
            ccollection.Add(new Cookie("version", itemVersion));
            ccollection.Add(new Cookie("platform", "xbmc"));
            CookieContainer cc = new CookieContainer();
            cc.Add(new Uri(url), ccollection);
            return OnlineVideos.Sites.SiteUtilBase.GetWebData(url, cc);
        }

        public bool Process()
        {
            if(mediaItem == null || string.IsNullOrEmpty(procTxt))
                return false;

            if (!procTxt.StartsWith("v2"))
            {
                //TODO handle v1
                return false;
            }
            //Remove version line
            procTxt = procTxt.Substring(2);
            string procArgs = "";
            string instPrev = "";

            //Regex
            Regex lParse = new Regex(@"^([^ =]+)([ =])(.+)$", RegexOptions.Compiled); //command and variable assignement match
            Regex dotVarParse = new Regex(@"^(nookies|s_headers)\.(.+)$", RegexOptions.Compiled); //special variables match (nookies or s_headers)
            multiIfTest = new Regex(@"^\(", RegexOptions.Compiled); //multi statement if block
            conditionExtract = new Regex(@"\(\s*([^\(\)\u0010\u0011]+)\s*\)", RegexOptions.Compiled); //individual if statement match
            ifParse = new Regex(@"^([^<>=!]+)\s*([!<>=]+)\s*(.+)$", RegexOptions.Compiled); //variable names and operator match for if statement

            //Special dictionary to store processor variables, accessing unassigned variables will just return an empty string
            NaviXVars vars = new NaviXVars();
            //Stores any assigned 's_headers' to use for the next scrape
            Dictionary<string, string> headers = new Dictionary<string, string>();
            logDebug("nookies: ");
            foreach(NaviXNookie nookie in NaviXNookie.GetNookies(mediaItem.Processor))
            {
                string key = "nookies." + nookie.Name;
                vars[key] = nookie.Value;
                logDebug("{0}: {1}", key, nookie.Value);
            }

            int phase = 0;
            bool phase1Complete = false;
            bool exitFlag = false;
            while (!exitFlag)
            {
                phase++;
                logInfo("phase {0}", phase);
                int scrape = 0;

                //Stores any key/values to use when the report command is specified
                Dictionary<string, string> rep = new Dictionary<string, string>();
                //Stack to keep track of nested if blocks
                Stack<NaviXIfBlock> ifStack = new Stack<NaviXIfBlock>();
                //reset default variables, leave user variables alone
                vars.Reset();

                //if processor args have been specified, reload processor using args
                if (!string.IsNullOrEmpty(procArgs))
                {
                    logInfo("phase {0} learn", phase);
                    procTxt = OnlineVideos.Sites.SiteUtilBase.GetWebData(mediaItem.Processor + "?" + procArgs);
                    procArgs = "";
                }
                else if (phase1Complete) //if we've been through all lines once, exit                    
                    exitFlag = true;
                else //else set s_url to media url
                    vars["s_url"] = mediaItem.URL;

                //reloaded processor is same as previous, endless loop
                if (procTxt == instPrev)
                {
                    logError("endless loop detected");
                    return false;
                }
                //keep reference to current text
                instPrev = procTxt;
                vars["NIPL"] = procTxt;

                //split text into individual lines
                string[] lines = procTxt.Split("\r\n".ToCharArray());
                if (lines.Length < 1)
                {
                    logError("processor has no content"); //no content
                    return false;
                }

                //loop through each line and process commands
                for (int x = 0; x < lines.Length; x++)
                {
                    //remove leading whitespace
                    string line = lines[x].TrimStart();
                    //checok if line empty or a comment
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
                        continue;

                    if (ifStack.Count > 0 && !line.StartsWith("if ") && !line.StartsWith("endif"))
                    {
                        //if we are waiting for endif, continue
                        if (ifStack.Peek().IfEnd && line != "endif")
                            continue;
                        //else if we are waiting for next else block, continue
                        if (ifStack.Peek().IfNext && !line.StartsWith("elseif") && line != "else" && line != "endif")
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
                            Referer = vars["s_referer"],
                            RequestCookies = vars["s_cookie"],
                            Method = vars["s_method"],
                            UserAgent = vars["s_agent"],
                            PostData = vars["s_postdata"],
                            RequestHeaders = headers
                        };
                        logDebug("\t url: {0}", vars["s_url"]);
                        logDebug("\t referer: {0}", vars["s_referer"]);
                        logDebug("\t cookies: {0}", vars["s_cookie"]);
                        logDebug("\t method: {0}", vars["s_method"]);
                        logDebug("\t useragent: {0}", vars["s_agent"]);
                        logDebug("\t post data: {0}", vars["s_postdata"]);

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
                        logDebug("Scrape {0} headers", scrape);
                        foreach (string key in webData.ResponseHeaders.Keys)
                        {
                            string hkey = "headers." + key;
                            logDebug("\t {0}: {1}", hkey, webData.ResponseHeaders[key]);
                            vars[hkey] = webData.ResponseHeaders[key];
                        }

                        //Copy response cookies
                        logDebug("Scrape {0} cookies", scrape);
                        foreach (string key in webData.ResponseCookies.Keys)
                        {
                            string ckey = "cookies." + key;
                            logDebug("\t {0}: {1}", ckey, webData.ResponseCookies[key]);
                            vars[ckey] = webData.ResponseCookies[key];
                        }

                        //if we're set to read and we have a response and a regex pattern
                        if (vars["s_action"] == "read" && !string.IsNullOrEmpty(vars["regex"]) && !string.IsNullOrEmpty(vars["htmRaw"]))
                        {
                            //create regex
                            logDebug("Scrape {0} regex: {1}", scrape, vars["regex"]);
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
                                logDebug("Scrape {0} regex:", scrape);
                                for (int i = 1; i < m.Groups.Count; i++)
                                {
                                    //create vars for each match group, v1,v2,v3 etc
                                    string val = m.Groups[i].Value;
                                    if (val == null)
                                        val = "";
                                    string key = "v" + i.ToString();
                                    logDebug("\t {0}={1}", key, val);
                                    vars[key] = val;
                                    rep[key] = val;
                                }
                            }
                            else //no match
                            {
                                logDebug("Scrape {0} regex: no match", scrape);
                                vars["nomatch"] = "1";
                                rep["nomatch"] = "1";
                            }
                        }
                        //reset scrape vars for next scrape
                        vars.Reset("scrape");
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
                            bool? ifEval = null;
                            //if start of elseif and previous if was satisfied, skip to next endif
                            if (ifStack.Count > 0 && subj == "elseif" && ifStack.Peek().IfSatisified)
                            {
                                ifStack.Peek().IfEnd = true;
                                continue;
                            }
                            else
                            {
                                //start of new if block
                                if (subj == "if")
                                {
                                    NaviXIfBlock ifBlock = new NaviXIfBlock();
                                    //if we are nested in an if block that we are set to skip, skip to next endif
                                    if (ifStack.Count > 0 && (!ifStack.Peek().IfSatisified || ifStack.Peek().IfEnd))
                                        ifBlock.IfEnd = true;
                                    //add to stack
                                    ifStack.Push(ifBlock);
                                }
                                //attempt to evaluate if block
                                ifEval = evaluateIfBlock(arg, vars);
                                if (ifEval == null)
                                {
                                    logError("error evaluating if statement: {0}", line);
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
                            continue;
                        }

                        //variable assignment
                        if (m.Groups[2].Value == "=")
                        {
                            string aReport;
                            string tReport = "";
                            string tSubj;
                            string val = getValue(arg, vars);
                            if (arg.StartsWith("'"))
                                aReport = "string literal";
                            else                            
                                aReport = arg;

                            m = dotVarParse.Match(subj);
                            if (m.Success)
                            {
                                string dpType = m.Groups[1].Value;
                                string dpKey = m.Groups[2].Value;
                                tSubj = dpKey;
                                if (dpType == "nookies")
                                {
                                    tReport = "nookie";
                                    //TODO
                                    NaviXNookie.AddNookie(mediaItem.Processor, dpKey, val, vars["nookie_expires"]);
                                    vars[subj] = val;
                                }
                                else if (dpType == "s_headers")
                                {
                                    tReport = "scrape header";
                                    headers[dpKey] = val;
                                }
                            }
                            else
                            {
                                tReport = "variable";
                                tSubj = subj;
                                vars[subj] = val;
                            }
                            logDebug("{0}: {1} set to {2}\n {3}", tReport, tSubj, aReport, val);
                        }
                        else
                        {
                            if (subj == "verbose")
                            { }
                            else if (subj == "error")
                            {
                                string errorStr = getValue(arg, vars);
                                logError(errorStr);
                                return false;
                            }
                            else if (subj == "report_val")
                            {
                                m = lParse.Match(arg);
                                if (!m.Success)
                                {
                                    logError("syntax error: {0}", line);
                                    return false;
                                }
                                string ke = m.Groups[1].Value;
                                string va = m.Groups[3].Value;
                                if (va.StartsWith("'"))
                                {
                                    rep[ke] = va.Substring(1);
                                    logDebug("report value: {0} set to string literal\n {1]", ke, va.Substring(1));
                                }
                                else
                                {
                                    rep[ke] = vars[va];
                                    logDebug("report value: {0} set to {1}\n {2]", ke, va, vars[va]);
                                }
                            }
                            else if (subj == "concat")
                            {
                                m = lParse.Match(arg);
                                if (!m.Success)
                                {
                                    logError("syntax error: {0}", line);
                                    return false;
                                }
                                string ke = m.Groups[1].Value;
                                string va = m.Groups[3].Value;
                                string oldTmp = vars[ke];
                                string concatVal = getValue(va, vars);
                                vars[ke] = oldTmp + concatVal;
                                logDebug("concat: {0} + {1} = {2}", oldTmp, concatVal, vars[ke]);
                            }
                            else if (subj == "match")
                            {
                                logDebug("regex:\r\n\t pattern: " + vars["regex"] + "\r\n\t source: " + vars[arg]);
                                vars["nomatch"] = "";
                                rep["nomatch"] = "";
                                for (int i = 1; i < 12; i++)
                                {
                                    string ke = "v" + i.ToString();
                                    vars[ke] = "";
                                    rep[ke] = "";
                                }
                                m = null;
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
                                        logDebug("regex: {0}={1}", key, val);
                                        vars[key] = val;
                                    }
                                }
                                else
                                {
                                    logDebug("regex: no match");
                                    vars["nomatch"] = "1";
                                }
                            }
                            else if (subj == "replace")
                            {
                                m = lParse.Match(arg);
                                if (!m.Success)
                                {
                                    logError("syntax error: {0}", line);
                                    return false;
                                }
                                string ke = m.Groups[1].Value;
                                string va = getValue(m.Groups[3].Value, vars);
                                string oldTmp = vars[ke];
                                logDebug("replace:\r\n\t source: " + oldTmp + "\r\n\t pattern: " + vars["regex"] + "\r\n\t replacement: " + va);                                
                                try
                                {
                                    string result = new Regex(vars["regex"]).Replace(oldTmp, va);
                                    vars[ke] = result;
                                    logDebug("replace: result: {0}", result);
                                }
                                catch
                                {
                                    logError("replace: error creating regex: {0}", vars["regex"]);
                                }
                            }
                            else if (subj == "unescape")
                            {
                                string oldTmp = vars[arg];
                                vars[arg] = HttpUtility.UrlDecode(vars[arg]);
                                logDebug("unescape: {0}:\r\n\t new: {1}", oldTmp, vars[arg]);
                            }
                            else if (subj == "escape")
                            {
                                string oldTmp = vars[arg];
                                vars[arg] = HttpUtility.UrlEncode(vars[arg]);
                                logDebug("escape: {0}:\r\n\t new: {1}", oldTmp, vars[arg]);
                            }
                            else if (subj == "debug")
                            {
                                string info;
                                if (vars[arg] != null)
                                    info = ":  " + vars[arg];
                                else
                                    info = ": does not exist";
                                logDebug("debug: {0}{1}", arg, info);
                            }
                            else if (subj == "print")
                            {
                                string info;
                                if (arg.StartsWith("'"))
                                    info = arg.Substring(1);
                                else
                                    info = string.Format("{0}: {1}", arg, vars[arg]);
                                logInfo("info: {0}", info);
                            }
                            else if (subj == "countdown")
                            {
                                string secsStr = getValue(arg, vars);
                                int secs;
                                if (int.TryParse(secsStr, out secs))
                                {
                                    logError("countdown: {0} seconds *****NOT SUPPORTED - Ignoring*****", secs);

                                    //System.Threading.Thread.Sleep(secs * 1000);
                                }
                                else
                                {
                                    logError("countdown: error parsing seconds {0}", secsStr);
                                }
                            }
                            else if (subj == "show_playlist")
                            {
                                string plUrl;
                                if (arg.StartsWith("'"))
                                    plUrl = arg.Substring(1);
                                else
                                    plUrl = vars[arg];
                                logInfo("redirecting to playlist {0} *****NOT SUPPORTED*****", plUrl);
                                Data = plUrl;
                                return false;
                            }
                            else
                            {
                                logError("unrecognised method {0}", subj);
                                return false;
                            }
                        }
                    }
                }                
            }

            mediaItem.URL = vars["url"];
            if (!string.IsNullOrEmpty(vars["referer"]))
                mediaItem.Referer = vars["referer"];
            if (!string.IsNullOrEmpty(vars["agent"]))
            {
                //vars["url"] = string.Format("{0}?|User-Agent={1}", vars["url"], vars["agent"]);
                mediaItem.Agent = vars["agent"];
            }
            if (!string.IsNullOrEmpty(vars["playpath"]) || !string.IsNullOrEmpty(vars["swfplayer"]))
            {
                mediaItem.URL += string.Format(" tcUrl={0}", vars["url"]);
                if (!string.IsNullOrEmpty(vars["app"]))
                    mediaItem.URL += string.Format(" app={0}", vars["app"]);
                if (!string.IsNullOrEmpty(vars["playpath"]))
                    mediaItem.URL += string.Format(" playpath={0}", vars["playpath"]);
                if (!string.IsNullOrEmpty(vars["swfplayer"]))
                    mediaItem.URL += string.Format(" swfUrl={0}", vars["swfplayer"]);
                if (!string.IsNullOrEmpty(vars["pageurl"]))
                    mediaItem.URL += string.Format(" pageUrl={0}", vars["pageurl"]);
                if (!string.IsNullOrEmpty(vars["swfVfy"]))
                    mediaItem.URL += string.Format(" swfVfy={0}", vars["swfVfy"]);
            }
            else
            {
                mediaItem.SWFPlayer = vars["swfplayer"];
                mediaItem.PlayPath = vars["playpath"];
                mediaItem.PageURL = vars["pageurl"];
            }
            mediaItem.Processor = "";
            Data = mediaItem.URL;
            logInfo("complete: final url {0}", mediaItem.URL);
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
