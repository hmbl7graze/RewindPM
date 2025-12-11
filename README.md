# RewindPM

RewindPMは過去任意の時点にさかのぼってプロジェクトの振り返りを行うことができるプロジェクト管理ツールです。

## 機能仕様

### プロジェクト管理
ユーザーは複数のプロジェクトを管理できる。

**プロジェクトのプロパティ**
- プロジェクトID（自動採番）
- プロジェクト名
- 説明
- 複数のタスク（論理的な関連。実装上はTaskAggregateとして独立管理）

### タスク管理
**タスクのプロパティ**
- タスクID（自動採番）
- プロジェクトID（所属するプロジェクトへの参照）
- タスク名
- 説明（自由記述）
- **予定期間と工数**
  - 予定開始日
  - 予定終了日
  - 予定工数（時間）
- **実績期間と工数**
  - 実績開始日
  - 実績終了日
  - 実績工数（時間）
- **ステータス**
  - TODO
  - 進行中
  - レビュー中
  - 完了
- **操作履歴**
  - 各変更時に操作者情報を記録

**注意:** 予定期間と工数は`ScheduledPeriod` Value Objectとして、実績期間と工数は`ActualPeriod` Value Objectとして実装し、バリデーションをカプセル化する。

### 画面構成

UI/UXの詳細仕様は[UI_MVP.md](UI_MVP.md)を参照。

**主要画面:**
- **ホーム画面:** プロジェクト一覧とプロジェクト作成
- **プロジェクト管理画面:** ガントチャート形式のタスク一覧、タスク詳細モーダル、リワインド機能

### リワインド機能の動作詳細

**編集履歴の例:**
```
1. 1月2日 10:00 - タスクA作成
2. 1月2日 11:00 - タスクAのステータス変更
3. 1月3日 11:00 - タスクB作成
4. 1月3日 15:00 - タスクAの予定日変更
5. 1月5日 10:00 - タスクC作成（最新）
```

**操作の流れ:**
1. 初期表示：最新の状態（5）を表示、右ボタンは無効
2. 左ボタンを押す：1月3日の最後の状態（4）を表示
   - 1月4日は編集がないため、スキップ
3. もう一度左ボタンを押す：1月2日の最後の状態（2）を表示
4. 右ボタンを押す：1月3日の最後の状態（4）に戻る

**重要な仕様:**
- 同じ日に複数回編集された場合、その日の最後の状態のみを表示
- 理由：振り返りは日単位で十分であり、日中の細かい変更履歴は不要と判断
- 実装上はイベントを時刻単位で記録し、UI側で日単位にフィルタリング

## ブランチ戦略

**mainとdevelopの二つのブランチを軸に開発を進める**
- `develop`: 開発ブランチ（通常の作業はこちら）
- `main`: 安定版ブランチ（マイルストーン達成時にマージ）

### マイルストーン一覧

#### 1. 基盤構築
ドメイン層とイベントソーシングの基礎を動かす
- ✅ プロジェクト構造の作成
- ✅ CQRS対応のプロジェクト構成への移行
- ✅ CI/CDの設定
- ドメインモデルの実装（Aggregate、Value Object）
- ドメインイベントの実装
- ✅ イベントストアの実装（SQLite + EF Core）
- ✅ Event Storeのユニットテスト実装
- 最小限のBlazor UI（タスク作成とリプレイ結果の表示のみ）

#### 2. プロジェクト管理の基本UI
基本的なCRUD操作ができるUIを作る
- プロジェクト一覧画面
- プロジェクト作成・編集機能
- ガントチャート形式のタスク一覧表示
- タスク作成・編集モーダル
- タスクステータス変更
- Read Modelの実装（最新状態の高速表示用）

#### 3. リワインド機能
過去の状態を表示する機能の実装
- 過去の時点のタスク状態の表示
- 日付の移動機能（左右トグルボタン）
- 過去表示中の編集制限

#### 4. UIのブラッシュアップ
暫定実装の改善と仕上げ
- UIとバックエンド全体で暫定にしていた部分を仕上げる
- パフォーマンス最適化
- ユーザビリティの向上

## 将来対応機能

### ユーザー管理機能
- プロジェクトメンバー管理
- タスクへの担当者設定
- 担当者ごとのタスク一覧ビュー
- **注意:** ログインや認証は実装しない方針（疑似ログイン画面は検討可）

### タスク管理の拡張
- **タスクへの追加プロパティ**
  - 担当者（AssignedTo）
  - 優先度（Priority）
  - 進捗率（ProgressPercentage）
- **タスク間の依存関係**
  - 先行タスク・後続タスクの設定
  - ガントチャートでの依存関係の可視化
- **カンバン形式でのタスク管理**
  - ステータスごとのカラム表示
  - ドラッグアンドドロップでのステータス変更

### リワインド機能の拡張
- カレンダーピッカーで特定の日に直接ジャンプ
- 月単位での移動機能
- 時刻単位での表示オプション

### プロジェクト振り返り機能の強化
- タスクの統計情報
- バーンダウンチャート
- 進捗推移の可視化
- 予定と実績の差分分析

### UI/UX改善
- タスク説明のMarkdown対応
- コメント機能
- 変更履歴の詳細表示

## 技術スタック

### 基盤技術
- **プラットフォーム:** .NET 10 / C# 13
- **フロントエンド:** Blazor Server
- **データベース:** SQLite
- **ORM:** Entity Framework Core

### アーキテクチャパターン
- **CQRS** (Command Query Responsibility Segregation)
  - Write側とRead側を完全に分離したアーキテクチャ
  - コマンド：MediatR v12を使用（MITライセンス）
  - 将来的に自作ライブラリへの切り替えも検討
- **イベントソーシング** (自前実装)
  - Event StoreをSQLiteで実装
  - すべての変更をイベントとして永続化
- **レイヤードアーキテクチャ** (CQRS対応)
  - **Presentation層** (RewindPM.Web) - Blazor UI
  - **Application層**
    - RewindPM.Application.Write - Command、CommandHandler
    - RewindPM.Application.Read - Query、QueryHandler、DTO
  - **Domain層** (RewindPM.Domain) - Aggregate、Value Object、Domain Event
  - **Infrastructure層**
    - RewindPM.Infrastructure.Write - Event Store実装、Repository
    - RewindPM.Infrastructure.Read - Read Model DB、Query実装
  - **Projection層** (RewindPM.Projection) - Domain EventをRead DBに反映
- **DDD** (Domain-Driven Design)
  - Aggregate、Value Object、Domain Eventパターンの採用
  - ドメインロジックをドメイン層に集約

### 開発・テスト
- **テストフレームワーク:** xUnit
- **バリデーション:** FluentValidation（Application層で使用）
- **CI/CD:** GitHub Actions

### 開発方針
- C#のベストプラクティスに準拠
- 依存関係の原則を厳守（Domain層は他層に依存しない）
- テスタビリティの重視

## 設定

### タイムゾーン設定

RewindPMは日単位でプロジェクトの振り返りを行うため、タイムゾーンの設定が重要です。

#### 設定方法

`RewindPM.Web/appsettings.json` の `TimeZone` セクションで設定します:

```json
{
  "TimeZone": {
    "TimeZoneId": "Asia/Tokyo"
  }
}
```

**利用可能なタイムゾーンID:**

RewindPMは.NETの標準タイムゾーンIDを使用します。プラットフォームによって使用可能なタイムゾーンIDが異なるため、注意が必要です。

**Windows環境:**
- `UTC` - 協定世界時
- `Tokyo Standard Time` - 日本標準時 (JST, UTC+9)
- `Eastern Standard Time` - 米国東部時間
- `GMT Standard Time` - 英国時間
- その他、Windowsの標準タイムゾーンID（[一覧はこちら](https://learn.microsoft.com/ja-jp/windows-hardware/manufacture/desktop/default-time-zones)）

**Linux/macOS環境（IANA形式）:**
- `UTC` - 協定世界時
- `Asia/Tokyo` - 日本標準時 (JST, UTC+9)
- `America/New_York` - 米国東部時間
- `Europe/London` - 英国時間
- その他、IANA タイムゾーンデータベースのID

**クロスプラットフォーム対応:**

異なるプラットフォームで同じアプリケーションを実行する場合、環境に応じた設定ファイルを用意することをお勧めします:

```json
// appsettings.Development.json (Windows開発環境)
{
  "TimeZone": {
    "TimeZoneId": "Tokyo Standard Time"
  }
}

// appsettings.Production.json (Linux本番環境)
{
  "TimeZone": {
    "TimeZoneId": "Asia/Tokyo"
  }
}
```

無効なタイムゾーンIDが指定された場合、自動的にUTCにフォールバックされ、警告がログに記録されます。

#### タイムゾーン変更時の注意

**重要:** タイムゾーンを変更してアプリケーションを再起動すると、ReadModelデータベース（プロジェクト、タスク、履歴データ）が自動的に削除されます。

- **EventStore（イベント履歴）は保持されます** - イベントデータは変更されません
- **ReadModelは再作成が必要です** - データを再作成するか、新規にプロジェクトを作成してください
- タイムゾーン変更は慎重に行ってください

#### 動作の仕組み

1. アプリケーション起動時に、設定ファイルのタイムゾーンIDと、データベースに保存されているタイムゾーンIDを比較
2. 変更が検出された場合、ReadModelデータベースの内容を削除
3. 新しいタイムゾーン設定でアプリケーションが起動
4. すべての日付処理（スナップショット作成、タイムトラベル機能など）が新しいタイムゾーンに基づいて動作