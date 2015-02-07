using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace OnlineVideos.Sites
{
    [Serializable]
    public class ContextMenuEntry
    {
        public ContextMenuEntry()
        {
            SubEntries = new BindingList<ContextMenuEntry>();
            SubEntries.ListChanged += (o, e) => { if (e.ListChangedType == ListChangedType.ItemAdded) SubEntries[e.NewIndex].ParentEntry = this; };
        }

        public enum UIAction { Execute, GetText, ShowList, PromptYesNo }

        /// <summary>
        /// The text that should be displayed for this context menu entry.
        /// </summary>
        public string DisplayText { get; set; }

		/// <summary>
		/// The text that should be shown as prompt when <see cref="Action"/> is set to PromptYesNo.
		/// </summary>
		public string PromptText { get; set; }

        /// <summary>
        /// What should happen when this entry is chosen:
        /// <list type="number">
        /// <item><term>Execute</term><description>execute an action in the code</description></item>
        /// <item><term>GetText</term><description>ask the user to input text and then execute an action in the code</description></item>
        /// <item><term>ShowList</term><description>show the list of <see cref="SubEntries"/></description></item>
		/// <item><term>PromptYesNo</term><description>ask the user a question he can confirm or cancel, before the action is executed</description></item>
        /// </list>
        /// </summary>
        public UIAction Action { get; set; }
        
        /// <summary>
        /// If the <see cref="Action"/> is set to <see cref="UIAction.GetText"/>, this will hold the text the user has entered.
        /// </summary>
        public string UserInputText { get; set; }
        
        /// <summary>
        /// If this context menu entry has a list of items to show, add them to this list and set the <see cref="Action"/> to <see cref="UIAction.ShowList"/>.
        /// </summary>
        public BindingList<ContextMenuEntry> SubEntries { get; protected set; }
        
        /// <summary>
        /// When a <see cref="ContextMenuEntry"/> is added to the <see cref="SubEntries"/>, its <see cref="ParentEntry"/> is automatically set.
        /// </summary>
        public ContextMenuEntry ParentEntry { get; protected set; }
        
        /// <summary>
        /// Make sure the class of this object is attributed as <see cref="Serializable"/> or inherits from <see cref="MarshalByRef"/>, because it has to cross AppDomain boundaries.
        /// </summary>
        public object Other { get; set; }
    }

    [Serializable]
    public class ContextMenuExecutionResult
    {
        /// <summary>
        /// Any text set to this property will be displayed to the user after executing the <see cref="ContextMenuEntry"/>. It can be a success message or error info.
        /// </summary>
        public string ExecutionResultMessage { get; set; }

        /// <summary>
        /// Setting this property to true will force the GUI to retrieve the current items (categories or videos) from the site again.
        /// </summary>
        public bool RefreshCurrentItems { get; set; }

        /// <summary>
        /// If you execution results in a list of videos or categories, set them to this property and they will be displayed in the GUI.
        /// </summary>
        public List<SearchResultItem> ResultItems { get; set; }
    }
}
