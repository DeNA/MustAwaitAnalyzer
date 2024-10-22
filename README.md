# MustAwaitAnalyzer

[![CI](https://github.com/DeNA/MustAwaitAnalyzer/actions/workflows/ci.yml/badge.svg?branch=master&event=push)](https://github.com/DeNA/MustAwaitAnalyzer/actions/workflows/ci.yml)

## Overview

MustAwaitAnalyzer is a roslyn analyzer that enforces the use of `await` when calling methods that return:

- [System.Threading.Tasks.Task](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task?view=net-7.0)
- [Cysharp.Threading.Tasks.UniTask](https://github.com/Cysharp/UniTask)

## Install into Unity Project

Requires Unity 2021.1.2f1 or later.
You can add `https://github.com/DeNA/MustAwaitAnalyzer.git?path=com.dena.must-await-analyzer` to Package Manager.

## DENA008: Must use await

| Item     | Value              |
|--------- |--------------------|
| Category | DenaUnityAnalyzers |
| Enabled  | True               |
| Severity | Error              |
| CodeFix  | False              |

### Analysis Targets

- Method calls, property references, and instance creations whose evaluation result is one of the following types:
    - [`System.Threading.Tasks.Task`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task?view=net-7.0)
    - [`Cysharp.Threading.Tasks.UniTask`](https://github.com/Cysharp/UniTask?tab=readme-ov-file#compare-with-standard-task-api)
    - [`System.Threading.Tasks.Task<T>`](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task-1?view=net-8.0)
    - [`Cysharp.Threading.Tasks.UniTask<T>`](https://github.com/Cysharp/UniTask?tab=readme-ov-file#compare-with-standard-task-api)

Examples are provided below:

```csharp
// bad
namespace BadCase
{
    public class Task1
    {
        Task Task_NoAsync() => Task.Run(() => {}); // DENA008, "Must use await" is reported

        public Task UseNoAsync()
        {
            Task_NoAsync(); // DENA008, "Must use await" is reported
        }
        
        Action<object> action = (object obj) => {};
    
        public async Task Hoge()
        {
            new Task(action, "a"); // DENA008, "Must use await" is reported
        }
    }
}

// good
namespace GoodCase
{
    public class Task1
    {
        async Task Task_NoAsync() => await Task.Run(() => {}); // DENA008, "Must use await" is not reported

        public async Task UseNoAsync()
        {
            await Task_NoAsync(); // DENA008, "Must use await" is not reported
        }
        
        Action<object> action = (object obj) => {};
    
        public async Task Hoge()
        {
            await new Task(action, "a"); // DENA008, "Must use await" is not reported
        }
    }
}
```

NOTE1: The following types are not analyzed:
- [UniTaskVoid](https://github.com/Cysharp/UniTask#async-void-vs-async-unitaskvoid)
- [ValueTask](https://learn.microsoft.com/dotnet/api/system.threading.tasks.valuetask?view=net-7.0)

NOTE2: If the last method in a method chain is named `Terminate` or `Forget`, the analysis does not take place.

```csharp
static async UniTask HogeAsync()
{
    await Task.Run(() => {});
}

static void TerminateCase()
{
    HogeAsync(); // DENA008 is reported due to missing await
    HogeAsync().GetAwaiter(); // DENA008 is reported due to missing await
    HogeAsync().GetAwaiter().Terminate(); // Nothing is reported
    HogeAsync().GetAwaiter().Forget(); // Nothing is reported
}
```

NOTE3: If a method call directly references a property that is an analysis target, the property reference is analyzed instead of the method call.

```csharp
// bad
public async static Task<Task> BadPropertyCase()
{
    var p = new Program();
    p.Example(); // DENA008 is reported
    return p.Example().Result; // Result returns Task, hence DENA008 is reported
}

public async Task<Task> Example()
{
    return await new Task<Task>(null);
}

// good
public async static Task<int> GoodPropertyCase()
{
    var p = new Program();
    p.Example(); // DENA008 is reported
    return p.Example().Result; // Result returns int, hence DENA008 is not reported
}

public async Task<int> Example()
{
    return await new Task<int>(null);
}
```

NOTE4: The following method calls, property references, and field references are excluded from analysis:

- `System.Threading.Tasks.Task.CompletedTask`
- `System.Threading.Tasks.Task.FromResult`
- `Cysharp.Threading.Tasks.UniTask.CompletedTask`
- `Cysharp.Threading.Tasks.UniTask.FromResult`

NOTE5: If Task or UniTask objects are assigned to a variable and then awaited, no warning is issued. Additionally, if Task or UniTask objects are stored in a list (a class that implements System.Collections.Generic.IList) and awaited collectively, no warning is issued either.

```csharp
// good
public async Task M1()
{
    var task = Task.Run(() => {}); // DENA008 is not reported
    await task;
}
```

```csharp
// good
public async Task M1()
{
    var list = new List<Task>();
    list.Add(Task.Run(() => {})); // DENA008 is not reported
    list.Add(Task.Run(() => {})); // DENA008 is not reported
    await Task.WhenAll(list);
}
```

### Analysis Locations

If the target of analysis exists within the following syntax and lacks an `await`, DENA008 will be reported.

#### Within a Block

```csharp
// bad
namespace BadCase
{
    public class Task1
    {
        Task Task_NoAsync() => Task.Run(() => {}); // DENA008, "Must use await" is reported

        public Task UseNoAsync()
        {
            Task_NoAsync(); // DENA008, "Must use await" is reported
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

#### Within a Statement

Examples for various statement types are shown below.

- `using` Statement

```csharp
// bad

namespace BadCase
{
    public class Task1
    {
        public async void M1() 
        {
            using (M2()) // DENA008, "Must use await" is reported
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
namespace GoodCase
{
    public class Task1
    {
        public async void M1() 
        {
            using (await M2()) // DENA008 is not reported
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

- `do-while` Statement

```csharp
// bad
public class BadCase {
    public async void M() 
    {
        do 
        {
            Task.Run(() => {}); // DENA008, "Must use await" is reported.
        }
        while (true);
        {
        }
    }
}

// good
public class GoodCase {
    public async void M() 
    {
        do 
        {
            await Task.Run(() => {}); // DENA008 is not reported.
        }
        while (true);
        {
        }
    }
}
```

- `if` Statement

```csharp
// bad case
namespace FailedCase
{
    public class C {
        public async void M() 
        {
            if (M2()) // DENA008 is reported
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

- `yield` Statement

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
                yield return (Task<int>)Task.Run(() => 0); // DENA008 and CS0029 are reported
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

#### Lambda

```csharp
// bad
namespace FailedCase
{
    public class Task1
    {
        Func<Task<int>> Example() => (() => Task.Run(() => 0)); // DENA008, "Must use await" is reported

        public void UseAsync()
        {
            Example(); // DENA008 is not reported since the return type is Func<Task<int>>, not Task<int>.
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
            Example(); // DENA008 is not reported since the return type is Func<Task<int>>, not Task<int>.
        }
    }
}
```

#### Asynchronous Processing within Partial Methods

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
            Task.Run(() => {}); // DENA008 is reported.
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
            await Task.Run(() => {}); // DENA008, "Must use await" is not reported
        }
    }
}
```

#### Target-typed Inference

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
                { "item1", Hoge() } // DENA008 is reported
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
                { "item1", await Hoge() } // DENA008, "Must use await" is not reported
            };
        }
    }
}
```