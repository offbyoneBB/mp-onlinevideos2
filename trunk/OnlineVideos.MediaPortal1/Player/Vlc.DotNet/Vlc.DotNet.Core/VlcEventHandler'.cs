namespace Vlc.DotNet.Core
{
    public delegate void VlcEventHandler<in TSender, TArg>(TSender sender, VlcEventArgs<TArg> e);
}