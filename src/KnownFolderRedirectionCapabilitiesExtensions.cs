using WozDev.PSKnownFolders.Win32;

namespace WozDev.PSKnownFolders
{
    internal static class KnownFolderRedirectionCapabilitiesExtensions
    {
        internal static bool HasMask(this KF_REDIRECTION_CAPABILITIES caps, KF_REDIRECTION_CAPABILITIES mask)
        {
            return (caps & mask) != 0;
        }
    }
}
