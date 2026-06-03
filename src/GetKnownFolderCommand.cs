using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Runtime.InteropServices;
using WozDev.PSKnownFolders.Win32;

namespace WozDev.PSKnownFolders
{
    [Cmdlet(VerbsCommon.Get, "PSKnownFolder", DefaultParameterSetName = "PerUser")]
    [Alias("Get-KnownFolder")]
    [OutputType(typeof(KnownFolder))]
    public sealed class GetKnownFolderCommand : PSCmdlet, IDisposable
    {
        private static readonly HashSet<Guid> UserFolders = new HashSet<Guid>
        {
            KnownFolderIds.FOLDERID_Documents.value,
            KnownFolderIds.FOLDERID_Contacts.value,
            KnownFolderIds.FOLDERID_Links.value,
            KnownFolderIds.FOLDERID_Music.value,
            KnownFolderIds.FOLDERID_Pictures.value,
            KnownFolderIds.FOLDERID_Downloads.value,
            KnownFolderIds.FOLDERID_Desktop.value,
            KnownFolderIds.FOLDERID_Favorites.value,
            KnownFolderIds.FOLDERID_Videos.value,
            KnownFolderIds.FOLDERID_SavedSearches.value,
            KnownFolderIds.FOLDERID_SavedGames.value,
        };

        private static readonly HashSet<Guid> PublicFolders = new HashSet<Guid>
        {
            KnownFolderIds.FOLDERID_PublicDocuments.value,
            KnownFolderIds.FOLDERID_PublicMusic.value,
            KnownFolderIds.FOLDERID_PublicPictures.value,
            KnownFolderIds.FOLDERID_PublicDownloads.value,
            KnownFolderIds.FOLDERID_PublicVideos.value,
        };

        private IKnownFolderManager _knownFolderManager;

        [Parameter(ParameterSetName = "ByName", Mandatory = true, Position = 0)]
        public string[] Name { get; set; }

        [Parameter(ParameterSetName = "ByFolderId", Mandatory = true, Position = 0)]
        public Guid[] FolderId { get; set; }

        [Parameter(ParameterSetName = "BySpecialFolder", Mandatory = true, Position = 0)]
        public Environment.SpecialFolder[] SpecialFolder { get; set; }

        [Parameter(ParameterSetName = "All")]
        public SwitchParameter All { get; set; }

        [Alias("Common")]
        [Parameter(ParameterSetName = "Public")]
        public SwitchParameter Public { get; set; }

        [Alias("User")]
        [Parameter(ParameterSetName = "PerUser")]
        public SwitchParameter PerUser { get; set; }

        protected override void BeginProcessing()
        {
            _knownFolderManager = (IKnownFolderManager)new KnownFolderManager();
        }

        protected override void ProcessRecord()
        {
            IEnumerable<IKnownFolder> result;
            switch (this.ParameterSetName)
            {
                case "ByName":
                    result = this.GetByNames(this.Name);
                    break;
                case "BySpecialFolder":
                    result = this.GetByNames(this.SpecialFolder.Select(sf => sf == Environment.SpecialFolder.Personal ? "Personal" : sf.ToString()));
                    break;
                case "ByFolderId":
                    result = this.GetByIds(this.FolderId);
                    break;
                case "PerUser":
                    result = this.GetByIds(UserFolders);
                    break;
                case "Public":
                    result = this.GetByIds(PublicFolders);
                    break;
                case "All":
                    result = this.GetAll();
                    break;
                default:
                    throw new ArgumentException("Unsupported parameter set name: " + this.ParameterSetName);
            }

            // Create KnownFolder objects and sort by Category and Name
            var knownFolders = result.Select(kf => new KnownFolder(kf))
                                    .OrderBy(kf => kf.Category)
                                    .ThenBy(kf => kf.Name);

            foreach (var folder in knownFolders)
            {
                this.WriteObject(folder);
            }
        }

        private List<IKnownFolder> GetAll()
        {
            KNOWNFOLDERID[] ids;

            IntPtr ppIds = Marshal.AllocHGlobal(IntPtr.Size);
            try
            {
                Marshal.WriteIntPtr(ppIds, IntPtr.Zero);
                uint count = 0;
                _knownFolderManager.GetFolderIds(ppIds, ref count);
                IntPtr pIds = Marshal.ReadIntPtr(ppIds);
                if (pIds == IntPtr.Zero)
                {
                    throw new InvalidOperationException("GetFolderIds returned NULL");
                }

                try
                {
                    ids = new KNOWNFOLDERID[count];
                    int stride = Marshal.SizeOf<KNOWNFOLDERID>();
                    for (int u = 0; u < (int)count; u++)
                        ids[u] = Marshal.PtrToStructure<KNOWNFOLDERID>(IntPtr.Add(pIds, u * stride));
                }
                finally
                {
                    Marshal.FreeCoTaskMem(pIds);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ppIds);
            }

            var folders = new List<IKnownFolder>(ids.Length);
            foreach (var id in ids)
            {
                try
                {
                    folders.Add(this.GetKnownFolderById(id));
                }
                catch (Exception ex)
                {
                    this.WriteError(new ErrorRecord(
                        ex,
                        "KnownFolderNotFound",
                        ErrorCategory.ObjectNotFound,
                        id.value));
                }
            }
            return folders;
        }

        private IEnumerable<IKnownFolder> GetByNames(IEnumerable<string> names)
        {
            return names.Select(name => this.GetKnownFolderByName(name)).ToList();
        }

        private IEnumerable<IKnownFolder> GetByIds(IEnumerable<Guid> folderIds)
        {
            return folderIds.Select(folderId => this.GetKnownFolderById(new KNOWNFOLDERID(folderId.ToString()))).ToList();
        }

        private IKnownFolder GetKnownFolderById(KNOWNFOLDERID knownFolderId)
        {
            IKnownFolder nativeKnownFolder;
            try
            {
                _knownFolderManager.GetFolder(ref knownFolderId, out nativeKnownFolder);
            }
            catch (FileNotFoundException x)
            {
                throw new FileNotFoundException(string.Format("Known folder not found: {0}", knownFolderId.value), x);
            }

            return nativeKnownFolder;
        }

        private IKnownFolder GetKnownFolderByName(string name)
        {
            IKnownFolder nativeKnownFolder;
            try
            {
                _knownFolderManager.GetFolderByName(name, out nativeKnownFolder);
            }
            catch (FileNotFoundException x)
            {
                throw new FileNotFoundException(string.Format("Known folder not found: '{0}'", name), x);
            }

            return nativeKnownFolder;
        }

        protected override void Dispose(bool disposing)
        {
            if (_knownFolderManager != null)
            {
                Marshal.ReleaseComObject(_knownFolderManager);
                _knownFolderManager = null;
            }

            base.Dispose(disposing);
        }
    }
}
