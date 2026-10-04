# PSKnownFolders

A PowerShell binary module that exposes Windows Shell **Known Folders** as first-class cmdlets.
Known folders are the well-known paths managed by the Windows Shell — Desktop, Documents, Downloads,
AppData, and many more. This module lets you enumerate them and redirect (move) their physical
locations without manually editing the registry.

## Requirements

| Requirement | Minimum |
|---|---|
| OS | Windows (Shell COM APIs are Windows-only) |
| PowerShell | 5.1 (Desktop) **or** PowerShell 7+ (Core) |
| .NET | .NET Framework 4.x **or** .NET 6+ |
| Architecture | x64 or x86 — both supported |

## Installation

Copy the module folder to any directory listed in `$env:PSModulePath`:

```powershell
# Example — current user
Copy-Item -Recurse PSKnownFolders_2.5.0 "$HOME\Documents\PowerShell\Modules\PSKnownFolders"

# Then import
Import-Module PSKnownFolders
```

## Commands

| Cmdlet | Alias | Description |
|---|---|---|
| `Get-PSKnownFolder` | `Get-KnownFolder` | Retrieve one or more known folders |
| `Move-PSKnownFolder` | `Move-KnownFolder` | Redirect a known folder to a new path |

---

### Get-PSKnownFolder

Retrieves known folder objects. Use `-All` to list every registered folder or filter by
name, GUID, or scope.

#### Syntax

```
Get-PSKnownFolder [-PerUser] [<CommonParameters>]

Get-PSKnownFolder -Public [<CommonParameters>]

Get-PSKnownFolder -All [<CommonParameters>]

Get-PSKnownFolder [-Name] <string[]> [<CommonParameters>]

Get-PSKnownFolder [-FolderId] <guid[]> [<CommonParameters>]

Get-PSKnownFolder [-SpecialFolder] <Environment+SpecialFolder[]> [<CommonParameters>]
```

#### Parameters

| Parameter | Type | Description |
|---|---|---|
| `-All` | Switch | Return every registered known folder |
| `-Name` | `string[]` | Filter by canonical folder name (e.g. `Desktop`, `Downloads`) |
| `-FolderId` | `guid[]` | Filter by known folder GUID |
| `-SpecialFolder` | `Environment+SpecialFolder[]` | Filter by .NET `SpecialFolder` value (e.g. `MyDocuments`) |
| `-PerUser` | Switch | Return the standard per-user folders (default when no parameters are given). Alias: `-User` |
| `-Public` | Switch | Return the standard public (shared) folders. Alias: `-Common` |

#### Examples

```powershell
# List all known folders
Get-PSKnownFolder -All

# Get a single folder by name
Get-PSKnownFolder -Name Desktop

# Get multiple folders by name
Get-PSKnownFolder -Name Desktop, Downloads, Documents

# Get a folder by GUID
Get-PSKnownFolder -FolderId '{374DE290-123F-4565-9164-39C4925E467B}'

# List only per-user folders
Get-PSKnownFolder -PerUser

# List only public (shared) folders
Get-PSKnownFolder -Public
```

---

### Move-PSKnownFolder

Redirects a known folder to a new path. Supports single-folder and pipeline (multiple-folder)
workflows. Uses `ShouldProcess` — pass `-WhatIf` to preview or `-Confirm` to gate each
redirection interactively.

#### Syntax

```
Move-PSKnownFolder -SingleFolder <KnownFolder> -NewPath <string>
    [-Force] [-CheckOnly] [-PassThru] [-DontMoveExistingData]
    [-WhatIf] [-Confirm] [<CommonParameters>]

Move-PSKnownFolder -Folder <KnownFolder> -Destination <string>
    [-Force] [-CheckOnly] [-PassThru] [-DontMoveExistingData]
    [-WhatIf] [-Confirm] [<CommonParameters>]
```

#### Parameters

| Parameter | Type | Description |
|---|---|---|
| `-SingleFolder` | `KnownFolder` | The folder to move (single-folder parameter set) |
| `-NewPath` | `string` | The target path (single-folder parameter set) |
| `-Folder` | `KnownFolder` | Folder from the pipeline (multi-folder parameter set) |
| `-Destination` | `string` | Base destination directory — each folder is placed in a matching sub-folder |
| `-Force` | Switch | Skip the additional "Confirm folder redirection" (`ShouldContinue`) prompt. Does not suppress the `ShouldProcess` confirmation — use `-Confirm:$false` as well for unattended runs |
| `-CheckOnly` | Switch | Validate the redirect without actually moving files |
| `-PassThru` | Switch | Return the updated `KnownFolder` object after redirection |
| `-DontMoveExistingData` | Switch | Register the new path without migrating existing contents |

#### Examples

```powershell
# Move the Downloads folder
$downloads = Get-PSKnownFolder -Name Downloads
Move-PSKnownFolder -SingleFolder $downloads -NewPath 'D:\Downloads'

# Preview (no changes made)
Move-PSKnownFolder -SingleFolder $downloads -NewPath 'D:\Downloads' -WhatIf

# Validate only — no files moved
Move-PSKnownFolder -SingleFolder $downloads -NewPath 'D:\Downloads' -CheckOnly

# Move several per-user folders to a new drive, pass updated objects through
Get-PSKnownFolder -PerUser |
    Where-Object CanRedirect |
    Move-PSKnownFolder -Destination 'D:\UserFolders' -PassThru
```

---

## KnownFolder Object

`Get-PSKnownFolder` returns `WozDev.PSKnownFolders.KnownFolder` objects. `Move-PSKnownFolder` emits the
same object type only when `-PassThru` is specified; otherwise it produces no output. The object has the
following properties:

| Property | Type | Description |
|---|---|---|
| `Name` | `string` | Canonical folder name (e.g. `Desktop`) |
| `Path` | `string` | Current physical path; `$null` if the folder has no path |
| `FolderId` | `Guid` | Unique identifier for this known folder |
| `FolderTypeId` | `Guid?` | Optional type identifier; `$null` if not set |
| `Category` | `KnownFolderCategory` | `Virtual`, `Fixed`, `Common`, or `PerUser` |
| `CanRedirect` | `bool` | Whether the folder can be redirected |
| `Definition` | `KnownFolderDefinition` | Full definition metadata from the Shell |

The object implements `IDisposable`. PowerShell does not automatically call `Dispose()` for objects flowing
through the pipeline, and it has no C#-style `using` block, so cleanup may otherwise be delayed until garbage
collection. When you hold a folder in a variable, use `try`/`finally` to dispose it deterministically:

```powershell
$folder = Get-PSKnownFolder -Name Downloads
try {
    $folder.Path
}
finally {
    $folder.Dispose()
}
```

