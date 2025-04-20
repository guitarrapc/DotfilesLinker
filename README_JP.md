# DotfilesLinker

Dotfilesリポジトリからホームディレクトリやルートディレクトリへのシンボリックリンクを作成します。
dotfilesリポジトリの構造に基づいて、自動的にファイルをリンクします。

## インストール方法

[GitHubリリースページ](https://github.com/guitarrapc/DotfilesLinker/releases)から最新のバイナリをダウンロードし、PATHの通ったディレクトリに配置してください。

対応プラットフォーム:
- Windows (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)

## 使い方

1. 下記のようなdotfilesリポジトリの構造を準備します。

<details><summary>Linux の例</summary>

```sh
dotefiles
├─.bashrc_custom             # $HOME/.bashrc_customへリンク
├─.gitignore_global          # $HOME/.gitignore_globalへリンク
├─.gitconfig                 # $HOME/.gitconfigへリンク
├─aqua.yaml                  # ドットファイルでないため自動的に除外
├─dotfiles_ignore            # dotfilesリンク用除外リスト
├─.github
│  └─workflows               # 自動的に除外
├─HOME
│  ├─.config
│  │  └─aquaproj-aqua
│  │     └─aqua.yaml         # $HOME/.config/aquaproj-aqua/aqua.yamlへリンク
│  └─.ssh
│     └─config               # $HOME/.ssh/configへリンク
└─ROOT
    └─etc
        └─profile.d
           └─profile_foo.sh  # /etc/profile.d/profile_foo.shへリンク
```

</details>

<details><summary>Windows の例</summary>

```sh
dotefiles
├─dotfiles_ignore            # dotfilesリンク用除外リスト
├─.gitignore_global          # $HOME/.gitignore_globalへリンク
├─.gitconfig                 # $HOME/.gitconfigへリンク
├─.textlintrc.json           # $HOME/.textlintrc.jsonへリンク
├─.wslconfig                 # $HOME/.wslconfigへリンク
├─aqua.yaml                  # ドットファイルでないため自動的に除外
├─.github
│  └─workflows               # 自動的に除外
└─HOME
    ├─.config
    │  └─git
    │     └─config           # $HOME/.config/git/configへリンク
    │     └─ignore           # $HOME/.config/git/ignoreへリンク
    ├─.ssh
    │  ├─config              # $HOME/.ssh/configへリンク
    │  └─conf.d
    │     └─github           # $HOME/.ssh/conf.d/githubへリンク
    └─AppData
       ├─Local
       │  └─Packages
       │      └─Microsoft.WindowsTerminal_8wekyb3d8bbwe
       │          └─LocalState
       │              └─settings.json   # $HOME/AppData/Local/Packages/Microsoft.WindowsTerminal_8wekyb3d8bbwe/LocalState/settings.jsonへリンク
       └─Roaming
           └─Code
               └─User
                  └─settings.json   # $HOME/AppData/Roaming/Code/User/settings.jsonへリンク
```

</details>

2. DotfilesLinkerコマンドを実行します。既存のファイルを上書きするには `--force=y` オプションが必要です。

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

3. DotfilesLinkerによって作成されたシンボリックリンクを確認します。

```sh
$ ls -la $HOME
HOME
drwxr-x--- 18 guitarrapc guitarrapc  4096 Apr 10 03:08 .
drwxr-xr-x  3 root       root        4096 Mar 27 02:33 ..
-rw-r--r--  1 guitarrapc guitarrapc  4015 Mar 27 02:38 .bashrc
lrwxrwxrwx  1 guitarrapc guitarrapc    64 Mar 27 02:38 .bashrc_custom -> /home/guitarrapc/github/guitarrapc/dotfiles/.bashrc_custom
lrwxrwxrwx  1 guitarrapapc    60 Mar 27 02:38 .gitconfig -> /home/guitarrapc/github/guitarrapc/dotfiles/.gitconfig
lrwxrwxrwx  1 guitarrapc guitarrapc    67 Mar 27 02:38 .gitignore_global -> /home/guitarrapc/github/guitarrapc/dotfiles/.gitignore_global
drwxr-xr-x  2 guitarrapc guitarrapc  4096 Mar 27 02:38 .ssh
```

## コマンドオプション

すべてのオプションは任意です。デフォルトでは、リポジトリ内のすべてのドットファイルに対してシンボリックリンクを作成します。

| オプション | 説明 |
| --- | --- |
| `--help`, `-h` | ヘルプ情報を表示 |
| `--version` | バージョン情報を表示 |
| `--force=y|n` | `y`を指定すると既存のファイルやディレクトリを上書き。|
| `--verbose`, `-v` | 実行中の詳細情報を表示 |

## 環境変数

DotfilesLinkerは以下の環境変数で設定をカスタマイズできます：

| 変数 | 説明 | デフォルト値 |
| --- | --- | --- |
| `DOTFILES_ROOT` | dotfilesリポジトリのルートディレクトリ | カレントディレクトリ |
| `DOTFILES_HOME` | ユーザーのホームディレクトリ | ユーザープロファイルディレクトリ（`$HOME`） |
| `DOTFILES_IGNORE_FILE` | 除外ファイルの名前 | `dotfiles_ignore` |

環境変数を使用する例：

```sh
# カスタムdotfilesリポジトリのパスを設定
export DOTFILES_ROOT=/path/to/my/dotfiles

# カスタムホームディレクトリを設定
export DOTFILES_HOME=/custom/home/path

# カスタム設定で実行
DotfilesLinker --force=y
```

## 動作原理

DotfilesLinkerは、dotfilesリポジトリの構造に基づいてシンボリックリンクを作成します：

- ルートディレクトリのドットファイル → `$HOME` にリンク
- `HOME` ディレクトリ内のファイル → `$HOME` の対応するパスにリンク
- `ROOT` ディレクトリ内のファイル → ルートディレクトリ（`/`）の対応するパスにリンク（LinuxとmacOSのみ）

## 設定

### オプション

利用可能なすべてのオプションを表示するには、以下のコマンドを実行してください：

```bash
DotfilesLinker --help
```

### .dotfiles_ignore

`.dotfiles_ignore` ファイルを使用して、リンク作成から除外するファイルやディレクトリを指定できます：

```
# .dotfiles_ignore の例
.git
.github
README.md
LICENSE
```

### 自動除外

以下のファイルやディレクトリは自動的に除外されます：
- `.git` で始まるディレクトリ（`.github` など）
- ルートディレクトリの非ドットファイル（先頭が `.` でないファイル）
