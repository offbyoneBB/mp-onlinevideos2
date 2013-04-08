using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.MediaPortal2
{
    public static class Guids
    {
        public static readonly Guid WorkFlowModel = new Guid("C418243F-5BD3-4637-8871-DA6545387929");
        public static readonly Guid WorkflowStateSites = new Guid("F9D7500D-EC5C-4FEF-8FAE-E4DED8A22CE0");
		public static readonly Guid WorkflowStateSiteSettings = new Guid("BFDE18C2-0019-43D5-8ED7-8C9C426CE4A1");
        public static readonly string WorkflowStateCategoriesName = "OnlineVideos_Categories_WorkflowState";
        public static readonly Guid WorkflowStateVideos = new Guid("FF474A1A-CA39-4247-BFEA-4E7B578F482B");
        public static readonly Guid WorkflowStateDetails = new Guid("F7DF593E-A606-4096-B8E1-BE702C43A325");
        public static readonly Guid DialogStateSearch = new Guid("F068C0DE-3763-4BA1-A59F-24435DBF0227");
    }
}
