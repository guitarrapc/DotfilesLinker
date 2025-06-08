[![Build](https://github.com/guitarrapc/DotfilesLinker/actions/workflows/build.yaml/badge.svg?event=push)](https://github.com/guitarrapc/DotfilesLinker/actions/workflows/build.yaml)
[![Release](https://github.com/guitarrapc/DotfilesLinker/actions/workflows/release.yaml/badge.svg?event=push)](https://github.com/guitarrapc/DotfilesLinker/actions/workflows/release.yaml)

[English](README.md)

# DotfilesLinker

C# Native AOTで実装された高速な dotfiles シンボリックリンク作成ツール。WindowsやLinux、macOSに対応し、dotfilesリポジトリの構造を尊重します。

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->
# Table of Contents

- [クイックスタート](#%E3%82%AF%E3%82%A4%E3%83%83%E3%82%AF%E3%82%B9%E3%82%BF%E3%83%BC%E3%83%88)
- [動作原理](#%E5%8B%95%E4%BD%9C%E5%8E%9F%E7%90%86)
- [インストール方法](#%E3%82%A4%E3%83%B3%E3%82%B9%E3%83%88%E3%83%BC%E3%83%AB%E6%96%B9%E6%B3%95)
- [使い方](#%E4%BD%BF%E3%81%84%E6%96%B9)
- [設定](#%E8%A8%AD%E5%AE%9A)
- [セキュリティ](#%E3%82%BB%E3%82%AD%E3%83%A5%E3%83%AA%E3%83%86%E3%82%A3)
- [ライセンス](#%E3%83%A9%E3%82%A4%E3%82%BB%E3%83%B3%E3%82%B9)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## クイックスタート

1. [GitHubリリースページ](https://github.com/guitarrapc/DotfilesLinker/releases/latest)から最新のバイナリをダウンロードし、PATHの通ったディレクトリに配置します。
2. ターミナルで実行ファイル `DotfilesLinker` を実行します。

```sh
# 安全モード、既存ファイルを上書きしません
$ DotfilesLinker

# --force=y オプションで既存ファイルを上書き
$ DotfilesLinker --force=y
```

## 動作原理

DotfilesLinkerは、dotfilesリポジトリの構造に基づいてシンボリックリンクを作成します：

- ルートディレクトリのドットファイル → `$HOME` にリンク
- `HOME` ディレクトリ内のファイル → `$HOME` の対応するパスにリンク
- `ROOT` ディレクトリ内のファイル → ルートディレクトリ（`/`）の対応するパスにリンク（LinuxとmacOSのみ）

## インストール方法

### Scoop (Windows)

[Scoop](https://scoop.sh/)を使用してDotfilesLinkerをインストールできます：

```sh
$ scoop bucket add guitarrapc https://github.com/guitarrapc/scoop-bucket.git
$ scoop install DotfilesLinker
```

### バイナリダウンロード

[GitHubリリースページ](https://github.com/guitarrapc/DotfilesLinker/releases)から最新のバイナリをダウンロードし、PATHの通ったディレクトリに配置してください。

対応プラットフォーム:
- Windows (x64, ARM64)
- Linux (x64, ARM64)
- macOS (x64, ARM64)

### ソースからビルド

```bash
git clone https://github.com/guitarrapc/DotfilesLinker.git
cd DotfilesLinker
dotnet publish -r win-x64 --artifacts-path ./artifacts
```

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

ドライランモードを使用して、実際に変更を加えずに何が起こるかを確認することもできます：

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

3. DotfilesLinkerによって作成されたシンボリックリンクを確認します。

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

4. 利用可能なすべてのオプションを表示するには、以下のコマンドを実行します：

```bash
DotfilesLinker --help
```

## 設定

### コマンドオプション

すべてのオプションは任意です。デフォルトでは、リポジトリ内のすべてのドットファイルに対してシンボリックリンクを作成します。

| オプション | 説明 |
| --- | --- |
| `--help`, `-h` | ヘルプ情報を表示 |
| `--version` | バージョン情報を表示 |
| `--force=y` | 既存のファイルやディレクトリを上書き |
| `--verbose`, `-v` | 実行中の詳細情報を表示 |
| `--dry-run`, `-d` | ファイルシステムに変更を加えずに処理をシミュレーション |

### 環境変数

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

### dotfiles_ignore ファイル

`dotfiles_ignore` ファイルを使用して、リンク作成から除外するファイルやディレクトリを指定できます。DotfilesLinkerは柔軟なファイル除外のために複数のパターンタイプをサポートしています：

```
# dotfiles_ignore の例
.git
.github
README.md
LICENSE
```

#### サポートされているパターンタイプ

DotfilesLinkerは `dotfiles_ignore` ファイルで以下のパターンタイプをサポートしています。

```
# 完全に一致するシンプルなファイル名やパス
.github
README.md
LICENSE

# ワイルドカードパターン
# `*`: 任意の文字列（パス区切り文字を除く）にマッチ
# `?`: 任意の1文字にマッチ
*.log
temp*
backup.???

# Gitignoreスタイルパターン
# /を含むパスパターン - リポジトリルートからの特定のパスにマッチ
# ** - 任意の数のディレクトリ（ゼロを含む）にマッチ
# ディレクトリのみのパターン（`/`で終わる）- ディレクトリのみにマッチ
docs/build/
config/local_*.json
HOME/**.log
`**/temp/

# 否定パターン
# `!`で始まるパターンで、通常なら無視されるファイルを明示的に含める
# 非否定パターンの後に処理される
# --------------------------
# パターンは2段階で処理されます：
# 1. まず、すべての非否定パターンが評価される
# 2. 次に、否定パターン（!で始まる）が適用され、以前の除外決定を上書きできる
## important.log以外のすべての.logファイルを除外
*.log
!important.log
## docs内のREADME.md以外のすべてを除外
docs/
!docs/README.md
```

### 自動除外

以下のファイルやディレクトリは自動的に除外されます：
- バージョン管理システムのフォルダ（`.git`、`.svn`、`.hg`）
- ルートディレクトリの非ドットファイル（先頭が `.` でないファイル）
- OS固有のファイル（macOSの`.DS_Store`、Windowsの`Thumbs.db`など）
- 一時ファイル（`*.bak`、`*.tmp`、vimのスワップファイルなど）

## セキュリティ

すべてのリリースアーティファクトは、整合性と信頼性を確保するために[Cosign](https://github.com/sigstore/cosign)を使用してデジタル署名されています。これにより、Windows Defenderなどのウイルス対策ソフトウェアからのセキュリティ警告を防止できます。

### 署名の検証

[Cosign CLI](https://github.com/sigstore/cosign#installation)を使用して署名を検証できます：

```bash
# 公開鍵をダウンロード（初回のみ必要）
curl -O https://raw.githubusercontent.com/guitarrapc/DotfilesLinker/main/cosign.pub

# アーティファクトを検証（ダウンロードしたアーティファクトに合わせて変更）
cosign verify-blob --key cosign.pub --signature DotfilesLinker_win_amd64.zip.sig DotfilesLinker_win_amd64.zip
```

検証が成功すれば、そのファイルが公式にリリースされ、改ざんされていないことが確認できます。

各リリースには以下も含まれています：
- SBOM（ソフトウェア部品表）ファイル（SPDX形式）
- すべてのアーティファクトのSHA256チェックサム

## ライセンス

このプロジェクトはMITライセンスの下で公開されています - 詳細は[LICENSE](LICENSE)ファイルをご覧ください。
