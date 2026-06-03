using System;
using System.IO;
using WozDev.PSKnownFolders.Win32;

namespace WozDev.PSKnownFolders
{
    /// <summary>
    /// Exposes the definition metadata (<c>KNOWNFOLDER_DEFINITION</c>) for a Windows Shell Known Folder,
    /// including its name, paths, description strings, parent GUID, and behavioural flags.
    /// </summary>
    public sealed class KnownFolderDefinition
    {
        private readonly KNOWNFOLDER_DEFINITION nativeDefinition;

        internal KnownFolderDefinition(IKnownFolder nativeKnownFolder)
        {
            if (nativeKnownFolder == null)
            {
                throw new ArgumentNullException(nameof(nativeKnownFolder));
            }

            this.nativeDefinition = KNOWNFOLDER_DEFINITION.FromKnownFolder(nativeKnownFolder);
        }

        /// <summary>Gets the canonical name of the Known Folder (e.g. <c>"Documents"</c>).</summary>
        public string Name
        {
            get
            {
                return this.nativeDefinition.name;
            }
        }

        /// <summary>Gets the Shell namespace parsing name (e.g. <c>"::{…}"</c> or a file-system path).</summary>
        public string ParsingName
        {
            get
            {
                return this.nativeDefinition.parsingName;
            }
        }

        /// <summary>Gets the localized display name resource string for the folder.</summary>
        public string LocalizedName
        {
            get
            {
                return this.nativeDefinition.localizedName;
            }
        }

        /// <summary>Gets the description string for the folder.</summary>
        public string Description
        {
            get
            {
                return this.nativeDefinition.description;
            }
        }

        /// <summary>Gets the tooltip resource string for the folder.</summary>
        public string ToolTip
        {
            get
            {
                return this.nativeDefinition.toolTip;
            }
        }

        /// <summary>Gets the icon resource string for the folder.</summary>
        public string Icon
        {
            get
            {
                return this.nativeDefinition.icon;
            }
        }

        /// <summary>Gets the SDDL security descriptor string applied when the folder is created.</summary>
        public string Security
        {
            get
            {
                return this.nativeDefinition.security;
            }
        }

        /// <summary>Gets the path of the folder relative to its parent Known Folder.</summary>
        public string RelativePath
        {
            get
            {
                return this.nativeDefinition.relativePath;
            }
        }

        /// <summary>
        /// Gets the folder type GUID, or <see langword="null"/> if no type is defined.
        /// </summary>
        public Guid? FolderTypeId
        {
            get
            {
                return this.nativeDefinition.ftidType != Guid.Empty ? (Guid?)this.nativeDefinition.ftidType : null;
            }
        }

        /// <summary>
        /// Gets the GUID of the parent Known Folder, or <see langword="null"/> if there is no parent.
        /// </summary>
        public Guid? ParentId
        {
            get
            {
                return this.nativeDefinition.fidParent != Guid.Empty ? (Guid?)this.nativeDefinition.fidParent : null;
            }
        }

        /// <summary>Gets the category (scope) of the Known Folder.</summary>
        public KnownFolderCategory Category
        {
            get
            {
                return (KnownFolderCategory)this.nativeDefinition.category;
            }
        }

        /// <summary>Gets the behavioural definition flags for the Known Folder.</summary>
        public KnownFolderDefinitionFlags Flags
        {
            get
            {
                return (KnownFolderDefinitionFlags)this.nativeDefinition.kfdFlags;
            }
        }

        /// <summary>Gets the file-system attributes set on the folder when it is created.</summary>
        public FileAttributes Attributes
        {
            get
            {
                return (FileAttributes)this.nativeDefinition.dwAttributes;
            }
        }

        /// <summary>Returns the canonical name of the Known Folder.</summary>
        public override string ToString()
        {
            return this.Name;
        }
    }
}
