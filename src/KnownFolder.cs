using System;
using System.IO;
using System.Runtime.InteropServices;
using WozDev.PSKnownFolders.Win32;

namespace WozDev.PSKnownFolders
{
    public sealed class KnownFolder : IDisposable
    {
        private IKnownFolder nativeKnownFolder;

        private KnownFolderDefinition definition;

        private bool _disposed;

        internal KnownFolder(IKnownFolder nativeKnownFolder)
        {
            if (nativeKnownFolder == null)
            {
                throw new ArgumentNullException("nativeKnownFolder");
            }

            this.nativeKnownFolder = nativeKnownFolder;
        }

        public string Name
        {
            get
            {
                return this.Definition.Name;
            }
        }

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
