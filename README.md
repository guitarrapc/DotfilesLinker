# DotfilesLinker

Link dotfiles from a repository to your home/root directory.
Depends on your dotfiles repository structure, DotefilesLinker will link your dotfiles.

## Getting Started

1. Prepare your dotfiles repository structure like below.

<details><summary>Linux example</summary>

```
# Linux
.
├─.bashrc_custom             # link to $HOME/.bashrc_custom
├─.dotfiles_ignore           # ignore list for dotfiles link
├─.gitignore_global          # link to $HOME/.gitignore_global
├─.gitconfig                 # link to $HOME/.gitconfig
├─aqua.yaml                  # non-dotfiles file automatically ignore
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
        ├─docker
        │  └─daemon.json     # link to /etc/docker/daemon.json
        └─profile.d
           └─profile_foo.sh  # link to /etc/profile.d/profile_foo.sh
```

</details>

<details><summary>Windows example</summary>

```
# Windows
.
├─.dotfiles_ignore           # ignore list for dotfiles link
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
    ├─.docker
    │     └─daemon.json      # link to $HOME/.docker/daemon.json
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

2. Run `DotfilesLinker` command. --force option is required to overwrite existing files. (default is `n`)

```
$ DotfilesLinker --force y
```
