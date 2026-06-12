# タスクリスト / ロードマップ

## 凡例
- ✅ 完了
- 🔄 作業中
- ⬜ 未着手

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
- ✅ `DbSession`（同期）
- ✅ `DbSessionAsync`（非同期）
- ✅ `DbSessionDataTable`（DataTable 同期）
- ✅ `DbSessionDataTableAsync`（DataTable 非同期）
- ⬜ `DbParam`
- ⬜ `DataTableExtensions`
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
