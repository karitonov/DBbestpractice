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
├── App.WinForms.HostDI              WinForms + Generic Host（IHostBuilder） + appsettings.json
├── App.WinForms.HostMinimal         WinForms + Generic Host（HostApplicationBuilder） + appsettings.json
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

### なぜこんなにバリエーションがあるのか：.NET の進化の歴史

`ManualDI` → `DIContainer` → `HostDI` という3段階の構成は、.NET が DI・設定の仕組みを
「個別管理」→「コンテナ化」→「ホスト統合」へと進化させてきた歴史をそのまま縮図にしたもの。

| 年 | バージョン | 主な変更 | このプロジェクトでの対応 |
|---|---|---|---|
| 2002〜2016 | .NET Framework 1.0〜4.8 | 標準DIコンテナなし。`App.config` + `ConfigurationManager`が唯一の設定手段 | `ManualDI`（`App.config`、手動 `new`） |
| 2016 | .NET Core 1.0 | ASP.NET Core専用に `IServiceCollection` / `appsettings.json` を導入（Web限定） | — |
| 2017 | .NET Core 2.0 | DI・設定の仕組みがASP.NET Core外でも単体NuGetパッケージとして使えるよう分離 | `DIContainer`（`ServiceCollection` 単体、Host なし） |
| 2018 | .NET Core 2.1 | **Generic Host**（`IHostBuilder`）登場。Web以外（Console/Worker）でもDI＋設定＋ロギングを統一管理 | `HostDI`（`CreateDefaultBuilder`） |
| 2019 | .NET Core 3.0 | Worker Service テンプレートが正式化、Generic Host がバックグラウンドサービスの標準形に | — |
| 2020 | .NET 5 | .NET Framework／.NET Core／Mono が統合。Generic Host がアプリ種別を問わない標準ホスティングモデルとして定着 | — |
| 2021 | .NET 6 | ASP.NET Core にミニマルホスティングモデル（`WebApplication.CreateBuilder()`）＋トップレベルステートメント登場 | — |
| 2022 | .NET 7 | `Host.CreateApplicationBuilder()` でミニマルホスティングモデルが非Webアプリにも拡大。Native AOT対応も強化 | `HostMinimal`（`CreateApplicationBuilder`） |
| 2023〜 | .NET 8 | Native AOT全面対応。新規 Console/Worker プロジェクトの既定が `HostApplicationBuilder` 系に | — |

変化の軸は大きく2つ：

1. **設定ファイル**：XML（`App.config`、固定セクション）→ JSON（`appsettings.json`、複数ソースを階層的に合成できる `IConfiguration` モデル）
2. **DIコンテナ**：標準搭載なし（自分で `new`、または外部コンテナ）→ ASP.NET Core専用の軽量コンテナ → Web以外にも切り出された Generic Host → ボイラープレートを削った「ミニマルホスティングモデル」

**WinForms 特有の捻れ：** ASP.NET Core や Worker Service は元々「ホスト（起動・終了・DI・設定を司る土台）」を必要とするアプリ形態だった。
一方 WinForms/WPF は `Application.Run(new Form())` という独自のメッセージループを持ち、もともと「ホスト」という概念が存在しない。
`App.WinForms.HostDI` でやっていることは、本来 Web/Worker サービス向けに設計された Generic Host の枠組みを、WinForms の世界に**後付けで持ち込む**作業であり、
`host.Run()`（Web側の待ち受けループ）ではなく `Application.Run(host.Services.GetRequiredService<Form1>())` で WinForms のメッセージループに橋渡しする必要がある。

| サンプル | DI 方式 | 設定ファイル | 特徴 |
|---|---|---|---|
| `App.WinForms.ManualDI` | 手動 DI | `App.config` | 最もシンプル。依存関係が `new` で見える |
| `App.WinForms.DIContainer` | `ServiceCollection` | `appsettings.json` | Host なし。コンテナだけ使う |
| `App.WinForms.HostDI` | Generic Host（`IHostBuilder`） | `appsettings.json` | `CreateDefaultBuilder` + `ConfigureServices` コールバック方式 |
| `App.WinForms.HostMinimal` | Generic Host（`HostApplicationBuilder`） | `appsettings.json` | `CreateApplicationBuilder` + `builder.Services` 直接アクセス方式 |
| `App.Console` | Generic Host | `appsettings.json` | Console の標準的なアプローチ |
| `App.Wpf` | Generic Host | `appsettings.json` | MVVM と組み合わせ |

### Generic Host の2つの組み立て方：`CreateDefaultBuilder` と `CreateApplicationBuilder`

| 項目 | `Host.CreateDefaultBuilder()` | `Host.CreateApplicationBuilder()` |
|---|---|---|
| 戻り値 | `IHostBuilder` | `HostApplicationBuilder` |
| 導入時期 | .NET Core 2.1〜 | .NET 7〜（ミニマルホスティングモデル） |
| サービス登録 | `ConfigureServices((context, services) => {...})` のコールバック内 | `builder.Services.AddSingleton<>()` を直接呼ぶ |
| 設定アクセス | コールバック内の `context.Configuration` | `builder.Configuration` に直接アクセス |
| 実行タイミング | 遅延（`Build()` 時にコールバックがまとめて実行される） | 即時（プロパティを触った瞬間に反映） |
| `IHostBuilder` 実装 | する | しない（独立した型のため、`IHostBuilder` 前提の拡張メソッドが使えない場合がある） |

`App.WinForms.HostDI` では `CreateDefaultBuilder()`（`IHostBuilder` を返す方）を採用し、`App.WinForms.HostMinimal` では `CreateApplicationBuilder()` を採用する。
前者はビルダーパターンで設定をコールバックとして積み重ね、複数の拡張メソッドが `ConfigureServices` を呼び合うような合成・拡張性を重視した従来スタイル。
後者は `ASP.NET Core` の `WebApplication.CreateBuilder()` と同系統で、プロパティに直接アクセスできる分コードは短くなるが、`IHostBuilder` 前提のサードパーティ拡張は使えない。

#### 名前空間の衝突に注意：プロジェクト名を `*.Host` にすると `Host` クラスが隠れる

このプロジェクトは元々 `App.WinForms.Host` という名前だったが、以下のコンパイルエラーが出たため `App.WinForms.HostDI` に改名した。

```csharp
namespace App.WinForms.Host;
// ...
Host.CreateDefaultBuilder() // ← コンパイルエラー（CS0234）
```

C# の名前解決では、`using` ディレクティブより「同じ階層にある名前空間メンバー」が先に解決される。
`App.WinForms.Host` という名前空間自体が `App.WinForms` の子である `Host` として先に見つかってしまい、
`using Microsoft.Extensions.Hosting;` で取り込んだ `Host` クラスより優先されてしまう。
`using` エイリアス（`using Host = Microsoft.Extensions.Hosting.Host;`）でも解決しない（名前空間メンバーの解決が `using` より先に行われるため）。

改名してしまえば（`HostDI` は `Host` と完全に別の識別子なので）`Host.CreateDefaultBuilder()` をそのまま書ける。
改名せずに `*.Host` のままにしたい場合は、呼び出し箇所を完全修飾する対処法もある：

```csharp
using IHost host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
    .UseContentRoot(AppContext.BaseDirectory)
    // ...
    .Build();
```

#### 名前空間エイリアスで「接頭辞付き」の型参照にする

同名クラス（`ProductRepository` は `AdoNet` / `Dapper` / `EfCore` の3つの名前空間にそれぞれ存在する）を
同じファイルで扱いたい、あるいは「どの実装由来か」をコード上で明示したいとき、次のような書き方を試したくなる：

```csharp
using CSBestpPactice.Infrastructure.Repositories; // 親の名前空間を using

services.AddTransient<IProductRepository, EfCore.ProductRepository>(); // ← コンパイルエラー（CS0246）
```

これは**コンパイルできない**。`using` ディレクティブが取り込むのは、その名前空間に**直接定義されている型**だけで、
さらに下位の名前空間（`Repositories` の子である `EfCore`）の名前を自動的に短縮してくれるわけではないため。
（[名前空間の衝突に注意](#名前空間の衝突に注意プロジェクト名をhostにするとhostクラスが隠れる)で説明した「囲んでいる名前空間メンバーの解決」とは別の仕組みで、こちらは `using` では解決しない）

正しくは**名前空間エイリアス**を使う：

```csharp
using EfCore = CSBestpPactice.Infrastructure.Repositories.EfCore;
// ...
services.AddTransient<IProductRepository, EfCore.ProductRepository>(); // OK
```

これで `EfCore` という名前を「`CSBestpPactice.Infrastructure.Repositories.EfCore` の別名」として明示的に登録できる。
`AdoNet.ProductRepository` や `Dapper.ProductRepository` も同じファイルで併用したくなった場合に、
無印の `using ...EfCore;`（型を直接取り込む）では1つの名前空間しか同時に持ち込めないが、
エイリアス方式なら複数の実装を `AdoNet.ProductRepository` / `Dapper.ProductRepository` / `EfCore.ProductRepository` と
書き分けられる。

### 設定ファイル：`App.config` と `appsettings.json` の違い

| 項目 | `App.config` | `appsettings.json` |
|---|---|---|
| 形式 | XML | JSON |
| 主な対象 | .NET Framework（〜4.8） | .NET Core / .NET 5〜9 |
| 配置・実体 | ビルド後 `<アプリ名>.exe.config` にコピーされる | そのまま出力ディレクトリにコピーされる |
| 読み込み API | `ConfigurationManager`（`System.Configuration`） | `IConfiguration` / `ConfigurationBuilder`（`Microsoft.Extensions.Configuration`） |
| 接続文字列 | `<connectionStrings>` 専用セクション。`ConfigurationManager.ConnectionStrings["名前"].ConnectionString` | 専用構文はなく `"ConnectionStrings": { "名前": "..." }` を慣習として使う |
| 階層構造 | 基本フラット（`appSettings` / `connectionStrings` などセクション単位） | JSON なので任意の階層を自然に表現できる |
| 型付け／バインディング | 文字列キー・値が基本（`ConfigurationManager.AppSettings["Key"]`） | `IConfiguration.Bind()` / `IOptions<T>` で POCO に直接マッピング可能 |
| 環境別設定 | 標準機能なし（Web.config変換などが別途必要） | `appsettings.{Environment}.json` を環境変数で自動的に重ね合わせる仕組みが標準搭載 |
| 設定ソースの統合 | ファイル単体が基本 | JSON・環境変数・コマンドライン引数・Secret Manager・Key Vault などを `ConfigurationBuilder` で重ね合わせ可能 |
| 設計思想 | アプリ単位の静的な構成ファイル（.NET Framework 前提） | 複数ソースを階層的に合成する「設定プロバイダー」モデル |

`App.WinForms.ManualDI` は `App.config`、それ以外のサンプルは `appsettings.json` を使っており、
新旧2方式の設定ファイルを並べて比較できる構成にしている。

### ManualDI と DIContainer の組み立て方の違い

```csharp
// ManualDI（Program.cs）— その場で new して、結果（インスタンス）を次に渡す
var factory    = new SqliteConnectionFactory(connectionString);
var session    = new DbSession(factory.CreateConnection());
var repository = new ProductRepository(session);
var service    = new ProductService(repository);
Application.Run(new Form1(service, tableService));

// DIContainer（Program.cs）— 「作り方」（ラムダ式）だけを登録する
services.AddSingleton<IDbConnectionFactory>(_ =>
    new SqliteConnectionFactory(configuration.GetConnectionString("SQLite")!));
services.AddSingleton<IDbSession>(sp =>
    new DbSession(sp.GetRequiredService<IDbConnectionFactory>().CreateConnection()));
services.AddTransient<IProductRepository, ProductRepository>();
services.AddTransient<IProductService, ProductService>();
services.AddSingleton<Form1>();

using var provider = services.BuildServiceProvider();
Application.Run(provider.GetRequiredService<Form1>());
```

ManualDI は呼び出し側が `new` の順序を1行ずつ手で管理する（`factory` → `session` → `repository` → `service` → `Form1`）。

DIContainer は `AddSingleton`/`AddTransient` の**登録時点では何も `new` していない**。
登録しているのは「`IDbConnectionFactory` が必要になったらこのラムダを実行する」という**作り方（ファクトリー）**だけ。
実際に `new` が呼ばれる順序は、`provider.GetRequiredService<Form1>()` を呼んだ瞬間に、コンテナが `Form1` のコンストラクター引数 → その引数のコンストラクター引数 …と依存関係を逆向きにたどって自動的に決定する（登録した順序とは無関係）。

| 観点 | ManualDI | DIContainer |
|---|---|---|
| `new` のタイミング | コードを書いた時点の順序通り、即時 | `GetRequiredService` を呼んだ瞬間（遅延実行） |
| 順序の決定者 | 開発者が手で並べる | コンテナが依存関係グラフから自動決定 |
| 登録の単位 | インスタンスそのもの（変数） | 「作り方」（型 または ラムダ） |
| 依存の差し替え | コードを書き換える | 登録（`Add~`）を変えるだけ |

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

### SQLite の動的型付けと Dapper の型ハンドラー（`SqlMapper.TypeHandler<T>`）

SQLite は列に型を強制しない（動的型付け）。同じ列でも行ごとに格納形式（ストレージクラス）が変わることがある：

| 列 | 行によって変わりうる格納形式 |
|---|---|
| `Id`（`Guid`） | `TEXT`（文字列）／`BLOB`（`byte[]`） |
| `UnitPrice`（`decimal`） | `INTEGER`（`Int64`、小数部なし）／`REAL`（`double`、小数部あり） |

ADO.NET（`AdoNet/ProductRepository`）が問題にならないのは、`reader.GetDecimal(...)` のようなメソッドが
ドライバー側で格納形式を問わず指定した型に変換してくれるため。

一方 Dapper（`conn.Query<Product>(sql)`）は、結果セットの**最初の行の型情報だけ**を見てその列専用の高速な変換コードを1度だけ生成する。
最初の行が `REAL`（`double`）だった場合、後続の行が `INTEGER`（`Int64`）だと型変換に失敗し、次のような例外になる：

```
InvalidCastException: Unable to cast object of type 'System.Int64' to type 'System.Double'
```

対策として、`Dapper.SqlMapper.TypeHandler<T>` を継承した**型ハンドラー**を自作し、読み取り・書き込みの変換ロジックを差し替える：

```csharp
public sealed class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    // 読み取り（DB → C#）：BLOB（byte[]）でも TEXT（string）でも Guid に変換
    public override Guid Parse(object value)
        => value is byte[] bytes ? new Guid(bytes) : Guid.Parse(value.ToString()!);

    // 書き込み（C# → DB）：常に文字列として保存
    public override void SetValue(IDbDataParameter parameter, Guid value)
        => parameter.Value = value.ToString();
}

public sealed class DecimalTypeHandler : SqlMapper.TypeHandler<decimal>
{
    // Convert.ToDecimal(object) は Int64 / double どちらでも decimal に変換できる
    public override decimal Parse(object value)
        => Convert.ToDecimal(value);

    public override void SetValue(IDbDataParameter parameter, decimal value)
        => parameter.Value = value;
}
```

型ハンドラーは型ごとにプロセス全体で1つだけ有効（グローバル登録）。アプリ起動時、**最初に Dapper のクエリを実行する前**に1回だけ登録すればよい：

```csharp
// Program.cs の DI コンテナ構築より前
SqlMapper.AddTypeHandler(new GuidTypeHandler());
SqlMapper.AddTypeHandler(new DecimalTypeHandler());
```

登録後は `Repository` 側で `entity.Id.ToString()` のような手動変換が不要になり、`Id = entity.Id` とそのまま渡せる
（`SetValue` が変換を担うため）。

### EF Core での同じ問題：`HasConversion<string>()` の落とし穴

`HostDI`（EF Core ルート）でも同じ BLOB/TEXT 混在の影響を受けた。`AppDbContext.OnModelCreating` で
`entity.Property(p => p.Id).HasConversion<string>();` と明示すると、EF Core は「常に文字列として読む」コードを
生成してしまい、`Id` が `BLOB` 格納の行（過去に ADO.NET 経由で書き込まれた行）に対して
`System.FormatException: Unrecognized Guid format.` が発生した（生バイト列を文字列としてパースしようとして失敗）。

```csharp
// NG：常に GetString() 相当のコードが生成され、BLOB 行で例外になる
entity.Property(p => p.Id).HasConversion<string>();

// OK：変換指定を省略し、Microsoft.Data.Sqlite 標準の Guid 読み取りに任せる
entity.HasKey(p => p.Id);
```

`Microsoft.Data.Sqlite` は `Guid` 列を読むとき、列の実際のストレージクラス（`BLOB`/`TEXT`）を見て
自動的に読み取り方法を振り分ける機能を標準で持っている。Dapper や ADO.NET の素のメソッドにはこの振り分けがないため
自分で `ReadGuid` ヘルパーや `TypeHandler` を書く必要があったが、EF Core + Microsoft.Data.Sqlite の組み合わせでは
**変換を明示しない方がむしろ安全**という結果になった。「型変換を明示的に書けば安全」とは限らない一例。

### コラム：GUID と UUID の違い

結論：**仕様としては同じもの**。`UUID`（Universally Unique Identifier）は IETF が定めた標準の名称（RFC 4122 / 現在は RFC 9562）で、
`GUID`（Globally Unique Identifier）は Microsoft がほぼ同じ仕組みを採用したときに使った呼び名。
128bit（16バイト）の値であること、`8-4-4-4-12` 桁の16進数文字列で表すこと（例：`550e8400-e29b-41d4-a716-446655440000`）は共通。

実務上ハマりやすい違いは1点だけ：**バイト配列に変換したときのバイト順（エンディアン）**。

| | 先頭3グループ（8桁-4桁-4桁） | 末尾2グループ（4桁-12桁） |
|---|---|---|
| .NET の `Guid.ToByteArray()` | リトルエンディアン | ビッグエンディアン（バイト列のまま） |
| RFC 準拠の UUID バイト表現 | ビッグエンディアン | ビッグエンディアン（バイト列のまま） |

文字列表現（`ToString()`）は同じでも、`byte[]` に変換した瞬間に先頭3グループのバイト順が変わる。
今回の `HostDI` の調査で見えたとおり、この一致したプロジェクトの中でも「`Id` を `BLOB` で保存した行」と
「`TEXT` で保存した行」が混在しただけで型ハンドラーの自作が必要になった。もし将来 SQL Server（`uniqueidentifier` は
.NET と同じリトルエンディアン格納）と PostgreSQL（`uuid` 型は RFC 準拠のビッグエンディアン格納）の間でバイト列を
直接やり取りするような設計になった場合、同じ論理値でもバイト順の違いでソート順や比較結果がズレる落とし穴になるので
注意（本プロジェクトでは文字列変換を経由しているため対象外）。

なお `Guid.NewGuid()` が生成する値は、ビット構成上 RFC のバージョン4（ランダム生成）UUID と互換性がある。

---

## DataTable ルート

### DataAdapter の歴史的経緯とライブラリ差異

DataAdapter（`DbDataAdapter` を継承した `XxxDataAdapter` クラス）は .NET Framework 時代の WinForms 標準パターンだった。
DataGridView + DataAdapter + DataSet/DataTable を組み合わせ、`Fill` で取得・`Update` で書き戻す構成が広く使われた。

しかし、.NET Core 以降に SQLite を扱うライブラリが分裂し、DataAdapter の有無が異なる：

| ライブラリ | 主な対象 | `XxxDataAdapter` | 設計思想 |
|---|---|---|---|
| `System.Data.SQLite` | .NET Framework 2.0〜4.8 | ✅ `SQLiteDataAdapter` あり | ADO.NET を完全実装した旧来ライブラリ |
| `Microsoft.Data.Sqlite` | .NET Core〜 / .NET 5〜9 | ❌ **なし** | 軽量設計。重量級 ADO.NET パターンは意図的に省略 |
| `Npgsql` | .NET Framework〜 / .NET 5〜9 | ✅ `NpgsqlDataAdapter` あり | 旧来から継続開発。フル ADO.NET 実装を維持 |

このプロジェクトは `net8.0` + `Microsoft.Data.Sqlite` を使用しているため `SqliteDataAdapter` は存在しない。
そのため DataAdapter パターンのコアロジック（RowState による SQL 振り分け）を自前で実装している。

`ProductTableRepository` は DataTable の取得（Fill 相当）と書き戻し（Update 相当）の両方を担う。
接続管理は `IDbSession` に委譲し、DataAdapter パターンのコアである「RowState に基づく SQL 自動判定」を自前で実装する。

```
GetAll()
  IDbSession.QueryDataTable(sql)
    → DbDataReader → DataTable（全列 object 型）
    → Normalize（BLOB→TEXT、IsFeatured→bool）
    → AcceptChanges()（全行 RowState = Unchanged）
    → DataGridView 表示

Update(table)
  RowState を見て SQL を振り分け → IDbSession.ExecuteInTransaction
    → AcceptChanges()（書き戻し完了）
```

### エンティティルート（`IProductService`）とのフロー比較

`App.WinForms.ManualDI` には `IProductService` 経由のエンティティルートと、上記の DataTable ルートが並列で存在する。

```
エンティティルート
  btnAdd_Click
    ProductEditForm.ShowDialog() → Product を生成
    _service.Register(product)
      ProductService.Register → _repository.Add(product)
        AdoNet.ProductRepository.Add
          IDbSession.Execute(INSERT, entity の各プロパティを @パラメーターに展開)
  Reload()
    _service.GetAll()
      IDbSession.Query(sql, Map)
        DbDataReader → Map(reader) で1行ずつ Product にマッピング（手書き）
        → List<Product>
    dgvProducts.DataSource = list

DataTable ルート
  （グリッド上で直接セル編集 → RowState が自動で Added/Modified/Deleted に変化）
  btnUpdate_Click
    table = dgvProductsTable.DataSource（既に DataTable）
    _tableRepository.Update(table)
      GetChanges() で変更行抽出 → RowState で振り分け → InsertRow/UpdateRow/DeleteRow
      AcceptChanges()
  Reload()
    _tableRepository.GetAll()
      IDbSession.QueryDataTable(sql)
        DbDataReader → DataTable（全列 object 型、エンティティへのマッピングなし）
      Normalize（BLOB→TEXT、IsFeatured→bool、AcceptChanges）
    dgvProductsTable.DataSource = table
```

| 観点 | エンティティルート | DataTable ルート |
|---|---|---|
| データの型 | `Product`（強い型） | `DataTable`（列はすべて `object`） |
| DB → アプリ間のマッピング | `Map(DbDataReader)` で手動マッピング | マッピングなし（生の列値のまま） |
| 変更の検知 | なし（ボタンごとに明示的に呼び分け） | `RowState` が編集を自動追跡 |
| 書き戻しの単位 | 1操作 = 1エンティティ = 1 SQL | 1回の保存 = 複数行の変更をまとめて1トランザクション |
| UI のデータバインド | `List<Product>`（読み取り専用表示 + 別ダイアログで編集） | `DataTable`（グリッド直接編集） |

### RowState の仕組み

DataTable は行ごとに「変更前の値（Original）」と「変更後の値（Current）」を保持する。

```
GetAll() 直後
  行A: RowState = Unchanged
  行B: RowState = Unchanged

DataGridView で編集
  行A を変更 → RowState = Modified（Original = 変更前、Current = 変更後）
  行C を追加 → RowState = Added  （Current のみ）
  行B を削除 → RowState = Deleted（Original のみ残る）

Update(table) 実行
  Added    → InsertRow：Current 値で INSERT
  Modified → UpdateRow：Current 値で SET、Original["Id"] で WHERE
  Deleted  → DeleteRow：Original["Id"] で WHERE
  Unchanged→ スキップ
```

**`DataRowVersion.Original` が必要な理由：**
Modified/Deleted 行の WHERE 条件には「変更前の Id」が必要。
Deleted 行は `row["Id"]`（Current）にアクセスすると例外になるため、`DataRowVersion.Original` が必須。

### `AcceptChanges()` を Normalize 内で呼ぶ理由

`BuildDataTable`（DbSession）は `table.Rows.Add(row)` で行を追加するため、全行が `RowState = Added` の状態で返る。
Normalize の末尾で `AcceptChanges()` を呼ぶことで全行を `Unchanged` にリセットし、
その後のユーザー操作だけが Modified/Added/Deleted として記録される。
