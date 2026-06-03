namespace WozDev.PSKnownFolders
{
    /// <summary>
    /// Describes the category (scope) of a Windows Shell Known Folder,
    /// corresponding to the <c>KF_CATEGORY</c> native enumeration.
    /// </summary>
    public enum KnownFolderCategory
    {
        /// <summary>A virtual folder that does not have a physical path on disk.</summary>
        Virtual = 1,
        /// <summary>A fixed folder whose path cannot be redirected.</summary>
        Fixed,
        /// <summary>A common (shared/public) folder shared by all users.</summary>
        Common,
        /// <summary>A per-user folder that exists separately for each user profile.</summary>
        PerUser
    }
}
