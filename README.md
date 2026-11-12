<p align="center">
  <img src="MediaDeck/Assets/Square150x150Logo.scale-200.png" width="128" height="128" alt="MediaDeck Icon" />
</p>

<h1 align="center">MediaDeck</h1>

[![.NET](https://img.shields.io/badge/.NET-11.0-512bd4.svg)](https://dotnet.microsoft.com/download/dotnet/11.0)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

  Windows向けの高機能メディア管理アプリケーションです。画像、動画、PDF、アーカイブファイルなどを一元管理し、高度な検索、タグ付け、メタデータ管理機能を提供します。


## 主な機能

- **リポジトリ管理**: 複数のローカルフォルダをリポジトリとして登録し、一括して管理。
- **高度な検索とフィルタリング**:
  - **高速検索**: SQLiteと非同期fetchの最適化により、数万件のライブラリも待機時間なく瞬時にフィルタリング。
  - **プロパティ検索**: `prop.` プレフィックスを用いた詳細なプロパティ比較（解像度、ファイルサイズ、日付、評価等）。専用ダイアログで直感的に操作可能。
  - **多様な条件**: タグ、ファイルパス、解像度、評価、メディアタイプ、位置情報を自在に組み合わせ。
- **大規模データの高速表示**: `IAsyncEnumerable` を活用したストリーミング読み込みにより、UIをブロックせずバックグラウンドで逐次表示。
- **メタデータ・タグ管理**: Magick.NET や FFMpegCore を利用した自動抽出。タグカテゴリ、ふりがな（読み）、エイリアス機能による高度な整理。
- **多彩なビューア**: リスト表示、グリッド表示、詳細表示に加え、位置情報を利用したマップ表示に対応。


## スクリーンショット

| メイン画面 | マップビュー |
| :---: | :---: |
| ![MainWindow](https://github.com/xm-i/MediaDeck/wiki/images/main.webp) | ![MapView](https://github.com/xm-i/MediaDeck/wiki/images/map.webp) |

## 対応メディアタイプ

- **画像**: JPEG, PNG, GIF, BMP, TIFF, HEIF, PSD, RAW (RAF), ICO, PCX, Netpbm 等
- **動画**: MP4, MKV, AVI, MOV 等 (FFMpegがサポートする形式)
- **PDF**: Windows.Data.Pdf を利用した高速な表示とサムネイル生成
- **アーカイブ**: ZIP, 7z, RAR 等

## はじめかた

### 必要要件
- Windows 10 (1809) 以降

### インストール

以下のいずれかの方法でインストールできます。

**Microsoft Store**

[![Microsoft Store](https://img.shields.io/badge/Microsoft%20Store-からダウンロード-0078d4?logo=microsoft)](https://apps.microsoft.com/detail/9p4g7d3p2xm4)

**GitHub Releases**

[GitHub Releases](https://github.com/xm-i/MediaDeck/releases) から最新の zip ファイルをダウンロードして展開してください。

## 設定

初回起動時に設定ファイルが自動生成されます。アプリ内の設定画面から以下の調整が可能です。

- **スキャン**: 対象リポジトリのパス、スキャン対象の拡張子管理。
- **検索/読み込み**: バッチサイズや最大ロード件数の調整。
- **実行プログラム**: 特定のメディアタイプを外部プログラム（VLC等）で開く設定。
- **サムネイル**: 生成されるサムネイルのサイズ・品質設定。
- **表示言語**: 日本語、英語の切り替え（システムの既定に従うことも可能）。

### 起動引数

指定形式（共通）:

- `--<key> <value>`
- `--<key>=<value>`
- `/<key>:<value>`

| 引数名 | 値 | 説明 |
| --- | --- | --- |
| `base` | `<path>` | 起動時に `BaseDirectory` を指定します。指定したディレクトリを基準に、状態ファイル・設定ファイル・サムネイルフォルダ・DBファイルの保存先が決まります。 |

例:

```powershell
MediaDeck.exe --base C:\MediaDeckData
```

## ライセンス

このプロジェクトは [MIT License](LICENSE) の下で公開されています。

---

## 開発者向け情報

### 技術スタック

- **フレームワーク**: .NET 11 / WinUI 3 (Windows App SDK)
- **アーキテクチャ**: MVVM パターン
- **リアクティブ・状態管理**: [R3](https://github.com/Cysharp/R3)
- **データベース**: SQLite (Entity Framework Core)
- **画像・メタデータ**: Magick.NET, MetadataExtractor
- **動画処理**: FFMpegCore
- **ロギング**: Serilog
- **設定管理**: GenJsonConfig
- **DI・コード生成**: AutoDiAttributes

### プロジェクト構成

```
MediaDeck/
├── MediaDeck/                  # メインアプリケーション (WinUI 3 / Views / Styles)
├── MediaDeck.ViewModels/       # ViewModels (R3ベースのReactiveProperty/Command)
├── MediaDeck.Core/             # ビジネスロジック・検索エンジン・モデル
├── MediaDeck.MediaItemTypes/   # メディアタイプの共通定義とロジック実装
├── MediaDeck.MediaItemTypes.UI/# メディアタイプ別のUIコンポーネント
├── MediaDeck.Store/            # 状態管理・設定永続化 (GenJsonConfig)
├── MediaDeck.Composition/      # システム構成（DI、データベース層/EF Core）
├── MediaDeck.Common/           # 共通ユーティリティ・基底クラス類
└── lib/                        # 外部ライブラリ・サブモジュール
```

### ビルド

```powershell
# リポジトリのクローン
git clone --recursive https://github.com/xm-i/MediaDeck.git

# ソリューションのビルド
dotnet build -r win-x64
```
