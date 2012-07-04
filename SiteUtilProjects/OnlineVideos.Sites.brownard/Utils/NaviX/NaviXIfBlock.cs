using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.Sites.Utils.NaviX.Processor
{
    class NaviXIfBlock
    {
        bool ifNext = false;
        public bool IfNext { get { return ifNext; } set { ifNext = value; } }

        bool ifSatisfied = false;
        public bool IfSatisified { get { return ifSatisfied; } set { ifSatisfied = value; } }

        bool ifEnd = false;
        public bool IfEnd { get { return ifEnd; } set { ifEnd = value; } }
    }
}
