# CSBestpPactice

.NET 8 における DB アクセスのベストプラクティスを示すテンプレートプロジェクト。
オニオンアーキテクチャをベースに、DI の各種アプローチ・設定ファイルの新旧方式・ADO.NET / Dapper / EF Core の使い分けを網羅する。

---

## ソリューション構成

```
CSBestpPactice.slnx
│
├── CSBestpPactice.Domain            Entity / Repository インターフェース
├── CSBestpPactice.Infrastructure    DbSession / Factory / Repository 実装
├── CSBestpPactice.Service           Service / UseCase
│
├── App.Console                      コンソール + Generic Host + appsettings.json
├── App.WinForms.ManualDI            WinForms + 手動 DI + App.config
├── App.WinForms.DIContainer         WinForms + ServiceCollection（Host なし）
├── App.WinForms.Host                WinForms + Generic Host + appsettings.json
└── App.Wpf                          WPF + Generic Host + appsettings.json
```

---

## アーキテクチャ：オニオンアーキテクチャ

```
┌────────────────────────────────────────┐
│  UI（WinForms / WPF / Console）        │  外側（詳細・変わりやすい）
│  ┌──────────────────────────────────┐  │
│  │  Infrastructure（DB 実装）       │  │  外側（詳細・変わりやすい）
│  │  ┌────────────────────────────┐  │  │
│  │  │  Service（ユースケース）    │  │  │  中間層
│  │  │  ┌──────────────────────┐  │  │  │
│  │  │  │  Domain              │  │  │  │  中心（安定・変わりにくい）
│  │  │  │  Entity / Interface  │  │  │  │
│  │  │  └──────────────────────┘  │  │  │
│  │  └────────────────────────────┘  │  │
│  └──────────────────────────────────┘  │
└────────────────────────────────────────┘
```

**依存方向のルール：依存は常に内側（Domain）に向かう。**

- `Infrastructure` は `Domain` のインターフェースを実装するが、`Domain` は `Infrastructure` を知らない
- `UI` は `Service` / `Domain` のインターフェースしか知らない
- これにより、DB の種類や UI フレームワークを変えても `Domain` は無変更でいられる

---

## 各プロジェクトの役割

### CSBestpPactice.Domain

他プロジェクトへの依存を持たない。NuGet パッケージも不要。

```
Domain/
├── Entities/
│   └── Product.cs
└── Repositories/
    ├── IRepository.cs             汎用 CRUD（同期）
    ├── IRepositoryAsync.cs        汎用 CRUD（非同期）
    ├── IProductRepository.cs      Product 専用操作（同期）
    └── IProductRepositoryAsync.cs Product 専用操作（非同期）
```

### CSBestpPactice.Infrastructure

Domain を参照する。DB プロバイダーの NuGet パッケージはここにだけ入る。

```
Infrastructure/
├── Data/
│   ├── DbParam.cs
│   ├── DataTableExtensions.cs
│   ├── Factories/
│   │   ├── IDbConnectionFactory.cs        接続生成インターフェース（public）
│   │   ├── SqliteConnectionFactory.cs     SQLite 実装（internal sealed）
│   │   └── PostgreSqlConnectionFactory.cs PostgreSQL 実装（internal sealed）
│   └── Sessions/
│       ├── DbSession.cs                   ADO.NET 同期セッション
│       ├── DbSessionAsync.cs              ADO.NET 非同期セッション
│       ├── DbSessionDataTable.cs          DataTable 戻り値版（同期）
│       └── DbSessionDataTableAsync.cs     DataTable 戻り値版（非同期）
└── Repositories/
    ├── AdoNet/                            ADO.NET による実装
    ├── Dapper/                            Dapper による実装
    └── EfCore/                            EF Core による実装
```

---

## テクニック解説

### ジェネリック型制約 `where T : class`

```csharp
public interface IRepository<T> where T : class
```

`T` に渡せる型を「参照型（クラス）のみ」に制限する。

これがないと `T?`（nullable）の意味が型によって変わってしまう：

| T の種類 | `T?` の意味 |
|---|---|
| 参照型（class） | null を返せる（意図通り） |
| 値型（struct） | `Nullable<T>` に変わってしまう（意図と異なる） |

### インターフェースの継承

```csharp
public interface IProductRepository : IRepository<Product>
{
    IReadOnlyList<Product> GetFeaturedProducts();
}
```

基本 CRUD は `IRepository<T>` に一度だけ定義し、エンティティ固有の操作だけ追加する。
インターフェース型のまま渡せる範囲が広がるため、DI と相性が良い：

```csharp
// IProductRepository は IRepository<Product> としても渡せる
void Process(IRepository<Product> repo) { }
IProductRepository repo = ...;
Process(repo); // ✅
```

実装クラスで両方継承する方法もあるが、その場合はインターフェース型レベルでの代替ができない。

### `public` vs `internal`

| 対象 | 修飾子 | 理由 |
|---|---|---|
| Domain のインターフェース | `public` | 他プロジェクトから参照される |
| Infrastructure の実装クラス | `internal sealed` | DI 経由でのみ使わせる。外から直接 `new` させない |

### `sealed`

```csharp
internal sealed class SqliteConnectionFactory : IDbConnectionFactory
```

継承を禁止する修飾子。「これ以上特化させない」という設計意図を示す。
JIT コンパイラが仮想メソッドの最適化をしやすくなる副次効果もある。

### `readonly` フィールド

```csharp
private readonly string connectionString;
```

コンストラクター以外での変更を禁止する。誤って後から書き換えるバグを防ぐ。
「構築後は変わらない値」には必ず付ける。

### 同期版に `Sync` サフィックスを付けない

.NET の慣例（TAP: Task-based Asynchronous Pattern）に従い、非同期版にだけ `Async` を付ける：

```
DbSession         // 同期版（デフォルト）
DbSessionAsync    // 非同期版
```

`File.ReadAllText` / `File.ReadAllTextAsync` と同じルール。

### `DbConnection`（抽象基底クラス）を使う理由

```csharp
public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}
```

`IDbConnection`（インターフェース）ではなく `DbConnection`（抽象クラス）を使う。
`IDbConnection` には `OpenAsync` / `BeginTransactionAsync` などの非同期メソッドが定義されていない。
`DbConnection` を使うことで、SQLite・PostgreSQL どちらのプロバイダーでも同一コードで非同期 DB アクセスが可能になる。

### Factory パターン（`IDbConnectionFactory`）

```
IDbConnectionFactory（インターフェース）
├── SqliteConnectionFactory
└── PostgreSqlConnectionFactory
```

呼び出し側（Repository）は `IDbConnectionFactory.CreateConnection()` を呼ぶだけで、どの DB かを知らずに接続を取得できる。DB 切り替えは DI の登録を変えるだけで済む。

---

## DI アプローチ比較

| サンプル | DI 方式 | 設定ファイル | 特徴 |
|---|---|---|---|
| `App.WinForms.ManualDI` | 手動 DI | `App.config` | 最もシンプル。依存関係が `new` で見える |
| `App.WinForms.DIContainer` | `ServiceCollection` | `appsettings.json` | Host なし。コンテナだけ使う |
| `App.WinForms.Host` | Generic Host | `appsettings.json` | ライフタイム・ロギング・設定を一括管理 |
| `App.Console` | Generic Host | `appsettings.json` | Console の標準的なアプローチ |
| `App.Wpf` | Generic Host | `appsettings.json` | MVVM と組み合わせ |

---

## DB アクセス方式比較

| 方式 | 場所 | 特徴 |
|---|---|---|
| ADO.NET（DbSession） | `Repositories/AdoNet/` | 生の SQL。最も低レベルで制御しやすい |
| Dapper | `Repositories/Dapper/` | SQL はそのまま。オブジェクトへのマッピングを自動化 |
| EF Core | `Repositories/EfCore/` | LINQ でクエリ。マイグレーション機能あり |
