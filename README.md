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
git clone https://github.com/guitarrapc/dotfiles.git ~/.dotfiles
# Safe mode, do not overwrite existing files
DotfilesLinker --root ~/.dotfiles --dry-run
# use --force to overwrite destination files
DotfilesLinker --root ~/.dotfiles --force
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

2. Run the DotfilesLinker command. The `--force` option is required to overwrite existing files.

```sh
$ DotfilesLinker --force
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
| `--root PATH` | Directory containing dotfiles; takes precedence over `DOTFILES_ROOT` |
| `--force` | Overwrite existing files or directories |
| `--verbose`, `-v` | Display detailed information during execution |
| `--dry-run`, `-d` | Simulate operations without making any changes to the filesystem |

### Environment Variables

DotfilesLinker can be configured using the following environment variables:

| Variable | Description | Default |
| --- | --- | --- |
| `DOTFILES_ROOT` | Root directory used when `--root` is omitted | Current directory |
| `DOTFILES_HOME` | User's home directory | User profile directory (`$HOME`) |
| `DOTFILES_IGNORE_FILE` | Name of the ignore file | `dotfiles_ignore` |

Example usage with environment variables:

```sh
# Set custom dotfiles repository path
export DOTFILES_ROOT=/path/to/my/dotfiles

# Set custom home directory
export DOTFILES_HOME=/custom/home/path

# Run with custom settings
DotfilesLinker --force
```

The command-line option takes precedence over the environment variable:

```sh
DotfilesLinker --root /path/to/my/dotfiles
```

### dotfiles_ignore File

You can specify files or directories to be excluded from linking in the `dotfiles_ignore` file. Rules use gitignore-style syntax and paths are relative to the dotfiles repository root.

```
# Example dotfiles_ignore
.git
.github
README.md
LICENSE
```

#### Gitignore-style Rules

Rules are evaluated from top to bottom. If multiple rules match, the last matching rule wins. Empty lines and lines beginning with `#` are ignored.

```
# A name without `/` matches at any depth
.github
README.md
LICENSE

# Wildcards
# `*` matches any string (excluding path separators)
# `?` matches any single character
# `[a-z]` matches one character in a range
*.log
temp*
backup.???
file[0-9].txt

# A pattern containing `/` is relative to the repository root
# A leading `/` explicitly anchors a pattern to the repository root
# `**` matches any number of directories (including zero)
# A pattern ending with `/` matches directories only
docs/build/
/config/local_*.json
HOME/**/*.log
**/temp/

# Negation patterns
# A pattern starting with `!` explicitly includes files that would otherwise be ignored
## Exclude all .log files except important.log
*.log
!important.log
## Exclude everything in docs except README.md
docs/
!docs/
docs/*
!docs/README.md

# Escape a leading `#` or `!` to match it literally
\#notes
\!important
```

As with Git, a file cannot be re-included while one of its parent directories remains excluded. Re-include the parent directory first, as shown in the `docs/README.md` example. Built-in automatic exclusions cannot be overridden by negation rules.

#### Compatibility with `.gitignore`

`dotfiles_ignore` implements a practical subset of the [Git ignore pattern format](https://git-scm.com/docs/gitignore), but it is not a drop-in replacement for Git's complete ignore mechanism.

Supported behavior:

| Feature | Support |
| --- | --- |
| Empty lines and comments beginning with `#` | Supported |
| Escaped leading `\#` and `\!` | Supported |
| Unescaped trailing spaces are ignored | Supported |
| Escaped trailing spaces are significant | Supported |
| Last matching rule wins | Supported |
| Negation with `!` | Supported, including Git's excluded-parent restriction |
| Patterns without `/` | Match a file or directory name at any depth |
| Leading `/` and patterns containing `/` | Match relative to the dotfiles repository root |
| Trailing `/` | Matches directories and their descendants only |
| `*` and `?` | Supported within one path segment |
| Simple character classes such as `[abc]`, `[0-9]`, `[!abc]`, and `[^abc]` | Supported |
| `**/name`, `dir/**`, and `a/**/b` | Supported |
| Backslash escapes such as `file\*.txt` | Supported within a path segment |

Differences and unsupported behavior:

| Git behavior | DotfilesLinker behavior |
| --- | --- |
| Git combines repository `.gitignore` files, nested `.gitignore` files, `.git/info/exclude`, a global excludes file, and command-line rules | Only the configured `DOTFILES_IGNORE_FILE` is read once from the repository root; the default filename is `dotfiles_ignore` |
| A nested `.gitignore` uses its containing directory as the pattern base | Nested ignore files are not discovered; all slash-containing patterns use the dotfiles repository root as their base |
| Case sensitivity follows Git/filesystem configuration such as `core.ignoreCase` | Matching is always case-insensitive on every platform |
| Git uses its complete wildmatch/fnmatch character-class behavior | POSIX classes such as `[[:digit:]]`, collating/equivalence classes, and uncommon literal `]` class forms are not supported |
| Git defines only specific placements of consecutive `**` as special | Only `**` used as a complete path segment in the documented forms is Git-compatible; other consecutive-star forms are not validated and may behave like `*` |
| Git ignore rules affect untracked-file discovery and interact with Git's index | DotfilesLinker has no tracked/untracked concept; rules only decide which repository files are considered for linking |
| Git has no mandatory built-in ignore patterns | DotfilesLinker's automatic exclusions are applied separately and cannot be re-included with `!` |


### Automatic Exclusions

The following files and directories are automatically excluded:
- Version control system folders/files (`.git`, `.svn`, `.hg`)
- Non-dotfiles in the root directory
- OS-specific files like `.DS_Store` (macOS) and `Thumbs.db` (Windows)
- Temporary files like `*.bak`, `*.tmp`, and vim swap files

## Security

Release archives have signed GitHub artifact attestations for both build provenance and their associated SBOM. The attestations use short-lived Sigstore certificates issued through GitHub Actions, so no long-lived signing key is required.

### Verifying Attestations

Use the [GitHub CLI](https://cli.github.com/) to verify that an archive was produced by this repository's release workflow:

```bash
# Verify build provenance
gh attestation verify DotfilesLinker_win_amd64.zip --repo guitarrapc/DotfilesLinker

# Verify the SPDX SBOM attestation
gh attestation verify DotfilesLinker_win_amd64.zip \
  --repo guitarrapc/DotfilesLinker \
  --predicate-type https://spdx.dev/Document/v2.2
```

A successful verification confirms that the archive matches an attestation signed by this repository's GitHub Actions workflow.

Each release also includes:
- An SPDX JSON SBOM for each release archive
- SHA256 checksums for all release archives and SBOM files

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
