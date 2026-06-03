using System;

namespace WozDev.PSKnownFolders
{
    /// <summary>
    /// Flags describing the behavioural definition of a Windows Shell Known Folder,
    /// corresponding to the <c>KF_DEFINITION_FLAGS</c> native enumeration.
    /// </summary>
    [Flags]
    public enum KnownFolderDefinitionFlags
    {
        /// <summary>No flags are set.</summary>
        None = 0,
        /// <summary>The folder can be personalized per-user. Not documented in the Windows 8.1 SDK.</summary>
        [Obsolete("Not documented in Windows 8.1 SDK")]
        Personalize = 0x00000001,
        /// <summary>The folder can only be redirected to a local path (not a network share).</summary>
        LocalRedirectOnly = 0x00000002,
        /// <summary>The folder is roamable — its contents can follow the user across devices.</summary>
        Roamable = 0x00000004,
        /// <summary>The folder should be created when the user's profile is initialized.</summary>
        PreCreate = 0x00000008,
        /// <summary>The folder is backed by a stream rather than a physical directory.</summary>
        Stream = 0x00000010,
        /// <summary>The folder's expanded path (with environment variables resolved) is published to the registry.</summary>
        PublishExpandedPath = 0x00000020,
        /// <summary>No UI is shown to the user when redirecting this folder.</summary>
        NoRedirectUI = 0x00000040,
    }
}
