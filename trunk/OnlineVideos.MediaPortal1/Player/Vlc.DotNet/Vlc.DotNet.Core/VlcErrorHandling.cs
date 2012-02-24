namespace Vlc.DotNet.Core
{
    /// <summary>
    /// VlcErrorHandling class
    /// </summary>
    public sealed class VlcErrorHandling
    {
        internal VlcErrorHandling()
        {
        }

        public void ClearError()
        {
            if (VlcContext.InteropManager != null &&
                VlcContext.InteropManager.ErrorHandlingInterops != null &&
                VlcContext.InteropManager.ErrorHandlingInterops.ClearError.IsAvailable)
            {
                VlcContext.InteropManager.ErrorHandlingInterops.ClearError.Invoke();
            }
        }
        public string GetErrorMessage()
        {
            if (VlcContext.InteropManager != null &&
                VlcContext.InteropManager.ErrorHandlingInterops != null &&
                VlcContext.InteropManager.ErrorHandlingInterops.ClearError.IsAvailable)
            {
                return IntPtrExtensions.ToStringAnsi(VlcContext.InteropManager.ErrorHandlingInterops.GetErrorMessage.Invoke());
            }
            return null;
        }
    }
}