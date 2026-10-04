using System;
using System.IO;
using System.Runtime.InteropServices;
using WozDev.PSKnownFolders.Win32;

namespace WozDev.PSKnownFolders
{
    /// <summary>
    /// Represents a Windows Shell Known Folder and exposes its properties and lifetime management.
    /// </summary>
    public sealed class KnownFolder : IDisposable
    {
        private IKnownFolder nativeKnownFolder;

        private KnownFolderDefinition definition;

        private bool _disposed;

        internal KnownFolder(IKnownFolder nativeKnownFolder)
        {
            if (nativeKnownFolder == null)
            {
                throw new ArgumentNullException(nameof(nativeKnownFolder));
            }

            this.nativeKnownFolder = nativeKnownFolder;
        }

        /// <summary>Gets the canonical name of this Known Folder (e.g. <c>"Documents"</c>).</summary>
        public string Name
        {
            get
            {
                return this.Definition.Name;
            }
        }

        /// <summary>
        /// Gets the file-system path of this Known Folder, or <see langword="null"/> if the
        /// folder has no physical path or the path could not be retrieved.
        /// </summary>
        public string Path
        {
            get
            {
                ThrowIfDisposed();
                string path;
                try
                {
                    this.nativeKnownFolder.GetPath(KNOWN_FOLDER_FLAG.KF_FLAG_NONE, out path);
                }
                catch (DirectoryNotFoundException)
                {
                    return null;
                }
                catch (FileNotFoundException)
                {
                    return null;
                }
                catch (COMException ex) when (ex.HResult == unchecked((int)0x80004005))
                {
                    return null;
                }

                return path;
            }
        }

        /// <summary>
        /// Gets whether this Known Folder can be redirected (moved) to a different path.
        /// Returns <see langword="false"/> if the folder has deny-all or lacks allow-all redirection capabilities.
        /// </summary>
        public bool CanRedirect
        {
            get
            {
                ThrowIfDisposed();
                KF_REDIRECTION_CAPABILITIES caps;
                this.nativeKnownFolder.GetRedirectionCapabilities(out caps);
                return caps.HasMask(KF_REDIRECTION_CAPABILITIES.KF_REDIRECTION_CAPABILITIES_ALLOW_ALL)
                    && !caps.HasMask(KF_REDIRECTION_CAPABILITIES.KF_REDIRECTION_CAPABILITIES_DENY_ALL);
            }
        }

        /// <summary>Gets the category (scope) of this Known Folder.</summary>
        public KnownFolderCategory Category
        {
            get
            {
                ThrowIfDisposed();
                KF_CATEGORY cat;
                this.nativeKnownFolder.GetCategory(out cat);
                return (KnownFolderCategory)cat;
            }
        }

        /// <summary>Gets the unique identifier (GUID) of this Known Folder.</summary>
        public Guid FolderId
        {
            get
            {
                ThrowIfDisposed();
                KNOWNFOLDERID id;
                this.nativeKnownFolder.GetId(out id);
                return id.value;
            }
        }

        /// <summary>
        /// Gets the folder type GUID of this Known Folder, or <see langword="null"/> if the folder has no type assigned.
        /// </summary>
        public Guid? FolderTypeId
        {
            get
            {
                ThrowIfDisposed();
                FOLDERTYPEID typeid;
                try
                {
                    this.nativeKnownFolder.GetFolderType(out typeid);
                }
                catch (COMException ex) when (ex.HResult == unchecked((int)0x80004001))
                {
                    return null;
                }

                return typeid.value;
            }
        }

        /// <summary>Gets the full definition metadata for this Known Folder as reported by the Shell.</summary>
        public KnownFolderDefinition Definition
        {
            get
            {
                ThrowIfDisposed();
                if (this.definition == null)
                {
                    this.definition = new KnownFolderDefinition(this.nativeKnownFolder);
                }

                return this.definition;
            }
        }

        /// <summary>Releases the underlying COM Known Folder object.</summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (nativeKnownFolder != null)
                {
                    Marshal.ReleaseComObject(nativeKnownFolder);
                    nativeKnownFolder = null;
                }
            }
        }

        internal void InvalidateDefinitionCache()
        {
            this.definition = null;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(KnownFolder));
            }
        }
    }
}
