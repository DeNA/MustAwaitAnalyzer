# MustAwaitAnalyzer

## 概要

MustAwaitAnalyzerは

- [System.Threading.Tasks.Task](https://learn.microsoft.com/ja-jp/dotnet/api/system.threading.tasks.task?view=net-7.0)
- [Cysharp.Threading.Tasks.UniTask](https://github.com/Cysharp/UniTask)

を戻り値に持つメソッドの呼び出しの際にawaitをつけることを強制するアナライザーです。

## Unity プロジェクトでの使用方法

Unity 2021.1.2f1 以上が必要です。
`https://github.com/DeNA/MustAwaitAnalyzer.git?path=com.dena.must-await-analyzer#1.0.0` を Package Manager に追加してください。

## .NET プロジェクトでの使用方法

[NuGet packages](https://www.nuget.org/packages/MustAwaitAnalyzer) からインストールしてください。

## DENA008: Must use await

| Item     | Value              |
|----------|--------------------|
| Category | DenaUnityAnalyzers |
| Enabled  | True               |
| Severity | Error              |
| CodeFix  | False              |


### 解析対象

- 式の評価結果が以下の型であるメソッドの呼び出し、プロパティの参照、インスタンスの作成
- メソッドの呼び出し、プロパティの参照、インスタンスの作成について、それらの評価結果が以下の型のいずれかであるもの
  - [`System.Threading.Tasks.Task`](https://learn.microsoft.com/ja-jp/dotnet/api/system.threading.tasks.task?view=net-7.0)
  - [`Cysharp.Threading.Tasks.UniTask`](https://github.com/Cysharp/UniTask?tab=readme-ov-file#compare-with-standard-task-api)
  - [`System.Threading.Tasks.Task<T>`](https://learn.microsoft.com/ja-jp/dotnet/api/system.threading.tasks.task-1?view=net-8.0)
  - [`Cysharp.Threading.Tasks.UniTask<T>`](https://github.com/Cysharp/UniTask?tab=readme-ov-file#compare-with-standard-task-api)

以下に例を記載します。

```csharp
// bad
namespace BadCase
{
    public class Task1
    {
        Task Task_NoAsync() => Task.Run(() => {}); // DENA008, Must use await" がレポートされます

        public Task UseNoAsync()
        {
            Task_NoAsync(); // DENA008, Must use await" がレポートされます
        }
        
        Action<object> action = (object obj) =>
                                {
                                };
    
        public async Task Hoge()
        {
            new Task(action, "a"); // DENA008, Must use await" がレポートされます
        }
    }
}

// good
namespace GoodCase
{
    public class Task1
    {
        async Task Task_NoAsync() => await Task.Run(() => {}); // DENA008, Must use await" がレポートされません

        public async Task UseNoAsync()
        {
            await Task_NoAsync(); // DENA008, Must use await" がレポートされません
        }
        
        Action<object> action = (object obj) =>
                                {
                                };
    
        public async Task Hoge()
        {
            await new Task(action, "a"); // DENA008, Must use await" がレポートされません
        }
    }
}
```

NOTE1: 以下の型の解析は行いません

- [UniTaskVoid](https://github.com/Cysharp/UniTask#async-void-vs-async-unitaskvoid)
- [ValueTask](https://learn.microsoft.com/ja-jp/dotnet/api/system.threading.tasks.valuetask?view=net-7.0)

NOTE2: メソッドチェーンの最後のメソッド名が Terminate あるいは Forget の場合は、解析は行いません

```csharp
static async UniTask HogeAsync()
{
    await Task.Run(() => {});
}

static void TerminateCase()
{
    HogeAsync(); // await漏れによりDENA008がレポートされます
    HogeAsync().GetAwaiter(); // await漏れによりDENA008がレポートされます
    HogeAsync().GetAwaiter().Terminate(); // 何もレポートされません
    HogeAsync().GetAwaiter().Forget(); // 何もレポートされません
}
```

NOTE3: 解析対象となるメソッドから直接プロパティ参照が行われる場合、メソッドは解析対象とならずプロパティ参照のみが解析対象となります

```csharp
// bad
public async static Task<Task> BadPropertyCase()
{
    var p = new Program();
    p.Example(); // DENA008がレポートされます
    return p.Example().Result; // ResultはTaskを返すためDENA008がレポートされます
}

public async Task<Task> Example()
{
    return await new Task<Task>(null);
}

// good
public async static Task<int> GoodPropertyCase()
{
    var p = new Program();
    p.Example(); // DENA008がレポートされます
    return p.Example().Result; // Resultはintを返すためDENA008はレポートされません
}

public async Task<int> Example()
{
    return await new Task<int>(null);
}
```

NOTE4: 下記のメソッド呼び出し、プロパティ参照、フィールド参照は解析対象外とします

- `System.Threading.Tasks.Task.CompletedTask`
- `System.Threading.Tasks.Task.FromResult`
- `Cysharp.Threading.Tasks.UniTask.CompletedTask`
- `Cysharp.Threading.Tasks.UniTask.FromResult`

NOTE5: Task や UniTask オブジェクトを変数に代入してから await している場合や、リスト（System.Collections.Generic.IList を実装しているクラス）に格納してまとめて await している場合は警告しません。ただし、try-catch の中で await している場合は警告されます。

```csharp
// good
public async Task M1()
{
    var task = Task.Run(() => {}); // DENA008 はレポートされません
    await task;
}
```

```csharp
// good
public async Task M1()
{
    var list = new List<Task>();
    list.Add(Task.Run(() => {})); // DENA008 はレポートされません
    list.Add(Task.Run(() => {})); // DENA008 はレポートされません
    await Task.WhenAll(list);
}
```

### 解析箇所

解析対象が以下の構文内に存在し、awaitが無い場合、DENA008をレポートします。

#### blockの中

```csharp
// bad
namespace BadCase
{
    public class Task1
    {
        Task Task_NoAsync() => Task.Run(() => {}); // DENA008, Must use await" がレポートされます

        public Task UseNoAsync()
        {
            Task_NoAsync(); // DENA008, Must use await" がレポートされます
        }
    }
}

// good
namespace GoodCase
{
    public class Task1
    {
        async Task Task_NoAsync() => await Task.Run(() => {});

        public async Task UseNoAsync()
        {
            await Task_NoAsync();
        }
    }
}
```

#### statementの中

statementの解析箇所をいくつか列挙します。

- using statement

```csharp
// bad

namespace BadCase
{
    public class Task1
    {
        public async void M1() 
        {
            using (M2()) // DENA008, Must use await" がレポートされます。
            {
            }
        }
        
        public async Task<IDisposable> M2()
        {
            return await (Task<IDisposable>)Task.Run(() => {});
        }
    }
}

// good
namespace BadCase
{
    public class Task1
    {
        public async void M1() 
        {
            using (await M2()) // awaitがついているのでDENA008はレポートされません。
            {
            }
        }
        
        public async Task<IDisposable> M2()
        {
            return await (Task<IDisposable>)Task.Run(() => {});
        }
    }
}
```

- do while statement

```csharp
// Bad
public class BadCase {
    public async void M() 
    {
        do 
        {
            Task.Run(() => {}); // DENA008, Must use await" がレポートされます。
        }
        while (true);
        {
        }
    }
}

// Good
public class GoodCase {
    public async void M() 
    {
        do 
        {
            await Task.Run(() => {}); // awaitがついているのでDENA008はレポートされません。
        }
        while (true);
        {
        }
    }
}
```

- if statement

```csharp
// bad case
namespace FailedCase
{
    public class C {
        public async void M() 
        {
            if (M2()) // DENA008がレポートされます
            {
            }
        }

        public async Task<bool> M2()
        {
            return await (Task<bool>)Task.Run(() => {});
        }
    }
}

// good case
namespace SuccessCase
{
    public class C {
        public async void M() 
        {
            if (await M2())
            {
            }
        }

        public async Task<bool> M2()
        {
            return await (Task<bool>)Task.Run(() => {});
        }
    }
}
```

- yield statement

```csharp
// bad
namespace BadCase
{
    public class YieldReturn
    {
        public async IAsyncEnumerable<int> M1()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return (Task<int>)Task.Run(() => 0); // DENA008、CS0029がレポートされます
            }
        }
    }
}

// good
namespace GoodCase
{
    public class YieldReturn
    {
        public async IAsyncEnumerable<int> M1()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return await (Task<int>)Task.Run(() => 0);
            }
        }
    }
}
```

#### lambda

```csharp
// bad
namespace FailedCase
{
    public class Task1
    {
        Func<Task<int>> Example() => (() => Task.Run(() => 0)); // DENA008, Must use await" がレポートされます

        public void UseAsync()
        {
            Example(); // Task<int>型ではなくFunc<Task<int>>型が戻り値のためDENA008はレポートされません
        }
    }
}

// good
namespace SuccessCase
{
    public class Task1
    {
        Func<Task<int>> Example() => (async () => await Task.Run(() => 0));

        public void UseAsync()
        {
            Example(); // Task<int>型ではなくFunc<Task<int>>型が戻り値のためDENA008はレポートされません
        }
    }
}
```

#### PartialMethod内での非同期処理

```csharp
namespace BadCase
{
    interface IStudent
    {
        Task GetName();
    }

    partial class PartialMethod : IStudent
    {
        public virtual partial Task GetName();
    }

    public partial class PartialMethod
    {
        public virtual partial async Task GetName()
        {
            Task.Run(() => {}); // DENA008がレポートされます。
        }
    }
}

namespace GoodCase
{
    interface IStudent
    {
        Task GetName();
    }

    partial class PartialMethod : IStudent
    {
        public virtual partial Task GetName();
    }

    public partial class PartialMethod
    {
        public virtual partial async Task GetName()
        {
            await Task.Run(() => {});
        }
    }
}
```

#### ターゲット型推論

```csharp
// badcase
namespace BadCase
{
    public class TargetTypedNewExpression
    {
        Func<object, int> func = null;


        public async Task<int> Hoge()
        {
            return await new Task<int>(func, "a");
        }

        public async void M()
        {
            Dictionary<string, int> field = new()
            {
                { "item1", Hoge() } // DENA008がレポートされます
            };
        }
    }
}

// GoodCase
namespace GoodCase
{
    public class TargetTypedNewExpression
    {
        Func<object, int> func = null;


        public async Task<int> Hoge()
        {
            return await new Task<int>(func, "a");
        }

        public async void M()
        {
            Dictionary<string, int> field = new()
            {
                { "item1", await Hoge() }
            };
        }
    }
}
```
