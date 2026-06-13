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
│       ├── IDbSession.cs                  全操作インターフェース（public）
│       └── DbSession.cs                   sync・async・DataTable を統合した実装（internal sealed）
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

### `IDbSession` インターフェースと Unit of Work パターン

```csharp
public interface IDbSession : IAsyncDisposable, IDisposable
{
    void Open();
    IReadOnlyList<T> Query<T>(...);
    void Execute(...);
    void BeginTransaction(...);
    void Commit();
    void Rollback();
    void ExecuteInTransaction(Action work, ...);
    // ...
}
```

`IDbSession` は EF Core の `DbContext` に相当する **Unit of Work（作業単位）** の抽象。
1 つのビジネス処理（複数の SQL）をひとまとめに扱い、コミット or ロールバックをワンセットで管理する。

- Repository のコンストラクターに `IDbSession` を注入することで、テスト時にモックへ差し替えられる
- `internal sealed class DbSession` が実装。外から直接 `new DbSession()` はさせない

### トランザクション分離レベル（`IsolationLevel`）

```csharp
void ExecuteInTransaction(
    Action work,
    IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
```

`IsolationLevel` は「複数のトランザクションが同時に走ったとき、互いにどこまで影響を受けるか」を制御する列挙型（`System.Data.IsolationLevel`）。

| レベル | 防げる問題 | 説明 |
|---|---|---|
| `ReadUncommitted` | なし | コミット前のデータも読める。最速だが危険 |
| **`ReadCommitted`** | ダーティリード | **コミット済みのデータのみ読む。多くの DB のデフォルト** |
| `RepeatableRead` | ダーティリード・反復不能読み取り | トランザクション中に同じ行を 2 回読むと同じ結果になる |
| `Serializable` | 上記すべて + ファントムリード | 完全な分離。最も安全だが最も遅い |

**3 つの問題：**

```
ダーティリード：    他のトランザクションの未コミット変更を読んでしまう
反復不能読み取り：  同じ行を 2 回 SELECT したら値が変わっていた
ファントムリード：  同じ条件で 2 回 SELECT したら行数が変わっていた
```

`= IsolationLevel.ReadCommitted` はデフォルト引数。省略時は `ReadCommitted` が使われ、
明示的に変えたいときだけ指定する：

```csharp
// 省略 → ReadCommitted
session.ExecuteInTransaction(() => { ... });

// 厳密な一貫性が必要な場合
session.ExecuteInTransaction(() => { ... }, IsolationLevel.Serializable);
```

`ReadCommitted` を既定値にしているのは、SQLite・PostgreSQL ともにデフォルトがこのレベルで、
一般的な業務処理（参照 + 更新が混在）に適したバランスだから。

### 拡張メソッド（`DataTableExtensions`）

```csharp
internal static class DataTableExtensions          // ① static クラス
{
    public static IReadOnlyList<T> ToList<T>(
        this DataTable dataTable,                  // ② 第一引数に this
        Func<DataRow, T> map)                      // ③ 通常の引数
    {
        var list = new List<T>(dataTable.Rows.Count);
        foreach (DataRow row in dataTable.Rows)
            list.Add(map(row));
        return list;
    }
}
```

既存クラスを変更せずにメソッドを追加したかのように呼べる構文。3 条件を満たすと拡張メソッドになる：

| 条件 | 内容 |
|---|---|
| `static class` | 拡張メソッドを格納するクラスは static |
| `this T` | 第一引数の `this` でレシーバー型を指定 |
| 呼び出し | インスタンスメソッドと同じ構文で呼べる |

LINQ（`Where` / `Select` / `FirstOrDefault` など）もすべて拡張メソッドとして実装されている。

```csharp
// DataTable のメソッドとして呼べる
var products = table.ToList(row => new Product
{
    Id   = Guid.Parse(row.Field<string>("Id")!),
    Name = row.Field<string>("Name")!,
});
```

`row.Field<T>("列名")` は .NET 標準の拡張メソッド（`System.Data` 名前空間）。
`row["Id"]` より型安全で、nullable 列も `Field<string?>` のように扱える。

### `new List<T>(capacity)` — 初期容量の指定

```csharp
var list = new List<T>(dataTable.Rows.Count);
```

`List<T>` は内部配列が不足すると自動で 2 倍に拡張する（リアロケーション）。
件数が事前にわかる場合は初期容量を渡すことで、リアロケーションのコストをゼロにできる。

### `params` とタプル型（`DbParam`）

```csharp
public static Action<DbCommand> Of(params (string Name, object? Value)[] parameters)
```

**`params`** — 引数を可変長で受け取る修飾子。呼び出し側は配列を意識せず列挙できる：

```csharp
// 配列を明示せず、そのまま並べて渡せる
DbParam.Of(("@Name", "foo"), ("@Id", id.ToString()))
```

**タプル型 `(string Name, object? Value)`** — 名前付きタプル。`Tuple<string, object?>` より
簡潔に書けて、`item.Name` / `item.Value` と名前でアクセスできる。

### `DBNull.Value` — null と DB NULL の変換

```csharp
param.Value = value ?? DBNull.Value;
```

ADO.NET では C# の `null` をそのままパラメーターに渡すと例外になる。
DB の `NULL` を表すには `DBNull.Value`（専用のシングルトン）を使う必要がある。
`??`（null 合体演算子）で `null` のときだけ `DBNull.Value` に差し替えている。

### `cmd.CreateParameter()` — プロバイダー非依存のパラメーター生成

```csharp
var param = cmd.CreateParameter();
param.ParameterName = name;
param.Value = value ?? DBNull.Value;
cmd.Parameters.Add(param);
```

`new SqliteParameter(...)` や `new NpgsqlParameter(...)` を直接書くと、そのプロバイダーに依存したコードになる。
`DbCommand.CreateParameter()` を使うと、コマンドが内部で適切な型のパラメーターを生成するため、
SQLite・PostgreSQL どちらでも同じコードが動く。

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
| ADO.NET | `Repositories/AdoNet/` | 生の SQL。最も低レベルで制御しやすい |
| Dapper | `Repositories/Dapper/` | SQL はそのまま。オブジェクトへのマッピングを自動化 |
| EF Core | `Repositories/EfCore/` | LINQ でクエリ。マイグレーション機能あり |

### 実装の対比

| 項目 | ADO.NET | Dapper | EF Core |
|---|---|---|---|
| 注入するもの | `IDbSession` | `IDbConnectionFactory` | `AppDbContext` |
| SQL | 手書き | 手書き | LINQ（自動生成） |
| マッピング | 手動（`Map` メソッド） | 自動（列名 = プロパティ名） | 自動 |
| パラメーター渡し | `DbParam.Of(("@Id", id))` | 匿名型 `new { Id = id }` | LINQ 引数 |
| トランザクション | `IDbSession.ExecuteInTransaction` | `conn.BeginTransaction()` | `SaveChanges()` で自動 |
| マイグレーション | なし | なし | `dotnet ef migrations add` |
| SQL の制御 | 完全 | 完全 | LINQ 経由（生 SQL も可） |
| 学習コスト | 高 | 低〜中 | 中 |

### コード量の対比（`GetAll` の例）

```csharp
// ADO.NET — 手動マッピングが必要
public IReadOnlyList<Product> GetAll()
{
    using var conn = _factory.CreateConnection();
    conn.Open();
    return conn.Query("SELECT ...", Map).ToList();  // Map は自前メソッド
}

// Dapper — 列名一致で自動マッピング
public IReadOnlyList<Product> GetAll()
{
    using var conn = _factory.CreateConnection();
    conn.Open();
    return conn.Query<Product>("SELECT ...").ToList();
}

// EF Core — SQL 不要
public IReadOnlyList<Product> GetAll()
    => _context.Products.ToList();
```
