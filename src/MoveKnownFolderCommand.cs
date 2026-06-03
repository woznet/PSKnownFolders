using System;
using System.IO;
using System.Management.Automation;
using System.Runtime.InteropServices;
using WozDev.PSKnownFolders.Win32;

namespace WozDev.PSKnownFolders
{
    /// <summary>
    /// <para type="synopsis">Redirects (moves) a Windows Known Folder to a new path.</para>
    /// <para type="description">
    /// Calls the Shell Known Folder redirection API to change where a Known Folder is stored on disk.
    /// Supports moving a single folder by value or multiple folders from the pipeline, with optional
    /// data migration, read-only probing via <c>-CheckOnly</c>, and pass-through output.
    /// </para>
    /// </summary>
    [Cmdlet(VerbsCommon.Move, "PSKnownFolder", DefaultParameterSetName = "SingleFolder", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
    [Alias("Move-KnownFolder")]
    [OutputType(typeof(KnownFolder))]
    public sealed class MoveKnownFolderCommand : PSCmdlet, IDisposable
    {
        private IKnownFolderManager _knownFolderManager;

        private bool yesToAll;

        private bool noToAll;

        /// <summary>Gets or sets the Known Folder to redirect (single-folder parameter set).</summary>
        [Parameter(ParameterSetName = "SingleFolder", Mandatory = true, Position = 0)]
        public KnownFolder SingleFolder { get; set; }

        /// <summary>Gets or sets the new absolute path for the folder (single-folder parameter set).</summary>
        [Parameter(ParameterSetName = "SingleFolder", Mandatory = true, Position = 1)]
        public string NewPath { get; set; }

        /// <summary>Gets or sets the Known Folder piped from the pipeline (multiple-folders parameter set).</summary>
        [Parameter(ParameterSetName = "MultipleFolders", Mandatory = true, ValueFromPipeline = true)]
        public KnownFolder Folder { get; set; }

        /// <summary>Gets or sets the destination base directory; the folder's existing directory name is appended automatically (multiple-folders parameter set).</summary>
        [Parameter(ParameterSetName = "MultipleFolders", Mandatory = true, Position = 0)]
        public string Destination { get; set; }

        /// <summary>Gets or sets a value indicating that the <c>ShouldContinue</c> confirmation prompt should be suppressed.</summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>Gets or sets a value indicating that the redirection should only be validated without making changes.</summary>
        [Parameter]
        public SwitchParameter CheckOnly { get; set; }

        /// <summary>Gets or sets a value indicating that the redirected <see cref="KnownFolder"/> object should be emitted to the pipeline.</summary>
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        /// <summary>Gets or sets a value indicating that existing data in the source folder should not be moved to the new location.</summary>
        [Parameter]
        public SwitchParameter DontMoveExistingData { get; set; }

        /// <inheritdoc/>
        protected override void BeginProcessing()
        {
            _knownFolderManager = (IKnownFolderManager)new KnownFolderManager();
        }

        /// <inheritdoc/>
        protected override void ProcessRecord()
        {
            switch (this.ParameterSetName)
            {
                case "SingleFolder":
                    this.RedirectOneFolder(this.SingleFolder, this.NewPath);
                    break;
                case "MultipleFolders":
                    var newPath = this.DetermineNewPath(this.Folder, this.Destination);
                    this.RedirectOneFolder(this.Folder, newPath);
                    break;
                default:
                    throw new ArgumentException("Unsupported parameter set name: " + this.ParameterSetName);
            }
        }

        private string DetermineNewPath(KnownFolder knownFolder, string destinationPath)
        {
            var existingPath = knownFolder.Path;
            if (string.IsNullOrEmpty(existingPath))
            {
                throw new InvalidOperationException(string.Format("Unable to determine new path of folder '{0}': existing path is empty.", knownFolder.Name));
            }

            var existingDirectoryName = Path.GetFileName(existingPath);
            if (string.IsNullOrEmpty(existingDirectoryName))
            {
                throw new InvalidOperationException(string.Format("Unable to determine new path of folder '{0}': cannot determine existing directory name.", knownFolder.Name));
            }

            return Path.Combine(destinationPath, existingDirectoryName);
        }

        private void RedirectOneFolder(KnownFolder folder, string newPath)
        {
            if (!folder.CanRedirect)
            {
                throw new InvalidOperationException(string.Format("Folder '{0}' cannot be redirected.", folder.Name));
            }

            string currentPath = folder.Path;
            if (!this.CheckOnly)
            {
                if (!this.ShouldProcess(folder.Name, string.Format("Move from '{0}' to '{1}'", currentPath, newPath)))
                {
                    return;
                }

                if (!this.Force && !this.ShouldContinue(
                    string.Format("Move folder '{0}' from '{1}' to '{2}'?", folder.Name, currentPath, newPath),
                    "Confirm folder redirection",
                    ref this.yesToAll,
                    ref this.noToAll))
                {
                    return;
                }
            }

            var id = new KNOWNFOLDERID(folder.FolderId);
            var flags = KF_REDIRECT_FLAGS.KF_REDIRECT_NONE;
            if (this.CheckOnly)
            {
                flags |= KF_REDIRECT_FLAGS.KF_REDIRECT_CHECK_ONLY;
            }

            if (!this.DontMoveExistingData)
            {
                flags |= KF_REDIRECT_FLAGS.KF_REDIRECT_COPY_CONTENTS | KF_REDIRECT_FLAGS.KF_REDIRECT_DEL_SOURCE_CONTENTS;
            }

            string error;
            HResult hr = _knownFolderManager.Redirect(ref id, IntPtr.Zero, flags, newPath, 0, null, out error);
            int hrCode = unchecked((int)hr);
            if (hrCode < 0)
            {
                if (string.IsNullOrEmpty(error))
                {
                    Marshal.ThrowExceptionForHR(hrCode);
                }
                else
                {
                    throw new COMException(error, hrCode);
                }
            }
            else if (hr != HResult.S_OK)
            {
                this.WriteWarning(string.Format("Redirection returned success code other than S_OK ({0})", hr));
            }

            if (this.PassThru)
            {
                folder.InvalidateDefinitionCache();
                this.WriteObject(folder);
            }
        }

        /// <summary>Releases the COM <see cref="IKnownFolderManager"/> instance.</summary>
        public void Dispose()
        {
            if (_knownFolderManager != null)
            {
                Marshal.ReleaseComObject(_knownFolderManager);
                _knownFolderManager = null;
            }
        }
    }
}
