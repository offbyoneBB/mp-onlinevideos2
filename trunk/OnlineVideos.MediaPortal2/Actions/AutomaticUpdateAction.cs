using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Workflow;

namespace OnlineVideos.MediaPortal2
{
	public class AutomaticUpdateAction : IWorkflowContributor
	{
		protected readonly IResourceString _displayTitle;

		public AutomaticUpdateAction()
        {
			_displayTitle = LocalizationHelper.CreateResourceString("[OnlineVideos.AutomaticUpdate]");
        }

        #region IWorkflowContributor Member

        public IResourceString DisplayTitle
        {
            get { return _displayTitle; }
        }

		public void Execute()
		{
			IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
			workflowManager.NavigatePush(Guids.DialogStateSiteUpdate);
		}

		public void Initialize()
		{
			
		}

		public bool IsActionEnabled(NavigationContext context)
		{
			if (context.WorkflowModelId == Guids.WorkFlowModelSiteManagement || context.WorkflowState.StateId != Guids.DialogStateSiteUpdate)
			{
				return true;
			}
			return false;
		}

		public bool IsActionVisible(NavigationContext context)
		{
			return context.WorkflowModelId == Guids.WorkFlowModelSiteManagement;
		}

		public event ContributorStateChangeDelegate StateChanged;

		public void Uninitialize()
		{
			
		}

		#endregion
	}
}
