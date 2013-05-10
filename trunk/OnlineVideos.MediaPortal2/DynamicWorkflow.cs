using System;
using System.Collections.Generic;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;

namespace OnlineVideos.MediaPortal2
{
	public static class DynamicWorkflow
	{
		/// <summary>
		/// Creates a <see cref="WorkflowAction"/> that pushes a dialog as transient state on the navigation stack.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="displayLabel"></param>
		/// <param name="dialogItems"></param>
		/// <param name="sourceState"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		public static WorkflowAction CreateDialogMenuAction(Guid id, string name, string displayLabel, ItemsList dialogItems, WorkflowState sourceState, Action<ListItem> action)
		{
			return new PushTransientStateNavigationTransition(
				id,
				sourceState.Name + "->" + name,
				displayLabel,
				new Guid[] { sourceState.StateId },
				WorkflowState.CreateTransientState(name, displayLabel, true, "ovsDialogGenericItems", false, WorkflowType.Dialog),
				LocalizationHelper.CreateResourceString(displayLabel))
			{
				SortOrder = name,
				WorkflowNavigationContextVariables = new Dictionary<string, object>
					{
						{ Constants.CONTEXT_VAR_ITEMS, dialogItems },
						{ Constants.CONTEXT_VAR_COMMAND, new CommandContainer<ListItem>(action) }
					}
			};
		}
	}
}
