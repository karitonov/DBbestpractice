# ADO.NET DB操作ベストプラクティス

このディレクトリは、C# で SQLite・PostgreSQL を使った DB 操作のベストプラクティスをまとめたものです。  
非同期版（`Async/`）と同期版（`Sync/`）を同じ構造で実装し、WinMerge で比較したときの差分が最小になるよう設計しています。

```
bestpractice/
├── 01_Standard/               # 標準版: 型付きオブジェクトを返す
│   ├── Async/
│   │   ├── DbSession.cs       # 非同期版（DbSessionAsync）
│   │   └── UsageExample.cs
│   └── Sync/
│       ├── DbSession.cs       # 同期版（DbSessionSync）
│       └── UsageExample.cs
├── 02_DataTable/              # DataTable 版: DataTable / DataRow を返す
│   ├── DataTableExtensions.cs # ToList<T> 拡張メソッド（Sync/Async 共用）
│   ├── Async/
│   │   ├── DbSession.cs       # 非同期版（DbSessionAsync）
│   │   └── UsageExample.cs
│   └── Sync/
│       ├── DbSession.cs       # 同期版（DbSessionSync）
│       └── UsageExample.cs
└── DbParam.cs                 # パラメーターヘルパー（全バージョン共用）
```

---

## 1. DB操作の種類と必要な手順

DB 操作はすべて「接続を開く → SQL を実行する → 接続を閉じる」という共通の骨格を持ちます。  
操作の種類によって「SQL 実行の方法」と「結果の受け取り方」が異なります。

### 1.1. 共通の骨格

```
① 接続を生成する       DbConnection を new する（SqliteConnection / NpgsqlConnection）
② 接続を開く           connection.Open() / OpenAsync()
③ コマンドを生成する   connection.CreateCommand()
④ SQL を設定する       command.CommandText = "SELECT ..."
⑤ パラメーターを設定   command.Parameters.Add(...)  ← SQL インジェクション対策
⑥ SQL を実行する       ExecuteReader / ExecuteNonQuery / ExecuteScalar
⑦ 結果を処理する       操作の種類ごとに異なる（後述）
⑧ リソースを解放する   using / await using で Command・Reader・Connection を Dispose
```

### 1.2. 操作種別ごとの手順

| 操作 | 用途 | 実行メソッド | 戻り値 | 結果の処理 |
|---|---|---|---|---|
| **複数行 SELECT** | 一覧取得 | `ExecuteReader` | `DbDataReader` | `while (reader.Read())` でループ |
| **単一行 SELECT** | 1件取得（存在しない場合あり） | `ExecuteReader` | `DbDataReader` | `reader.Read()` が `true` なら1行読む |
| **スカラー SELECT** | `COUNT(*)` など集計値 | `ExecuteScalar` | `object?` | `DBNull` チェック後にキャスト |
| **INSERT / UPDATE / DELETE** | データ変更 | `ExecuteNonQuery` | `int`（影響行数） | 戻り値で成否や件数を確認 |
| **トランザクション（複数操作）** | 複数の変更を1つにまとめる | 上記の組み合わせ | ― | 全成功でコミット、例外でロールバック |

---

## 2. 手順の詳細図

### 2.1. 複数行 SELECT

```
接続を生成・オープン
    ↓
コマンド生成 → SQL 設定 → パラメーター設定
    ↓
ExecuteReader() → DbDataReader を取得
    ↓
while (reader.Read())          ← 行がある間ループ
    ├─ reader.GetString(0)     ← 列番号でアクセス
    ├─ reader.GetDecimal(1)
    └─ ...オブジェクトに詰める
    ↓
Reader を Dispose（カーソルを閉じる）
    ↓
Connection を Dispose（接続を返却）
```

### 2.2. スカラー SELECT

```
接続を生成・オープン
    ↓
コマンド生成 → SQL 設定 → パラメーター設定
    ↓
ExecuteScalar() → object? を取得
    ↓
null チェック・DBNull チェック → キャスト
    ↓
Connection を Dispose
```

### 2.3. INSERT / UPDATE / DELETE

```
接続を生成・オープン
    ↓
コマンド生成 → SQL 設定 → パラメーター設定
    ↓
ExecuteNonQuery() → 影響行数（int）を取得
    ↓
Connection を Dispose
```

### 2.4. トランザクション（複数操作）

```
接続を生成・オープン
    ↓
BeginTransaction() → DbTransaction を取得
    ↓
┌─ try ──────────────────────────────────────────┐
│  操作1: ExecuteNonQuery / ExecuteReader ...     │
│  操作2: ExecuteNonQuery / ExecuteReader ...     │
│  ...                                           │
│  Commit()                                      │
└────────────────────────────────────────────────┘
    ↓ 例外発生時
┌─ catch ─────────────────────────────────────────┐
│  Rollback()                                     │
│  throw（例外を再スロー）                          │
└─────────────────────────────────────────────────┘
    ↓
Transaction を Dispose
Connection を Dispose
```

---

## 3. 同期・非同期 比較表

非同期版と同期版の違いは、以下の対応関係に集約されます。  
SQL・パラメーター・ロジックは完全に同一です。

### 3.1. クラス・インターフェース

| | 非同期（`Async/`） | 同期（`Sync/`） |
|---|---|---|
| セッションクラス | `DbSession` | `DbSessionSync` |
| 実装インターフェース | `IAsyncDisposable, IDisposable` | `IDisposable` |
| セッション生成 | `await using var session = new DbSession(...)` | `using var session = new DbSessionSync(...)` |

### 3.2. 各操作の対応

| 操作 | 非同期 | 同期 |
|---|---|---|
| 接続オープン | `await session.OpenAsync()` | `session.Open()` |
| 複数行 SELECT | `await session.QueryAsync(...)` | `session.Query(...)` |
| 単一行 SELECT | `await session.QuerySingleOrDefaultAsync(...)` | `session.QuerySingleOrDefault(...)` |
| スカラー SELECT | `await session.ExecuteScalarAsync<T>(...)` | `session.ExecuteScalar<T>(...)` |
| INSERT/UPDATE/DELETE | `await session.ExecuteAsync(...)` | `session.Execute(...)` |
| トランザクション（自動） | `await session.ExecuteInTransactionAsync(async () => {...})` | `session.ExecuteInTransaction(() => {...})` |
| トランザクション開始 | `await session.BeginTransactionAsync(...)` | `session.BeginTransaction(...)` |
| コミット | `await session.CommitAsync()` | `session.Commit()` |
| ロールバック | `await session.RollbackAsync()` | `session.Rollback()` |

### 3.3. 戻り値の型

| 操作 | 非同期 | 同期 |
|---|---|---|
| 複数行 SELECT | `Task<IReadOnlyList<T>>` | `IReadOnlyList<T>` |
| 単一行 SELECT | `Task<T?>` | `T?` |
| スカラー SELECT | `Task<T?>` | `T?` |
| INSERT/UPDATE/DELETE | `Task` | `void` |
| トランザクション（自動） | `async () =>` | `() =>` |

---

## 4. 実装例

### 4.1. 複数行 SELECT

```csharp
// 非同期
public static async Task<IReadOnlyList<Product>> GetFeaturedProducts()
{
    await using var session = new DbSession(CreateConnection());
    await session.OpenAsync();

    return await session.QueryAsync(
        sql: "SELECT Id, Name, UnitPrice FROM Products WHERE IsFeatured = @featured",
        map: r => new Product(r.GetGuid(0), r.GetString(1), r.GetDecimal(2)),
        parameters: DbParam.Of(("@featured", true)));
}

// 同期
public static IReadOnlyList<Product> GetFeaturedProducts()
{
    using var session = new DbSessionSync(CreateConnection());
    session.Open();

    return session.Query(
        sql: "SELECT Id, Name, UnitPrice FROM Products WHERE IsFeatured = @featured",
        map: r => new Product(r.GetGuid(0), r.GetString(1), r.GetDecimal(2)),
        parameters: DbParam.Of(("@featured", true)));
}
```

**ポイント:**
- `map:` で列番号 → オブジェクトの変換を定義する。列番号は SELECT 句の順番と一致させる
- 結果が0件でも例外にならず空リストが返る

---

### 4.2. 単一行 SELECT

```csharp
// 非同期
public static async Task<Product?> FindProduct(Guid id)
{
    await using var session = new DbSession(CreateConnection());
    await session.OpenAsync();

    return await session.QuerySingleOrDefaultAsync(
        sql: "SELECT Id, Name, UnitPrice FROM Products WHERE Id = @id",
        map: r => new Product(r.GetGuid(0), r.GetString(1), r.GetDecimal(2)),
        parameters: DbParam.Of(("@id", id)));
}

// 同期
public static Product? FindProduct(Guid id)
{
    using var session = new DbSessionSync(CreateConnection());
    session.Open();

    return session.QuerySingleOrDefault(
        sql: "SELECT Id, Name, UnitPrice FROM Products WHERE Id = @id",
        map: r => new Product(r.GetGuid(0), r.GetString(1), r.GetDecimal(2)),
        parameters: DbParam.Of(("@id", id)));
}
```

**ポイント:**
- 戻り値は `T?`（null 許容）。「存在しない場合は null」として呼び出し側で判定する
- `Query` との違いは、最初の1行だけ読んで終了する点。WHERE 句で一意に絞っていても `Query` を使うと余計なループが発生する

---

### 4.3. スカラー SELECT（`COUNT(*)` など）

```csharp
// 非同期
public static async Task<int> CountFeaturedProducts()
{
    await using var session = new DbSession(CreateConnection());
    await session.OpenAsync();

    return await session.ExecuteScalarAsync<int>(
        sql: "SELECT COUNT(*) FROM Products WHERE IsFeatured = @featured",
        parameters: DbParam.Of(("@featured", true))) ?? 0;
}

// 同期
public static int CountFeaturedProducts()
{
    using var session = new DbSessionSync(CreateConnection());
    session.Open();

    return session.ExecuteScalar<int>(
        sql: "SELECT COUNT(*) FROM Products WHERE IsFeatured = @featured",
        parameters: DbParam.Of(("@featured", true))) ?? 0;
}
```

**ポイント:**
- `ExecuteScalar` の戻り値は `object?`。DB が `NULL` を返した場合は `DBNull.Value` になるため、そのままキャストすると例外になる。内部で `DBNull` チェックを行い、null 返却にしている
- `?? 0` で null の場合のデフォルト値を指定している

---

### 4.4. INSERT / UPDATE / DELETE

```csharp
// 非同期
public static async Task AddProduct(Product product)
{
    await using var session = new DbSession(CreateConnection());
    await session.OpenAsync();

    await session.ExecuteAsync(
        sql: @"INSERT INTO Products (Id, Name, UnitPrice, IsFeatured)
               VALUES (@id, @name, @price, @featured)",
        parameters: DbParam.Of(
            ("@id",       product.Id),
            ("@name",     product.Name),
            ("@price",    product.UnitPrice),
            ("@featured", false)));
}

// 同期
public static void AddProduct(Product product)
{
    using var session = new DbSessionSync(CreateConnection());
    session.Open();

    session.Execute(
        sql: @"INSERT INTO Products (Id, Name, UnitPrice, IsFeatured)
               VALUES (@id, @name, @price, @featured)",
        parameters: DbParam.Of(
            ("@id",       product.Id),
            ("@name",     product.Name),
            ("@price",    product.UnitPrice),
            ("@featured", false)));
}
```

**ポイント:**
- 戻り値は影響行数（`int`）。必要なければ無視してよい
- 値は必ず `DbParam.Of(...)` 経由で渡す。文字列補間（`$"... {name} ..."`）は **SQL インジェクションの原因になるため絶対に使わない**

---

### 4.5. トランザクション（自動管理）

```csharp
// 非同期
public static async Task PlaceOrder(Guid orderId, Guid productId, int quantity)
{
    await using var session = new DbSession(CreateConnection());
    await session.OpenAsync();

    await session.ExecuteInTransactionAsync(async () =>
    {
        await session.ExecuteAsync(
            sql: "INSERT INTO Orders (Id, ProductId, Quantity) VALUES (@id, @pid, @qty)",
            parameters: DbParam.Of(
                ("@id",  orderId),
                ("@pid", productId),
                ("@qty", quantity)));

        await session.ExecuteAsync(
            sql: "UPDATE Stock SET Quantity = Quantity - @qty WHERE ProductId = @pid",
            parameters: DbParam.Of(
                ("@qty", quantity),
                ("@pid", productId)));
    });
}

// 同期
public static void PlaceOrder(Guid orderId, Guid productId, int quantity)
{
    using var session = new DbSessionSync(CreateConnection());
    session.Open();

    session.ExecuteInTransaction(() =>
    {
        session.Execute(
            sql: "INSERT INTO Orders (Id, ProductId, Quantity) VALUES (@id, @pid, @qty)",
            parameters: DbParam.Of(
                ("@id",  orderId),
                ("@pid", productId),
                ("@qty", quantity)));

        session.Execute(
            sql: "UPDATE Stock SET Quantity = Quantity - @qty WHERE ProductId = @pid",
            parameters: DbParam.Of(
                ("@qty", quantity),
                ("@pid", productId)));
    });
}
```

**ポイント:**
- `ExecuteInTransaction(Async)` が `BeginTransaction` → `Commit` / `Rollback` の定型を隠蔽している
- ラムダ内で例外が発生した場合、**自動で Rollback して例外を再スロー**する。INSERT が成功して UPDATE が失敗したとき、INSERT も取り消される
- 単純な「複数操作をひとまとめ」ならこちらを使う

---

### 4.6. トランザクション（手動制御）

```csharp
// 非同期
public static async Task PlaceOrderManual(Guid orderId, Guid productId, int quantity)
{
    await using var session = new DbSession(CreateConnection());
    await session.OpenAsync();
    await session.BeginTransactionAsync(IsolationLevel.ReadCommitted);
    try
    {
        await session.ExecuteAsync(
            "INSERT INTO Orders (Id, ProductId, Quantity) VALUES (@id, @pid, @qty)",
            DbParam.Of(("@id", orderId), ("@pid", productId), ("@qty", quantity)));

        // コミット前に在庫を確認して判断する
        int remaining = await session.ExecuteScalarAsync<int>(
            "SELECT Quantity FROM Stock WHERE ProductId = @pid",
            DbParam.Of(("@pid", productId))) ?? 0;

        if (remaining < quantity)
            throw new InvalidOperationException("在庫が不足しています。");

        await session.ExecuteAsync(
            "UPDATE Stock SET Quantity = Quantity - @qty WHERE ProductId = @pid",
            DbParam.Of(("@qty", quantity), ("@pid", productId)));

        await session.CommitAsync();
    }
    catch
    {
        await session.RollbackAsync();
        throw;  // 呼び出し元に例外を伝える
    }
}

// 同期（構造は同じ、await なし）
```

**ポイント:**
- コミット前に DB の状態を確認して処理を分岐させたい場合は手動制御を使う
- `catch` の中で必ず `throw` で例外を再スローする。握りつぶすと呼び出し元が失敗を検知できなくなる
- `IsolationLevel` を明示することで、他のトランザクションとの競合をどの程度許容するかを制御できる

---

## 5. DataTable 版（`02_DataTable/`）

### 5.1. DataTable とは

`System.Data.DataTable` は、クエリ結果を**インメモリの表形式**で保持する .NET 標準クラスです。  
行（`DataRow`）と列（`DataColumn`）で構成され、列名や型情報（スキーマ）も保持します。

### 5.2. 01_Standard との違い

| | 01_Standard | 02_DataTable |
|---|---|---|
| 複数行 SELECT の戻り値 | `IReadOnlyList<T>`（型付きオブジェクト） | `DataTable` |
| 単一行 SELECT の戻り値 | `T?` | `DataRow?` |
| 列へのアクセス | `reader.GetString(0)`（列番号） | `row["Name"]`（列名） |
| 型変換 | `map:` 関数で明示 | `(string)row["Name"]`（キャスト） |
| NULL チェック | null 許容型（`string?` など） | `row.IsNull("ColumnName")` |
| メソッド名（複数行） | `Query(Async)` | `QueryDataTable(Async)` |
| メソッド名（単一行） | `QuerySingleOrDefault(Async)` | `QueryDataRow(Async)` |

### 5.3. DataTable を使うべき場面

- **WinForms / WPF の DataGrid にバインドする** — `DataTable` はデータバインディングに直接対応している
- **列名・列数が実行時まで不明な動的クエリ** — `table.Columns` でスキーマを取得できる
- **Excel / CSV にエクスポートする** — ライブラリ（ClosedXML など）が `DataTable` を直接受け付けることが多い
- **レイヤーをまたいでテーブル構造をそのまま渡す** — ドメインオブジェクトへのマッピングが不要な場合

### 5.4. DataTable を使うべきでない場面

- **ドメインロジックで値を扱う** — `(decimal)row["UnitPrice"]` のようなキャストは型安全でなく、列名の typo がランタイムエラーになる
- **大量データの処理** — 全行をメモリに展開するため、数万行以上では 01_Standard の `Query<T>` の方がメモリ効率が良い

### 5.5. SELECT メソッドの変更点

```csharp
// 01_Standard: map 関数で DbDataReader → T に変換する
public async Task<IReadOnlyList<T>> QueryAsync<T>(
    string sql,
    Func<DbDataReader, T> map,          // ← マッピング関数が必要
    Action<DbCommand>? parameters = null)

// 02_DataTable: map 関数が不要。DataTable.Load() が自動でスキーマ・値を読み込む
public async Task<DataTable> QueryDataTableAsync(
    string sql,
    Action<DbCommand>? parameters = null)   // ← マッピング関数が不要
```

`DataTable.Load(IDataReader)` は、`IDataReader` から列定義（型・名前）と全行データを読み取り、  
`DataTable` を自動構築します。列番号や型の指定は不要です。

### 5.6. 非同期版での注意点

`DataTable.Load()` には非同期版（`LoadAsync`）がありません。  
`02_DataTable/Async/DbSession.cs` では以下のように対処しています。

```csharp
// ExecuteReaderAsync() でネットワーク I/O を非同期化し、
// Load() でメモリ展開（同期）する。
await using DbDataReader reader = await cmd.ExecuteReaderAsync();  // ← ここが非同期
var table = new DataTable();
table.Load(reader);                                                 // ← メモリ操作なので同期で問題なし
```

DB からのデータ転送（ネットワーク I/O）は `ExecuteReaderAsync()` で非同期化できています。  
`Load()` の同期処理はメモリ上の操作のため、スレッドをブロックする時間は無視できます。

### 5.7. DataTable の主要操作

```csharp
DataTable table = await session.QueryDataTableAsync(sql);

// 行数・列数
int rows = table.Rows.Count;
int cols = table.Columns.Count;

// 列名でアクセス（object 型なのでキャストが必要）
foreach (DataRow row in table.Rows)
{
    var name  = (string)row["Name"];
    var price = (decimal)row["UnitPrice"];
}

// NULL チェック（IsNull を使う。== DBNull.Value でも可）
var desc = row.IsNull("Description") ? "(なし)" : (string)row["Description"];

// 条件フィルター（SQL の WHERE 相当）
DataRow[] filtered = table.Select("UnitPrice > 1000");

// ソート（DataView 経由）
table.DefaultView.Sort = "UnitPrice DESC";
foreach (DataRowView rowView in table.DefaultView)
    Console.WriteLine(rowView["Name"]);
```

### 5.8. DataTable → 型付きリスト（DataGrid とロジック処理の両立）

DataGrid へのバインドとロジック処理を **1 回のクエリ**で両立したい場合は、`DataTableExtensions.ToList<T>` を使います。

```
DB → QueryDataTableAsync → DataTable（メモリ上）
                               ├─ table.DefaultView → DataGrid バインド
                               └─ table.ToList(...)  → 型付きリストでロジック処理
```

```csharp
DataTable table = await session.QueryDataTableAsync(sql, parameters);

// ① DataGrid へのバインド（DataTable がそのままソースになる）
DataView gridSource = table.DefaultView;
// WinForms: dataGridView.DataSource = gridSource;
// WPF:      dataGrid.ItemsSource   = gridSource;

// ② 型付きリストに変換してロジック処理（メモリ上の変換のみ。DB アクセスなし）
IReadOnlyList<ProductRow> products = table.ToList(row => new ProductRow(
    Id:        (Guid)row["Id"],
    Name:      (string)row["Name"],
    UnitPrice: (decimal)row["UnitPrice"]));

decimal total   = products.Sum(p => p.UnitPrice);
decimal average = products.Average(p => p.UnitPrice);
```

**`DataTableExtensions.ToList<T>` の実装（`02_DataTable/DataTableExtensions.cs`）:**

```csharp
public static IReadOnlyList<T> ToList<T>(
    this DataTable table,
    Func<DataRow, T> map)
{
    var results = new List<T>(table.Rows.Count);
    foreach (DataRow row in table.Rows)
        results.Add(map(row));
    return results;
}
```

**`DataReader` マッパー（01_Standard）との違い:**

| | 01_Standard `Query<T>` | 02_DataTable `ToList<T>` |
|---|---|---|
| マッピングの入力型 | `Func<DbDataReader, T>` | `Func<DataRow, T>` |
| 列へのアクセス | `reader.GetString(0)`（列番号） | `row["Name"]`（列名） |
| DataGrid バインド | 別途 DataTable が必要 | そのまま `DefaultView` を使える |
| DB アクセス後の変換 | DataReader 読み取り中に変換 | DataTable 構築後に変換（2段階） |

`ProductRow` のようなマッピング先モデルは、実際のアプリでは Domain 層や ViewModel に配置します。`DataTableExtensions.cs` の `ProductRow` は学習用サンプルです。

---

## 6. 重要な注意点

### 6.1. Dispose を必ず行う

`DbConnection`・`DbCommand`・`DbDataReader`・`DbTransaction` はすべて `IDisposable` を実装しており、OS レベルのファイルハンドルや DB 接続を保持しています。

```csharp
// NG: using なし → 接続が解放されない
var session = new DbSession(CreateConnection());

// OK: using / await using で確実に解放
await using var session = new DbSession(CreateConnection());
using     var session = new DbSessionSync(CreateConnection());
```

Dispose せずに放置すると:
- SQLite: `sqlite.db` のファイルロックが残り、他のプロセスから「database is locked」エラーが発生することがある
- PostgreSQL: コネクションプールが枯渇し、新しい接続を取れなくなる

### 6.2. SQL インジェクションを防ぐ

```csharp
// NG: 文字列補間は SQL インジェクションの原因になる
cmd.CommandText = $"SELECT * FROM Users WHERE Name = '{name}'";

// OK: パラメーターを使う
cmd.CommandText = "SELECT * FROM Users WHERE Name = @name";
DbParam.Of(("@name", name))
```

悪意ある入力（例: `' OR '1'='1`）を渡されると、文字列補間では SQL を改ざんされる。パラメーターはプレースホルダーに値をバインドするため、SQL 構造を書き換えることができない。

### 6.3. `IDbConnection` ではなく `DbConnection` を使う

```csharp
// IDbConnection: 同期メソッドのみ。非同期メソッドが定義されていない
IDbConnection connection;

// DbConnection: OpenAsync / BeginTransactionAsync など非同期メソッドを持つ
DbConnection connection;   // ← こちらを使う
```

`SqliteConnection` と `NpgsqlConnection` はどちらも `DbConnection` を継承しているため、`DbConnection` を使えば両方に対応できる。

### 6.4. 接続は1操作（1 Unit of Work）ごとに作って解放する

```csharp
// NG: 長期間保持する
private static readonly DbSession _session = new DbSession(CreateConnection());

// OK: 操作のたびに生成・解放する
await using var session = new DbSession(CreateConnection());
```

DB ドライバーが内部でコネクションプールを管理しているため、毎回 `new` しても実際のTCP接続が毎回張られるわけではない。使い終わったら速やかに解放し、プールに返却することで効率よく多数のリクエストをさばける。

---

## 7. 付録：デリゲート・Action・Func

### 7.1. デリゲートとは

「メソッドを変数として扱うための型」です。  
int や string が値を表す型であるように、デリゲートは**関数（メソッド）そのものを表す型**です。

```csharp
// 「int を受け取って string を返すメソッド」を表す型を定義する例
delegate string 変換処理(int value);

// これを変数に代入・呼び出せる
変換処理 fn = n => n.ToString();
string result = fn(42);   // → "42"
```

C# には `Action` / `Func` という汎用デリゲートが標準ライブラリに用意されており、ほとんどのケースでこれを使えば独自デリゲートを定義する必要はありません。

---

### 7.2. Action — 戻り値なし（void）のメソッド

```
Action            引数なし、戻り値なし
Action<T>         引数1つ（T型）、戻り値なし
Action<T1, T2>    引数2つ、戻り値なし
```

```csharp
Action greet         = () => Console.WriteLine("Hello");
Action<string> print = name => Console.WriteLine(name);

greet();          // Hello
print("Alice");   // Alice
```

---

### 7.3. Func — 戻り値ありのメソッド

型パラメーターの**最後の1つが戻り値の型**です。

```
Func<TResult>           引数なし、TResult を返す
Func<T, TResult>        引数1つ（T型）、TResult を返す
Func<T1, T2, TResult>   引数2つ、TResult を返す
```

```csharp
Func<int, string>         toStr  = n => n.ToString();
Func<string, int, string> repeat = (s, n) => string.Concat(Enumerable.Repeat(s, n));

toStr(42);            // → "42"
repeat("ab", 3);      // → "ababab"
```

---

### 7.4. ラムダ式との関係

ラムダ式（`=>` を使う記法）は、Action / Func の**値を書く糖衣構文**です。  
型はコンパイラーが推論するため、明示しなくても使えます。

```csharp
// 型を明示した書き方
Func<int, string> fn = (int n) => n.ToString();

// コンパイラーが型を推論（実際にはこちらをよく使う）
Func<int, string> fn = n => n.ToString();
```

---

### 7.5. このコードでの使われ方

#### 7.5.1. `Action<DbCommand>? parameters`（DbParam の受け渡し）

```csharp
// DbSession のメソッドシグネチャ
public DataTable QueryDataTable(
    string sql,
    Action<DbCommand>? parameters = null)   // ← 「DbCommand を受け取って何か設定する」関数
```

`Action<DbCommand>` は「`DbCommand` を受け取り、戻り値なし」の関数です。  
`DbParam.Of(...)` が返す `Action<DbCommand>` を渡すことで、パラメーターのセットを外部から注入できます。

```csharp
// DbParam.Of の戻り値は Action<DbCommand>
public static Action<DbCommand> Of(params (string Name, object? Value)[] parameters)
    => cmd =>                                    // ← ラムダ式で Action<DbCommand> を生成して返す
    {
        foreach (var (name, value) in parameters)
        {
            DbParameter p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }
    };
```

#### 7.5.2. `Func<DbDataReader, T> map`（01_Standard の行マッピング）

```csharp
// DbSession（01_Standard）のシグネチャ
public IReadOnlyList<T> Query<T>(
    string sql,
    Func<DbDataReader, T> map,    // ← 「DbDataReader を受け取って T を返す」関数
    Action<DbCommand>? parameters = null)
```

`Func<DbDataReader, T>` は「`DbDataReader`（1行分のカーソル）を受け取り、`T` に変換して返す」関数です。

```csharp
// 呼び出し側でラムダ式を渡す
session.Query(
    sql: "SELECT Id, Name, UnitPrice FROM Products",
    map: r => new Product(r.GetGuid(0), r.GetString(1), r.GetDecimal(2)));
//  ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
//  Func<DbDataReader, Product> 型のラムダ式
```

#### 7.5.3. `Func<DataRow, T> map`（02_DataTable の ToList）

```csharp
// DataTableExtensions.ToList
public static IReadOnlyList<T> ToList<T>(
    this DataTable table,
    Func<DataRow, T> map)    // ← 「DataRow を受け取って T を返す」関数
```

```csharp
table.ToList(row => new ProductRow(
    Id:        (Guid)row["Id"],
    Name:      (string)row["Name"],
    UnitPrice: (decimal)row["UnitPrice"]));
// ↑ Func<DataRow, ProductRow> 型のラムダ式
```

#### 7.5.4. `Action work` / `Func<Task> work`（トランザクションのラムダ）

```csharp
// 同期版: 戻り値なし → Action
public void ExecuteInTransaction(Action work, ...)

// 非同期版: Task を返す → Func<Task>
public Task ExecuteInTransactionAsync(Func<Task> work, ...)
```

非同期ラムダ（`async () => { await ...; }`）は `Func<Task>` に代入できます。

```csharp
// 同期
session.ExecuteInTransaction(() => { session.Execute(...); });

// 非同期
await session.ExecuteInTransactionAsync(async () => { await session.ExecuteAsync(...); });
```

---

### 7.6. まとめ

| 型 | 意味 | このコードでの用途 |
|---|---|---|
| `Action<DbCommand>` | DbCommand を受け取り、副作用を起こす | パラメーターのセット（`DbParam.Of`） |
| `Func<DbDataReader, T>` | DbDataReader を T に変換して返す | 行 → オブジェクトのマッピング（01_Standard） |
| `Func<DataRow, T>` | DataRow を T に変換して返す | 行 → オブジェクトのマッピング（02_DataTable） |
| `Action` | 引数なし、戻り値なし | 同期トランザクション内の処理 |
| `Func<Task>` | 引数なし、Task を返す | 非同期トランザクション内の処理 |
