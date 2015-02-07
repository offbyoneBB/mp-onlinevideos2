using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OnlineVideos.CrossDomain
{
	public class PropertyChangedExecutor : MarshalByRefObject
	{
		public System.ComponentModel.PropertyChangedEventHandler InvokeHandler { get; set; }

		public void Execute(object s, string propertyName)
		{
			InvokeHandler.Invoke(s, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
		}

		#region MarshalByRefObject overrides
		public override object InitializeLifetimeService()
		{
			// In order to have the lease across appdomains live forever, we return null.
			return null;
		}
		#endregion
	}

	public class PropertyChangedDelegator : MarshalByRefObject
	{
		public PropertyChangedExecutor InvokeTarget { get; set; }

		public void EventDelegate(object s, System.ComponentModel.PropertyChangedEventArgs e)
		{
			InvokeTarget.Execute(s, e.PropertyName);
		}

		#region MarshalByRefObject overrides
		public override object InitializeLifetimeService()
		{
			// In order to have the lease across appdomains live forever, we return null.
			return null;
		}
		#endregion
	}
}
