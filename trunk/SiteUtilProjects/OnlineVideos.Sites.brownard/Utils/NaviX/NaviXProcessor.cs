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

        Regex multiIfTest = null;
        Regex conditionExtract = null;
        Regex ifParse = null;

        public NaviXProcessor(NaviXMediaItem item)
        {
            string url = string.Format("{0}?url={1}", item.Processor, HttpUtility.UrlEncode(item.URL));
            string txt = getProcessor(url, item.Version.ToString());
            process(txt, item);
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

        void process(string procTxt, NaviXMediaItem mediaItem)
        {
            if (!procTxt.StartsWith("v2"))
            {
                //TODO handle v1
                return;
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
                    return;
                }
                //keep reference to current text
                instPrev = procTxt;
                vars["NIPL"] = procTxt;

                //split text into individual lines
                string[] lines = procTxt.Split("\r\n".ToCharArray());
                if (lines.Length < 1)
                {
                    logError("processor has no content");
                    return; //no content
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
                        logInfo("scrape {0}", scrape);

                        //check if we have a url
                        if (string.IsNullOrEmpty(vars["s_url"]))
                        {
                            logError("no scrape URL defined");
                            return;
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
                        logDebug("scrape headers");
                        foreach (string key in webData.Headers.Keys)
                        {
                            string hkey = "headers." + key;
                            logDebug("\t {0}: {1}", hkey, webData.Headers[key]);
                            vars[hkey] = webData.Headers[key];
                        }

                        //Copy response cookies
                        logDebug("scrape cookies");
                        foreach (string key in webData.Cookies.Keys)
                        {
                            string ckey = "cookies." + key;
                            logDebug("\t {0}: {1}", ckey, webData.Cookies[key]);
                            vars[ckey] = webData.Cookies[key];
                        }

                        //if we're set to read and we have a response and a regex pattern
                        if (vars["s_action"] == "read" && !string.IsNullOrEmpty(vars["regex"]) && !string.IsNullOrEmpty(vars["htmRaw"]))
                        {
                            //create regex
                            Regex reg;
                            try
                            {
                                reg = new Regex(vars["regex"]);
                            }
                            catch (Exception ex)
                            {
                                logError("error creating regex with pattern {0} - {1}", vars["regex"], ex.Message);
                                return;
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
                                logDebug("scrape {0}:", scrape);
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
                                logDebug("scrape {0}: no match", scrape);
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

                    //report command - reload processor with with specified key/values
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
                            return;
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
                                ifStack.Peek().IfEnd = true;
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
                                    return;
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

                            logDebug("{0} => {1}", subj, ifEval == true);
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
                                    nookiesSet(mediaItem.Processor, dpKey, val, vars["nookie_expires"]);
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
                                string errorStr;
                                if (arg.StartsWith("'"))
                                    errorStr = arg.Substring(1);
                                else
                                    errorStr = vars[arg];
                                logError(errorStr);
                                return;
                            }
                            else if (subj == "report_val")
                            {
                                m = lParse.Match(arg);
                                if (!m.Success)
                                {
                                    logError("syntax error: {0}", line);
                                    return;
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
                                    return;
                                }
                                string ke = m.Groups[1].Value;
                                string va = m.Groups[3].Value;
                                string oldTmp = vars[ke];
                                vars[ke] = vars[ke] + getValue(va, vars);
                                logDebug("concat: old={0}\n new={1}", oldTmp, vars[ke]);
                            }
                            else if (subj == "match")
                            {
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
                                { vars["nomatch"] = "1"; }
                                if (m != null && m.Success)
                                {
                                    logDebug("match {0}:", arg);
                                    for (int i = 1; i < m.Groups.Count; i++)
                                    {
                                        string val = m.Groups[i].Value;
                                        if (val == null)
                                            val = "";
                                        string key = "v" + i.ToString();
                                        logDebug("\n {0}={1}", key, val);
                                        vars[key] = val;
                                    }
                                }
                                else
                                {
                                    logDebug("match: no match\n regex: {0}\n search: {1}", vars["regex"], vars[arg]);
                                    vars["nomatch"] = "1";
                                }
                            }
                            else if (subj == "replace")
                            {
                                m = lParse.Match(arg);
                                if (!m.Success)
                                {
                                    logError("syntax error: {0}", line);
                                    return;
                                }
                                string ke = m.Groups[1].Value;
                                string va = m.Groups[3].Value;
                                if (va.StartsWith("'"))
                                    va = va.Substring(1);
                                else
                                    va = vars[va];
                                string oldTmp = vars[ke];
                                vars[ke] = Regex.Replace(vars[ke], vars["regex"], va);
                                logDebug("replace {0}:\n old={1}\n new={2}", ke, oldTmp, vars[ke]);
                            }
                            else if (subj == "unescape")
                            {
                                string oldTmp = vars[arg];
                                vars[arg] = HttpUtility.UrlDecode(vars[arg]);
                                logDebug("unescape:\n old={0}\n new={1}", oldTmp, vars[arg]);
                            }
                            else if (subj == "escape")
                            {
                                string oldTmp = vars[arg];
                                vars[arg] = HttpUtility.UrlEncode(vars[arg]);
                                logDebug("escape:\n old={0}\n new={1}", oldTmp, vars[arg]);
                            }
                            else if (subj == "debug")
                            {
                                string info;
                                if (vars[arg] != null)
                                    info = ":\n " + vars[arg];
                                else
                                    info = " - does not exist";
                                logDebug("debug: {0}{1}", arg, info);
                            }
                            else if (subj == "print")
                            {
                                string info;
                                if (arg.StartsWith("'"))
                                    info = arg.Substring(1);
                                else
                                    info = string.Format("{0}:\n {1}", arg, vars[arg]);
                                logInfo("info: {0}", info);
                            }
                            else if (subj == "countdown")
                            {
                                string secsStr;
                                if (arg.StartsWith("'"))
                                    secsStr = arg.Substring(1);
                                else
                                    secsStr = vars[arg];
                                int secs;
                                if (int.TryParse(secsStr, out secs))
                                {
                                    logDebug("countdown: {0} seconds *****NOT SUPPORTED*****", secs);
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
                                returnCode = 2;
                                Data = plUrl;
                                return;
                            }
                            else
                            {
                                logError("unrecognised method {0}", subj);
                                return;
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
            returnCode = 0;
            logInfo("complete: final url {0}", mediaItem.URL);
        }

        private void nookiesSet(string p, string dpKey, string val, string p_2)
        {
            
        }

        private bool? evaluateIfBlock(string arg, NaviXVars vars)
        {
            Log.Debug("evaluating if block ({0})", arg);
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
                Log.Debug("evaluating if statement\n '{0}' {1} '{2}'", vars[lKey], oper, rside);
                result = NaviXStringEvaluator.Eval(vars[lKey], rside, oper);
            }
            else
                result = !string.IsNullOrEmpty(vars[arg]);

            Log.Debug("result - {0}", result);
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
                Log.Warn("NaviX: Processor: error creating log text - {0} \n{1}", ex.Message, ex.StackTrace);
                return format;
            }
        }
    }
}
