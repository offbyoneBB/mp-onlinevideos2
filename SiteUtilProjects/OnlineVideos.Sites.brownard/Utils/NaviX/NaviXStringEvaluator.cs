using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Utils.NaviX
{
    class NaviXStringEvaluator
    {
        public static bool? Eval(string var1, string var2, string oper)
        {
            if (var1 == null)
                var1 = "";
            if (var2 == null)
                var2 = "";

            bool? result;
            switch (oper.Trim())
            {
                case "=":
                case "==":
                    result = var1 == var2;
                    break;
                case "!=":
                case "<>":
                    result = var1 != var2;
                    break;
                case ">":
                    result = var1.CompareTo(var2) > 0;
                    break;
                case ">=":
                    result = var1.CompareTo(var2) >= 0;
                    break;
                case "<":
                    result = var1.CompareTo(var2) < 0;
                    break;
                case "<=":
                    result = var1.CompareTo(var2) <= 0;
                    break;
                default:
                    Log.Warn("NaviX Processor: Error evaluating if statement - unknown operator {0}", oper);
                    return null;
            }
            return result;
        }
    }
}
