---
name: version-bump
description: >-
  新バージョンのリリース準備、バージョン番号の更新、CHANGELOG.mdの更新、
  またはバージョンコミットを行う際に使用する。
---

# バージョンアップ & リリース準備手順

新バージョンのリリース準備、バージョン番号更新、CHANGELOG更新、およびバージョンコミットを行う際の手順書です。

## 1. コミットログと前回タグの確認

1. 前回タグを確認する:
   ```bash
   git tag --sort=-version:refname
   ```
2. 前回タグからのコミットログを取得する:
   ```bash
   git log <前回タグ>..HEAD --oneline --no-merges
   ```

## 2. バージョン番号の更新
以下の3箇所のバージョン番号を新バージョンに更新します。

1. **[Directory.Build.props](file:///Directory.Build.props)**
   - `<Version>`: 新バージョン（例: `1.0.3`）
   - `<AssemblyVersion>`: 新バージョン（例: `1.0.3.0`）
   - `<FileVersion>`: 新バージョン（例: `1.0.3.0`）
2. **[MediaDeck/Package.appxmanifest](file:///MediaDeck/Package.appxmanifest)**
   - `<Identity Version="...">`: 新バージョン（4桁形式、例: `1.0.3.0`）
3. **[MediaDeck/Package.Dev.appxmanifest](file:///MediaDeck/Package.Dev.appxmanifest)**
   - `<Identity Version="...">`: 新バージョン（4桁形式、例: `1.0.3.0`）
   - ※ Debug ビルドで使われるため忘れずに更新すること。

## 3. CHANGELOG.md の更新

[CHANGELOG.md](file:///CHANGELOG.md) の先頭に新バージョンのエントリを追加します。

### CHANGELOG の記載ルール
- **対象読者**: 一般ユーザー向け。
- **言語・記載順序**: **英語 → 日本語の順で日英併記**すること。1つのバージョン見出し（`## vX.Y.Z`）の中に、先に `### English` セクション、続いて `### 日本語` セクションを配置する。
  - ※ `.github/workflows/release.yml` が `## vX.Y.Z` から次の `## v` までを GitHub Release ノートとして自動抽出するため、バージョン見出し以外の行では `## v` を使わないこと。
- **記載基準**: ユーザーの操作・体験に直接影響する変更のみを記載する。内部的なリファクタリング、コード品質、開発用スクリプトの変更などは記載しない。英語と日本語で記載内容を揃える。
- **文体**: 技術用語・クラス名・メソッド名は使わず、英語・日本語ともに一般ユーザー向けの平易な表現で書く（英語は `Added ...` / `Improved ...` / `Fixed ...` 等、日本語は「〜できるようになりました」「〜する問題を修正しました」等）。

### 記載フォーマット例（該当するカテゴリのみ記載）
```markdown
## vX.Y.Z

### English
#### New Features
- Added ...

#### Improvements
- Improved ...

#### Bug Fixes
- Fixed ...

### 日本語
#### 新機能
- 〜を追加しました

#### 改善
- 〜を改善しました

#### 不具合修正
- 〜する問題を修正しました
```

## 4. ビルド・テスト・フォーマットの検証

作業完了前に、ビルドエラー、テスト失敗、フォーマット違反がないことを確認します。

```bash
dotnet build
dotnet test
dotnet format --verify-no-changes
```

## 5. コミットの作成

検証に合格し、**ユーザーからコミット作成の明示的な指示がある場合のみ**、更新した設定ファイルと CHANGELOG.md をコミットします（明示的な指示がない場合は勝手にコミットせず、変更内容の報告に留めること）。

- **コミットメッセージ形式**: `vX.Y.Z`（例: `v1.0.3`）
