using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Utils
{
    /// <summary>
    /// Simple TV Guide grabber using data from xmltv.radiotimes.com
    /// </summary>
    class TVGuideGrabber
    {
        static System.Text.RegularExpressions.Regex idReg = new System.Text.RegularExpressions.Regex(@"[?&]guideid=(\d+)", System.Text.RegularExpressions.RegexOptions.Compiled);

        public bool GetNowNextForChannel(string url)
        {
            string radioTimesId = GetRadioTimesId(url);
            if (radioTimesId == null)
                return false;

            //Retrieve .dat page for channel
            string guide = WebCache.Instance.GetWebData(string.Format("http://xmltv.radiotimes.com/xmltv/{0}.dat", radioTimesId));
            if (string.IsNullOrEmpty(guide))
                return false;

            DateTime startTime = DateTime.Now;
            bool foundNow = false;

            //split into individual programmes
            string[] progs = guide.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string prog in progs)
            {
                string[] progInfo = prog.Split('~');
                if (progInfo.Length != 23) //not a valid line
                    continue;

                if (foundNow) //if previous was current programme, we must be on next programme
                {
                    nextTitle = progInfo[0];
                    nextStart = progInfo[20];
                    nextEnd = progInfo[21];
                    break; //only get Now/Next
                }

                //see if we're on todays date
                DateTime progStart = DateTime.Parse(progInfo[19], new System.Globalization.CultureInfo("en-GB")); //uk date format
                int compare = progStart.Date.CompareTo(startTime.Date);
                if (compare < 0)
                    continue; //before today, skip (not sure if this can ever happen??)
                else if (compare > 0)
                    break; //after today, we've gone too far (shouldn't happen)

                DateTime progStartTime = DateTime.Parse(progInfo[20]);
                DateTime progEndTime = DateTime.Parse(progInfo[21]);
                //if programme starts before current time and ends after current time it is currently playing
                if (progStartTime.TimeOfDay.CompareTo(startTime.TimeOfDay) < 1 && progEndTime.TimeOfDay.CompareTo(startTime.TimeOfDay) > -1)
                {
                    foundNow = true;
                    nowTitle = progInfo[0];
                    nowDescription = progInfo[17];
                    nowStart = progInfo[20];
                    nowEnd = progInfo[21];
                }
            }
            return foundNow;
        }

        public string FormatTVGuide(string tvGuideFormatString)
        {
            string desc = tvGuideFormatString.Replace("<nowtitle>", NowTitle);
            desc = desc.Replace("<nowdescription>", NowDescription);
            desc = desc.Replace("<nowstart>", NowStart);
            desc = desc.Replace("<nowend>", NowEnd);
            desc = desc.Replace("<nexttitle>", NextTitle);
            desc = desc.Replace("<nextstart>", NextStart);
            desc = desc.Replace("<nextend>", NextEnd);
            desc = desc.Replace("<newline>", Environment.NewLine);
            return desc;
        }

        string GetRadioTimesId(string url)
        {
            System.Text.RegularExpressions.Match m = idReg.Match(url);
            if (m.Success)
                return m.Groups[1].Value;
            return null;
        }

        string nowTitle = null;
        public string NowTitle 
        {
            get
            {
                if (nowTitle == null)
                    return "";
                return nowTitle;
            }
            set { nowTitle = value; }
        }

        string nowStart = null;
        public string NowStart
        {
            get
            {
                if (nowStart == null)
                    return "";
                return nowStart;
            }
            set { nowStart = value; }
        }

        string nowEnd = null;
        public string NowEnd
        {
            get
            {
                if (nowEnd == null)
                    return "";
                return nowEnd;
            }
            set { nowEnd = value; }
        }

        string nowDescription = null;
        public string NowDescription 
        {
            get
            {
                if (nowDescription == null)
                    return "";
                return nowDescription;
            }
            set { nowDescription = value; }
        }

        string nextTitle = null;
        public string NextTitle
        {
            get
            {
                if (nextTitle == null)
                    return "";
                return nextTitle;
            }
            set { nextTitle = value; }
        }

        string nextStart = null;
        public string NextStart
        {
            get
            {
                if (nextStart == null)
                    return "";
                return nextStart;
            }
            set { nextStart = value; }
        }

        string nextEnd = null;
        public string NextEnd
        {
            get
            {
                if (nextEnd == null)
                    return "";
                return nextEnd;
            }
            set { nextEnd = value; }
        }
    }
}
