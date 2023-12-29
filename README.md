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