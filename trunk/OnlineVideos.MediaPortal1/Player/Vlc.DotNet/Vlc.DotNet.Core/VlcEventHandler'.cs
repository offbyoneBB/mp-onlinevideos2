namespace Vlc.DotNet.Core
{
    /// <summary>
    /// EventHandler for Vlc events
    /// </summary>
    /// <typeparam name="TSender">Type of the sender</typeparam>
    /// <typeparam name="TArg">Type of the args</typeparam>
    /// <param name="sender">The sender</param>
    /// <param name="e">The args</param>
    public delegate void VlcEventHandler<in TSender, TArg>(TSender sender, VlcEventArgs<TArg> e);
}