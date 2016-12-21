using System;

namespace OnlineVideos.MediaPortal2
{
    public class CommandContainer<T>
    {
        readonly Action<T> _action;
        public CommandContainer(Action<T> action)
        {
            this._action = action;
        }
        public void Execute(T item)
        {
            _action(item);
        }
    }

    public class CommandContainer<T, S>
    {
        readonly Action<T, S> _action;
        public CommandContainer(Action<T, S> action, S tag)
        {
            this._action = action;
            this.Tag = tag;
        }
        public void Execute(T item)
        {
            _action(item, Tag);
        }

        public S Tag { get; protected set; }
    }
}
