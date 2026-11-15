---
description: 新バージョンのリリース準備（バージョン番号の更新、CHANGELOG.mdの更新、バージョンコミット）を行う
---

# バージョンアップ手順の実行

以下の手順に従って、MediaDeck の新バージョンリリース準備を行ってください。
詳細なルールと背景は [.agents/skills/version-bump/SKILL.md](../../.agents/skills/version-bump/SKILL.md) を参照してください。

## 実行手順

1. **コミットログと前タグの確認**:
   - `git tag --sort=-version:refname` で最新タグを確認。
   - `git log <前タグ>..HEAD --oneline --no-merges` で変更コミットを取得。

2. **バージョン番号の更新**:
   - `Directory.Build.props`: `<Version>`, `<AssemblyVersion>`, `<FileVersion>` を更新。
   - `MediaDeck/Package.appxmanifest`: `<Identity Version="...">` を4桁（X.Y.Z.0）で更新。
   - `MediaDeck/Package.Dev.appxmanifest`: `<Identity Version="...">` を4桁（X.Y.Z.0）で更新。

3. **CHANGELOG.md の更新**:
   - **英語 → 日本語の順で日英併記**する（`## vX.Y.Z` 配下に `### English` セクション、続けて `### 日本語` セクションを配置し、該当するカテゴリ `#### New Features` / `#### Improvements` / `#### Bug Fixes` および `#### 新機能` / `#### 改善` / `#### 不具合修正` に分けて両言語の内容を揃えて記載する）。
   - 一般ユーザー向けに技術用語を避けた平易な表現（英語は `Added ...` / `Improved ...` / `Fixed ...` 等、日本語は「〜できるようになりました」「〜する問題を修正しました」等）で先頭に追記する。
   - 内部改善やリファクタリングは除外する。

4. **検証とコミット**:
   - `dotnet build`、`dotnet test`、`dotnet format` を実行してエラーがないことを確認。
   - ユーザーからコミット作成の明示的な指示がある場合のみ、コミットメッセージ `vX.Y.Z` でコミットを作成（指示がない場合は勝手にコミットしない）。
