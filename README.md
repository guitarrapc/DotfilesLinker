# DotfilesLinker

Link dotfiles from a repository to your home/root directory.
Depends on your dotfiles repository structure, DotfilesLinker will link your dotfiles.

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->

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

## Installation

Download the latest binary from the [GitHub Releases page](https://github.com/guitarrapc/DotfilesLinker/releases) and place it in a directory that is in your PATH.

Available platforms:
- Windows (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)

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
lrwxrwxrwx 1 guitarrapc guitarrapc   86 Mar 27 02:38 aqua.yaml -> /home/guitarrapc/github/guitarrapc/dotfiles/HOME/.config/aquaproj-aqua/aqua.yam

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

## Command Options

All options are optional. The default behavior is to create symbolic links for all dotfiles in the repository.

| Option | Description |
| --- | --- |
| `--help`, `-h` | Display help information |
| `--version` | Display version information |
| `--force=y` | Overwrite existing files or directories |
| `--verbose`, `-v` | Display detailed information during execution |

## Environment Variables

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

## How It Works

DotfilesLinker creates symbolic links based on your dotfiles repository structure:

- Dotfiles in the root directory → linked to `$HOME`
- Files in the `HOME` directory → linked to the corresponding path in `$HOME`
- Files in the `ROOT` directory → linked to the corresponding path in the root directory (`/`) (Linux and macOS only)

## Configuration

### .dotfiles_ignore

You can specify files or directories to be excluded from linking in the `.dotfiles_ignore` file:

```
# Example .dotfiles_ignore
.git
.github
README.md
LICENSE
```

### Automatic Exclusions

The following files and directories are automatically excluded:
- Directories starting with `.git` (like `.github`)
- Non-dotfiles in the root directory
