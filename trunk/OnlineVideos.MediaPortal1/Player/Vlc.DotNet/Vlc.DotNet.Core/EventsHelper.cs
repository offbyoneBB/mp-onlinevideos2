using System;

namespace Vlc.DotNet.Core
{
    internal static class EventsHelper
    {
        public delegate void ExecuteRaiseEvent(Delegate singleInvoke, object sender, object arg);

        public static ExecuteRaiseEvent ExecuteRaiseEventDelegate { get; set; }
        public static bool CanRaiseEvent { get; set; }

        static EventsHelper()
        {
            CanRaiseEvent = false;
        }

        public static void RaiseEvent<TSender, THandler>(VlcEventHandler<TSender, THandler> handler, TSender sender, VlcEventArgs<THandler> arg)
        {
            if (handler == null || ExecuteRaiseEventDelegate == null)
                return;
            foreach (VlcEventHandler<TSender, THandler> singleInvoke in handler.GetInvocationList())
            {
                if (!CanRaiseEvent)
                    return;
                ExecuteRaiseEventDelegate(singleInvoke, sender, arg);
            }
        }
    }
}