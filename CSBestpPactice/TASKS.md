# タスクリスト / ロードマップ

## 凡例
- ✅ 完了
- 🔄 作業中
- ⬜ 未着手
- 🔀 方針変更

---

## 方針変更履歴

| # | 変更内容 | 理由 |
|---|---|---|
| 1 | `IRepository<T>` と `IRepositoryAsync<T>` を分離 | sync / async で別インターフェースにすることで必要な方だけ実装できる |
| 2 | `DbSession` 4クラス → 1クラスに統合 | sync・async・DataTable の責務はすべて「DBとのセッション管理」で一致。`EF Core の DbContext` と同様に同期・非同期を同一クラスに持つ設計に変更 |
| 3 | `IDbSession` インターフェースを追加 | Repository コンストラクターへの注入・テスト時のモック差し替えを可能にするため |
| 4 | `IProductRepositoryAsync` を削除し `IProductRepository` に統合 | UI アプリは同期・非同期どちらか一方しか使わないため分離の意味が薄く、`IProductRepository : IRepository<Product>, IRepositoryAsync<Product>` の単一インターフェースに統合して Service からの注入を簡素化 |
| 5 | `DbSession` を遅延オープン方式に変更 | 起動時に `Open()` を呼ぶ設計はリソースを無駄に保持するため、`BuildCommand` / `BeginTransaction` で接続が閉じていれば自動で `Open()` する設計に変更 |
| 6 | `Repositories/DataTables/` を追加 | DataAdapter パターンとは分離し、`DbSession.QueryDataTable` を使う DataTable 返しのルートをデモとして追加 |
| 7 | `DataAdapters/` を廃止し `ProductTableRepository` に `Update(DataTable)` を統合 | `Microsoft.Data.Sqlite` は `SqliteDataAdapter` を提供しないため、DataAdapter パターンは `IDbSession` を使った手動実装で `ProductTableRepository` に集約 |

---

## Phase 1：共通層

### ✅ Step 1：Domain 層
- ✅ `CSBestpPactice.Domain` プロジェクト作成
- ✅ `Product` エンティティ
- ✅ `IRepository<T>`（同期）
- ✅ `IRepositoryAsync<T>`（非同期）
- 🔀 `IProductRepository`：同期・非同期を統合（`IRepository<Product>` + `IRepositoryAsync<Product>` を継承）（方針変更 #4）
- 🔀 `IProductRepositoryAsync`：削除（方針変更 #4）

### ✅ Step 2：Infrastructure 層
- ✅ `CSBestpPactice.Infrastructure` プロジェクト作成
- ✅ NuGet パッケージ追加（Dapper / EF Core / SQLite / Npgsql）
- ✅ フォルダ構成作成
- ✅ `IDbConnectionFactory` インターフェース
- ✅ `SqliteConnectionFactory`
- ✅ `PostgreSqlConnectionFactory`
- 🔀 `DbSession` 4クラス → `IDbSession` + `DbSession` 1クラスに統合（方針変更 #2・#3）
- ✅ `IDbSession`（全操作のインターフェース）
- ✅ `DbSession`（sync・async・DataTable を統合した実装）
- ✅ `DbParam`
- ✅ `DataTableExtensions`
- ✅ `Repositories/AdoNet/ProductRepository`（同期・非同期を1クラスに統合）
- ✅ `Repositories/Dapper/ProductRepository`（同期・非同期を1クラスに統合）
- ✅ `Repositories/EfCore/AppDbContext`
- ✅ `Repositories/EfCore/ProductRepository`（同期・非同期を1クラスに統合）
- 🔀 `DbSession` 遅延オープン方式に変更（方針変更 #5）
- ✅ `AdoNet/ProductRepository`：BLOB/TEXT 両対応の `ReadGuid` ヘルパー追加
- 🔀 `Repositories/DataTables/IProductTableRepository` + `ProductTableRepository`（方針変更 #6）
- 🔀 `ProductTableRepository` に `Update(DataTable)` を追加（方針変更 #7）

### ✅ Step 3：Service 層
- ✅ `CSBestpPactice.Service` プロジェクト作成
- ✅ `IProductService` インターフェース
- ✅ `ProductService` 実装

---

## Phase 2：UI サンプル

### 🔄 Step 4：App.WinForms.ManualDI
- ✅ プロジェクト作成
- ✅ `App.config` 接続文字列設定
- ✅ `ConfigurationManager` で設定読み込み
- ✅ `Program.cs`：手動で `new` して DI 組み立て
- ✅ `Form1`：商品一覧（DataGridView）— エンティティルート / DataTable ルート並列表示
- ✅ CRUD 機能（追加・編集・削除）— ADO.NET Repository ルート
- ✅ `ProductTableRepository.Update(DataTable)` 追加（方針変更 #7）
- ✅ DataTable ルートの CRUD 画面（`btnUpdate_Click` で `dgvProductsTable` から書き戻し、Name/UnitPrice 必須バリデーション付き）

### ✅ Step 5：App.WinForms.DIContainer
- ✅ プロジェクト作成
- ✅ `appsettings.json` 接続文字列設定
- ✅ `ServiceCollection` + `BuildServiceProvider()`
- ✅ `Program.cs`：DI コンテナ組み立て（`IDbSession` は `Singleton` 登録し、ManualDI と同様にアプリ全体で1接続を共有）
- ✅ `Form1`：商品一覧（DataGridView）— エンティティルート / DataTable ルート並列表示
- ✅ CRUD 機能（追加・編集・削除）— ADO.NET Repository ルート
- ✅ DataTable ルートの CRUD 画面（`btnUpdate_Click` で `dgvProductsTable` から書き戻し）

### ⬜ Step 6：App.WinForms.Host
- ⬜ プロジェクト作成
- ⬜ `appsettings.json` 接続文字列設定
- ⬜ `IHostBuilder` + WinForms 統合パターン
- ⬜ `Program.cs`：Generic Host 組み立て
- ⬜ `MainForm`：商品一覧（DataGridView）

### ⬜ Step 7：App.Console
- ⬜ プロジェクト作成
- ⬜ `appsettings.json` 接続文字列設定
- ⬜ `IHostBuilder` で起動
- ⬜ CRUD 操作のデモ出力

### ⬜ Step 8：App.Wpf
- ⬜ プロジェクト作成
- ⬜ `appsettings.json` 接続文字列設定
- ⬜ Generic Host + MVVM パターン
- ⬜ `MainViewModel`：商品一覧（ObservableCollection）
- ⬜ `MainWindow.xaml`：DataGrid バインディング
