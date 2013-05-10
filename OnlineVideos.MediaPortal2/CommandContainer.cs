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

	public class CommandContainer<T, S>
	{
		Action<T,S> action;
		public CommandContainer(Action<T,S> action, S tag)
		{
			this.action = action;
			this.Tag = tag;
		}
		public void Execute(T item)
		{
			action(item, Tag);
		}

		public S Tag { get; protected set; }
	}
}
