using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

namespace OnlineVideos.MediaPortal1
{
    public class DataboundTreeNode : TreeNode
    {
        public DataboundTreeNode(string text) : base(text) { }

        object tag;
        public new object Tag
        {
            get
            {
                return tag;
            }
            set
            {
                if (tag != null && tag is INotifyPropertyChanged) (tag as INotifyPropertyChanged).PropertyChanged -= TagPropertyChanged;
                tag = value;
                if (tag != null && tag is INotifyPropertyChanged) (tag as INotifyPropertyChanged).PropertyChanged += TagPropertyChanged;
            }
        }

        public string TagPropertyBoundToText { get; set; }

        void TagPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (TreeView == null) (sender as INotifyPropertyChanged).PropertyChanged -= TagPropertyChanged;
            else
            {
                if (e.PropertyName == TagPropertyBoundToText)
                {
                    Text = sender.GetType().GetProperty(TagPropertyBoundToText).GetValue(sender, null) as string;
                    TreeView.Refresh();
                }
            }
        }
    }
}
