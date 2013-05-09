using System;

namespace OnlineVideos.MediaPortal2
{
	public class CommandContainer<T>
	{
		Action<T> action;
		public CommandContainer(Action<T> action)
		{
			this.action = action;
		}
		public void Execute(T item)
		{
			action(item);
		}
	}
}
