# Async.Analyzers
Async.Analyzers is a code analyzer that includes multiple rules for analyzing and fixing synchronization issues in Task-based code.
## Task.Result -> await
```csharp
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var task = Task.FromResult(42);
       -var result = task.Result;
       +var result = await task;
    }
}
```
```csharp
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var task = Task.FromResult(42);
       -var result = task.Result.ToString();
       +var result = (await task).ToString();
    }
}
```
## Task.Wait() -> await
```csharp
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var task = Task.FromResult(42);
       -task.Wait();
       +await task;
    }
}
```
## Task.GetAwaiter().GetResult() -> await
```csharp
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var task = Task.FromResult(42);
       -var result = task.GetAwaiter().GetResult();
       +var result = await task;
    }
}
```
```csharp
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var task = Task.FromResult(42);
       -var result = task.GetAwaiter().GetResult().ToString();
       +var result = (await task).ToString();
    }
}
```
```csharp
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
       ——var result = Task.FromResult(42).GetAwaiter().GetResult();
       +var result = await Task.FromResult(42);
    }
}
```
## Found async method
```csharp
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var obj = new TestClass();
       -var result = obj.Get().ToString();
       +var result = (await obj.GetAsync()).ToString();
    }
    public int Get()
    {
        return 42;
    }
    public Task<int> GetAsync()
    {
        return Task.FromResult(42);
    }
}
```
```csharp
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
       -var result = Get(1);
       +var result = await GetAsync(1);
    }
    public int Get(int i)
    {
        return 42;
    }
    public Task<int> GetAsync(int i)
    {
        return Task.FromResult(42);
    }
}
```
```csharp
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var obj = new TestClass();
       -var result = obj.Get().ToString();
       +var result = (await obj.GetAsync()).ToString();
    }
    public int Get()
    {
        return 42;
    }
    public Task<int> GetAsync()
    {
        return Task.FromResult(42);
    }
}
```
```csharp
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var sc = new TestClass();
       -sc.MyMethod();
       +await sc.MyMethodAsync();
    }
}
public static class SyncClass
{
    public static void MyMethod(this TestClass obj) { }
}

public static class AsyncClass
{
    public static async Task MyMethodAsync(this TestClass obj) { }
}
```
```csharp
using System.Threading.Tasks;
using System.Collections.Generic;
public class TestClass
{
    public async Task MethodAsync()
    {
        var list = new List<int>();
       -list.IndexOf(Get<object>(1),1);
       +list.IndexOf(await GetAsync<object>(1),1);
    }
    public int Get<T>(int i)
    {
        return 42;
    }
    public Task<int> GetAsync<T>(int i)
    {
        return Task.FromResult(42);
    }
}
```
```csharp
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var sc = new TestClass();
       -sc.MyMethod<object>();
       +await sc.MyMethodAsync<object>();
    }
}
public static class SyncClass
{
    public static void MyMethod<T>(this TestClass obj) { }
}

public static class AsyncClass
{
    public static async Task MyMethodAsync<T>(this TestClass obj) { }
}
```
