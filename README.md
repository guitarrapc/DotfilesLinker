[![Build](https://github.com/guitarrapc/DotfilesLinker/actions/workflows/build.yaml/badge.svg?event=push)](https://github.com/guitarrapc/DotfilesLinker/actions/workflows/build.yaml)
[![Release](https://github.com/guitarrapc/DotfilesLinker/actions/workflows/release.yaml/badge.svg?event=push)](https://github.com/guitarrapc/DotfilesLinker/actions/workflows/release.yaml)

[日本語](README_ja.md)

# DotfilesLinker

Fast C# Native AOT utility to create symbolic links from dotfiles to your home directory. Supports Windows, Linux, and macOS while respecting your dotfiles repository structure.

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
# Table of Contents

- [Quick Start](#quick-start)
- [How It Works](#how-it-works)
- [Installation](#installation)
- [Usage](#usage)
- [Configuration](#configuration)
- [Security](#security)
- [License](#license)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## Quick Start

1. Download the latest binary from the [GitHub Releases page](https://github.com/guitarrapc/DotfilesLinker/releases/latest) and place it in a directory that is in your PATH.
2. Run executable file `DotfilesLinker` in your terminal.

```sh
# Safe mode, do not overwrite existing files
$ DotfilesLinker

# use --force=y to overwrite destination files
$ DotfilesLinker --force=y
```

## How It Works

DotfilesLinker creates symbolic links based on your dotfiles repository structure:

- Dotfiles in the root directory → linked to `$HOME`
- Files in the `HOME` directory → linked to the corresponding path in `$HOME`
- Files in the `ROOT` directory → linked to the corresponding path in the root directory (`/`) (Linux and macOS only)

## Installation

### Scoop (Windows)

Install DotfilesLinker using [Scoop](https://scoop.sh/):

```sh
$ scoop bucket add guitarrapc https://github.com/guitarrapc/scoop-bucket.git
$ scoop install DotfilesLinker
```

### Download Binary

Download the latest binary from the [GitHub Releases page](https://github.com/guitarrapc/DotfilesLinker/releases) and place it in a directory that is in your PATH.

Available platforms:
- Windows (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)

### Build from Source

```bash
git clone https://github.com/guitarrapc/DotfilesLinker.git
cd DotfilesLinker
dotnet publish -r win-x64 --artifacts-path ./artifacts
```

## Usage

1. Prepare your dotfiles repository structure as shown below.

<details><summary>Linux example</summary>

```sh
dotefiles
├─.bashrc_custom             # link to $HOME/.bashrc_custom
├─.gitignore_global          # link to $HOME/.gitignore_global
├─.gitconfig                 # link to $HOME/.gitconfig
├─aqua.yaml                  # non-dotfiles file automatically ignore
├─dotfiles_ignore            # ignore list for dotfiles link
├─.github
│  └─workflows               # automatically ignore
├─HOME
│  ├─.config
│  │  └─aquaproj-aqua
│  │     └─aqua.yaml         # link to $HOME/.config/aquaproj-aqua/aqua.yaml
│  └─.ssh
│     └─config               # link to $HOME/.ssh/config
└─ROOT
    └─etc
        └─profile.d
           └─profile_foo.sh  # link to /etc/profile.d/profile_foo.sh
```

</details>

<details><summary>Windows example</summary>

```sh
dotefiles
├─dotfiles_ignore            # ignore list for dotfiles link
├─.gitignore_global          # link to $HOME/.gitignore_global
├─.gitconfig                 # link to $HOME/.gitconfig
├─.textlintrc.json           # link to $HOME/.textlintrc.json
├─.wslconfig                 # link to $HOME/.wslconfig
├─aqua.yaml                  # non-dotfiles file automatically ignore
├─.github
│  └─workflows               # automatically ignore
└─HOME
    ├─.config
    │  └─git
    │     └─config           # link to $HOME/.config/git/config
    │     └─ignore           # link to $HOME/.config/git/ignore
    ├─.ssh
    │  ├─config              # link to $HOME/.ssh/config
    │  └─conf.d
    │     └─github           # link to $HOME/.ssh/conf.d/github
    └─AppData
       ├─Local
       │  └─Packages
       │      └─Microsoft.WindowsTerminal_8wekyb3d8bbwe
       │          └─LocalState
       │              └─settings.json   # link to $HOME/AppData/Local/Packages/Microsoft.WindowsTerminal_8wekyb3d8bbwe/LocalState/settings.json
       └─Roaming
           └─Code
               └─User
                  └─settings.json   # link to $HOME/AppData/Roaming/Code/User/settings.json
```

</details>

2. Run the DotfilesLinker command. The `--force=y` option is required to overwrite existing files.

```sh
$ DotfilesLinker --force=y
[o] Skipping already linked: C:\Users\guitarrapc\.textlintrc.json -> D:\github\guitarrapc\dotfiles-win\.textlintrc.json
[o] Skipping already linked: C:\Users\guitarrapc\.wslconfig -> D:\github\guitarrapc\dotfiles-win\.wslconfig
[o] Skipping already linked: C:\Users\guitarrapc\.ssh\config -> D:\github\guitarrapc\dotfiles-win\HOME\.ssh\config
[o] Skipping already linked: C:\Users\guitarrapc\.config\git\config -> D:\github\guitarrapc\dotfiles-win\HOME\.config\git\config
[o] Skipping already linked: C:\Users\guitarrapc\.config\git\ignore -> D:\github\guitarrapc\dotfiles-win\HOME\.config\git\ignore
[o] Skipping already linked: C:\Users\guitarrapc\.ssh\conf.d\aws.conf -> D:\github\guitarrapc\dotfiles-win\HOME\.ssh\conf.d\aws.conf
[o] Skipping already linked: C:\Users\guitarrapc\.ssh\conf.d\github.conf -> D:\github\guitarrapc\dotfiles-win\HOME\.ssh\conf.d\github.conf
[o] Skipping already linked: C:\Users\guitarrapc\Documents\PowerShell\Microsoft.PowerShell_profile.ps1 -> D:\github\guitarrapc\dotfiles-win\HOME\Documents\PowerShell\Microsoft.PowerShell_profile.ps1
[o] Skipping already linked: C:\Users\guitarrapc\Documents\WindowsPowerShell\Microsoft.PowerShell_profile.ps1 -> D:\github\guitarrapc\dotfiles-win\HOME\Documents\WindowsPowerShell\Microsoft.PowerShell_profile.ps1
[o] Skipping already linked: C:\Users\guitarrapc\AppData\Roaming\Code\User\settings.json -> D:\github\guitarrapc\dotfiles-win\HOME\AppData\Roaming\Code\User\settings.json
[o] Skipping already linked: C:\Users\guitarrapc\AppData\Local\Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState\settings.json -> D:\github\guitarrapc\dotfiles-win\HOME\AppData\Local\Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState\settings.json
[o] Skipping already linked: C:\Users\guitarrapc\Documents\Visual Studio 2022\Templates\ItemTemplates\CSharp\Code\1033\Class\Class.cs -> D:\github\guitarrapc\dotfiles-win\HOME\Documents\Visual Studio 2022\Templates\ItemTemplates\CSharp\Code\1033\Class\Class.cs
[o] Skipping already linked: C:\Users\guitarrapc\Documents\Visual Studio 2022\Templates\ItemTemplates\CSharp\Code\1033\Class\Class.vstemplate -> D:\github\guitarrapc\dotfiles-win\HOME\Documents\Visual Studio 2022\Templates\ItemTemplates\CSharp\Code\1033\Class\Class.vstemplate
[o] All operations completed.
```

You can also use the dry-run mode to see what would happen without making any changes:

```sh
$ DotfilesLinker --dry-run
DRY RUN MODE: No files will be actually linked
Starting to link dotfiles from /home/user/dotfiles to /home/user
Using ignore file: dotfiles_ignore
[o] [DRY-RUN] Would create file symlink: /home/user/.gitconfig -> /home/user/dotfiles/.gitconfig
[o] [DRY-RUN] Would create file symlink: /home/user/.config/git/config -> /home/user/dotfiles/HOME/.config/git/config
DRY RUN COMPLETED: No files were actually linked
Dry run completed successfully. No changes were made.
```

3. Verify the symbolic links created by DotfilesLinker.

```sh
$ ls -la $HOME
HOME
drwxr-x--- 18 guitarrapc guitarrapc  4096 Apr 10 03:08 .
drwxr-xr-x  3 root       root        4096 Mar 27 02:33 ..
-rw-r--r--  1 guitarrapc guitarrapc  4015 Mar 27 02:38 .bashrc
lrwxrwxrwx  1 guitarrapc guitarrapc    64 Mar 27 02:38 .bashrc_custom -> /home/guitarrapc/github/guitarrapc/dotfiles/.bashrc_custom
lrwxrwxrwx  1 guitarrapc guitarrapc    60 Mar 27 02:38 .gitconfig -> /home/guitarrapc/github/guitarrapc/dotfiles/.gitconfig
lrwxrwxrwx  1 guitarrapc guitarrapc    67 Mar 27 02:38 .gitignore_global -> /home/guitarrapc/github/guitarrapc/dotfiles/.gitignore_global
drwxr-xr-x  2 guitarrapc guitarrapc  4096 Mar 27 02:38 .ssh

$ ls -la ~/.config/aquaproj-aqua/
total 12
drwxr-xr-x 2 guitarrapc guitarrapc 4096 Mar 27 02:38 .
drwxr-xr-x 5 guitarrapc guitarrapc 4096 Mar 27 18:31 ..
lrwxrwxrwx 1 guitarrapc guitarrapc   86 Mar 27 02:38 aqua.yaml -> /home/guitarrapc/github/guitarrapc/dotfiles/HOME/.config/aquaproj-aqua/aqua.yaml

$ ls -la ~/.ssh
total 12
drwxr-xr-x  2 guitarrapc guitarrapc 4096 Mar 27 02:38 .
drwxr-x--- 18 guitarrapc guitarrapc 4096 Apr 10 03:08 ..
lrwxrwxrwx  1 guitarrapc guitarrapc   66 Mar 27 02:38 config -> /home/guitarrapc/github/guitarrapc/dotfiles/HOME/.ssh/config
```

4. Run the following command to see all available options:

```bash
DotfilesLinker --help
```

## Configuration

### Command Options

All options are optional. The default behavior is to create symbolic links for all dotfiles in the repository.

| Option | Description |
| --- | --- |
| `--help`, `-h` | Display help information |
| `--version` | Display version information |
| `--force=y` | Overwrite existing files or directories |
| `--verbose`, `-v` | Display detailed information during execution |
| `--dry-run`, `-d` | Simulate operations without making any changes to the filesystem |

### Environment Variables

DotfilesLinker can be configured using the following environment variables:

| Variable | Description | Default |
| --- | --- | --- |
| `DOTFILES_ROOT` | Root directory of your dotfiles repository | Current directory |
| `DOTFILES_HOME` | User's home directory | User profile directory (`$HOME`) |
| `DOTFILES_IGNORE_FILE` | Name of the ignore file | `dotfiles_ignore` |

Example usage with environment variables:

```sh
# Set custom dotfiles repository path
export DOTFILES_ROOT=/path/to/my/dotfiles

# Set custom home directory
export DOTFILES_HOME=/custom/home/path

# Run with custom settings
DotfilesLinker --force=y
```

### dotfiles_ignore File

You can specify files or directories to be excluded from linking in the `dotfiles_ignore` file. DotfilesLinker supports several pattern types for flexible file exclusion:

```
# Example dotfiles_ignore
.git
.github
README.md
LICENSE
```

#### Supported Pattern Types

DotfilesLinker supports the following pattern types in the `dotfiles_ignore` file:

```
# Simple filenames or paths that match exactly
.github
README.md
LICENSE

# Wildcard patterns
# `*` matches any string (excluding path separators)
# `?` matches any single character
*.log
temp*
backup.???

# Gitignore-style patterns
# A pattern containing `/` matches a specific path from the repository root
# `**` matches any number of directories (including zero)
# A pattern ending with `/` matches directories only
docs/build/
config/local_*.json
HOME/**.log
**/temp/

# Negation patterns
# A pattern starting with `!` explicitly includes files that would otherwise be ignored
# Processed after non-negated patterns
# --------------------------
# Patterns are processed in two stages:
# 1. First, all non-negation patterns are evaluated
# 2. Then, negation patterns (`!`) are applied and can override previous exclusions
## Exclude all .log files except important.log
*.log
!important.log
## Exclude everything in docs except README.md
docs/
!docs/README.md
```


### Automatic Exclusions

The following files and directories are automatically excluded:
- Version control system folders (`.git`, `.svn`, `.hg`)
- Non-dotfiles in the root directory
- OS-specific files like `.DS_Store` (macOS) and `Thumbs.db` (Windows)
- Temporary files like `*.bak`, `*.tmp`, and vim swap files

## Security

All release artifacts are digitally signed using [Cosign](https://github.com/sigstore/cosign) to ensure their integrity and authenticity. This helps prevent security warnings from antivirus software like Windows Defender.

### Verifying Signatures

You can verify the signatures using the [Cosign CLI](https://github.com/sigstore/cosign#installation):

```bash
# Download the public key (only needed once)
curl -O https://raw.githubusercontent.com/guitarrapc/DotfilesLinker/main/cosign.pub

# Verify an artifact (replace with the artifact you downloaded)
cosign verify-blob --key cosign.pub --signature DotfilesLinker_win_amd64.zip.sig DotfilesLinker_win_amd64.zip
```

A successful verification confirms the file was released officially and has not been tampered with.

Each release also includes:
- SBOM (Software Bill of Materials) files in SPDX format
- SHA256 checksums for all artifacts

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
