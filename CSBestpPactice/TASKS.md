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

---

## Phase 1：共通層

### ✅ Step 1：Domain 層
- ✅ `CSBestpPactice.Domain` プロジェクト作成
- ✅ `Product` エンティティ
- ✅ `IRepository<T>`（同期）
- ✅ `IRepositoryAsync<T>`（非同期）
- ✅ `IProductRepository`（同期）
- ✅ `IProductRepositoryAsync`（非同期）

### 🔄 Step 2：Infrastructure 層
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
- ⬜ `Repositories/AdoNet/ProductRepository`
- ⬜ `Repositories/Dapper/ProductRepository`
- ⬜ `Repositories/EfCore/AppDbContext`
- ⬜ `Repositories/EfCore/ProductRepository`

### ⬜ Step 3：Service 層
- ⬜ `CSBestpPactice.Service` プロジェクト作成
- ⬜ `IProductService` インターフェース
- ⬜ `ProductService` 実装

---

## Phase 2：UI サンプル

### ⬜ Step 4：App.WinForms.ManualDI
- ⬜ プロジェクト作成
- ⬜ `App.config` 接続文字列設定
- ⬜ `ConfigurationManager` で設定読み込み
- ⬜ `Program.cs`：手動で `new` して DI 組み立て
- ⬜ `MainForm`：商品一覧（DataGridView）

### ⬜ Step 5：App.WinForms.DIContainer
- ⬜ プロジェクト作成
- ⬜ `appsettings.json` 接続文字列設定
- ⬜ `ServiceCollection` + `BuildServiceProvider()`
- ⬜ `Program.cs`：DI コンテナ組み立て
- ⬜ `MainForm`：商品一覧（DataGridView）

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
