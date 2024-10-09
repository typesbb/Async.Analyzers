using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using ResultVerify = Microsoft.CodeAnalysis.CSharp.Testing.MSTest.CodeFixVerifier<
    Async.Analyzers.TaskResultAnalyzer,
    Async.Analyzers.TaskResultCodeFixProvider>;
using WaitVerify = Microsoft.CodeAnalysis.CSharp.Testing.MSTest.CodeFixVerifier<
    Async.Analyzers.TaskWaitAnalyzer,
    Async.Analyzers.TaskWaitCodeFixProvider>;
using AwaiterResultVerify = Microsoft.CodeAnalysis.CSharp.Testing.MSTest.CodeFixVerifier<
    Async.Analyzers.TaskAwaiterResultAnalyzer,
    Async.Analyzers.TaskAwaiterResultCodeFixProvider>;
using AsyncInsteadVerify = Microsoft.CodeAnalysis.CSharp.Testing.MSTest.CodeFixVerifier<
    Async.Analyzers.TaskAsyncInsteadAnalyzer,
    Async.Analyzers.TaskAsyncInsteadCodeFixProvider>;

namespace Async.Analyzers
{
    [TestClass]
    public class AwaitAccessAnalyzerTests
    {
        [TestMethod]
        public async Task TestResultUsage()
        {
            var testCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var task = Task.FromResult(42);
        var result = task.Result;
    }
}";

            var fixedCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var task = Task.FromResult(42);
        var result = await task;
    }
}";

            var expectedDiagnostic = ResultVerify.Diagnostic(TaskResultAnalyzer.DiagnosticId)
                .WithLocation(8, 22)
                .WithArguments("Result");

            await ResultVerify.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
        }
        [TestMethod]
        public async Task TestResultUsage2()
        {
            var testCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var task = Task.FromResult(42);
        var result = task.Result.ToString();
    }
}";

            var fixedCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var task = Task.FromResult(42);
        var result = (await task).ToString();
    }
}";

            var expectedDiagnostic = ResultVerify.Diagnostic(TaskResultAnalyzer.DiagnosticId)
                .WithLocation(8, 22)
                .WithArguments("Result");

            await ResultVerify.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
        }
        [TestMethod]
        public async Task TestWaitUsage()
        {
            var testCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var task = Task.FromResult(42);
        task.Wait();
    }
}";

            var fixedCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var task = Task.FromResult(42);
        await task;
    }
}";

            var expectedDiagnostic = WaitVerify.Diagnostic(TaskWaitAnalyzer.DiagnosticId)
                .WithLocation(8, 9)
                .WithArguments("Wait()");

            await WaitVerify.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
        }
        [TestMethod]
        public async Task TestGetAwaiterUsage()
        {
            var testCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var task = Task.FromResult(42);
        var result = task.GetAwaiter().GetResult();
    }
}";

            var fixedCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var task = Task.FromResult(42);
        var result = await task;
    }
}";

            var expectedDiagnostic = AwaiterResultVerify.Diagnostic(TaskAwaiterResultAnalyzer.DiagnosticId)
                .WithLocation(8, 22)
                .WithArguments("GetAwaiter().GetResult()");

            await AwaiterResultVerify.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
        }
        [TestMethod]
        public async Task TestGetAwaiterUsage2()
        {
            var testCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var task = Task.FromResult(42);
        var result = task.GetAwaiter().GetResult().ToString();
    }
}";

            var fixedCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var task = Task.FromResult(42);
        var result = (await task).ToString();
    }
}";

            var expectedDiagnostic = AwaiterResultVerify.Diagnostic(TaskAwaiterResultAnalyzer.DiagnosticId)
                .WithLocation(8, 22)
                .WithArguments("GetAwaiter().GetResult()");

            await AwaiterResultVerify.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
        }
        [TestMethod]
        public async Task TestGetAwaiterUsage3()
        {
            var testCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var result = Task.FromResult(42).GetAwaiter().GetResult();
    }
}";

            var fixedCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var result = await Task.FromResult(42);
    }
}";

            var expectedDiagnostic = AwaiterResultVerify.Diagnostic(TaskAwaiterResultAnalyzer.DiagnosticId)
                .WithLocation(7, 22)
                .WithArguments("GetAwaiter().GetResult()");

            await AwaiterResultVerify.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
        }
        [TestMethod]
        public async Task TestAsyncInsteadUsage()
        {
            var testCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var result = Get(1);
    }
    public int Get(int i)
    {
        return 42;
    }
    public Task<int> GetAsync(int i)
    {
        return Task.FromResult(42);
    }
}";

            var fixedCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var result = await GetAsync(1);
    }
    public int Get(int i)
    {
        return 42;
    }
    public Task<int> GetAsync(int i)
    {
        return Task.FromResult(42);
    }
}";

            var expectedDiagnostic = AsyncInsteadVerify.Diagnostic(TaskAsyncInsteadAnalyzer.DiagnosticId)
                .WithLocation(7, 22)
                .WithArguments("Get", "GetAsync");

            await AsyncInsteadVerify.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
        }
        [TestMethod]
        public async Task TestAsyncInsteadUsage2()
        {
            var testCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var obj = new TestClass();
        var result = obj.Get().ToString();
    }
    public int Get()
    {
        return 42;
    }
    public Task<int> GetAsync()
    {
        return Task.FromResult(42);
    }
}";

            var fixedCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var obj = new TestClass();
        var result = (await obj.GetAsync()).ToString();
    }
    public int Get()
    {
        return 42;
    }
    public Task<int> GetAsync()
    {
        return Task.FromResult(42);
    }
}";

            var expectedDiagnostic = AsyncInsteadVerify.Diagnostic(TaskAsyncInsteadAnalyzer.DiagnosticId)
                .WithLocation(8, 22)
                .WithArguments("Get", "GetAsync");

            await AsyncInsteadVerify.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
        }
        [TestMethod]
        public async Task TestAsyncInsteadUsage3()
        {
            var testCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var sc = new TestClass();
        sc.MyMethod();
    }
}
public static class SyncClass
{
    public static void MyMethod(this TestClass obj) { }
}

public static class AsyncClass
{
    public static async Task MyMethodAsync(this TestClass obj) { }
}";

            var fixedCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var sc = new TestClass();
        await sc.MyMethodAsync();
    }
}
public static class SyncClass
{
    public static void MyMethod(this TestClass obj) { }
}

public static class AsyncClass
{
    public static async Task MyMethodAsync(this TestClass obj) { }
}";

            var expectedDiagnostic = AsyncInsteadVerify.Diagnostic(TaskAsyncInsteadAnalyzer.DiagnosticId)
                .WithLocation(8, 9)
                .WithArguments("MyMethod", "MyMethodAsync");

            await AsyncInsteadVerify.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
        }
        [TestMethod]
        public async Task TestAsyncInsteadUsage4()
        {
            var testCode = @"
using System.Threading.Tasks;
using System.Collections.Generic;
public class TestClass
{
    public async Task MethodAsync()
    {
        var list = new List<int>();
        list.IndexOf(Get<object>(1),1);
    }
    public int Get<T>(int i)
    {
        return 42;
    }
    public Task<int> GetAsync<T>(int i)
    {
        return Task.FromResult(42);
    }
}";

            var fixedCode = @"
using System.Threading.Tasks;
using System.Collections.Generic;
public class TestClass
{
    public async Task MethodAsync()
    {
        var list = new List<int>();
        list.IndexOf(await GetAsync<object>(1),1);
    }
    public int Get<T>(int i)
    {
        return 42;
    }
    public Task<int> GetAsync<T>(int i)
    {
        return Task.FromResult(42);
    }
}";

            var expectedDiagnostic = AsyncInsteadVerify.Diagnostic(TaskAsyncInsteadAnalyzer.DiagnosticId)
                .WithLocation(9, 22)
                .WithArguments("Get", "GetAsync");

            await AsyncInsteadVerify.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
        }
        [TestMethod]
        public async Task TestAsyncInsteadUsage5()
        {
            var testCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var sc = new TestClass();
        sc.MyMethod<object>();
    }
}
public static class SyncClass
{
    public static void MyMethod<T>(this TestClass obj) { }
}

public static class AsyncClass
{
    public static async Task MyMethodAsync<T>(this TestClass obj) { }
}";

            var fixedCode = @"
using System.Threading.Tasks;
public class TestClass
{
    public async Task MethodAsync()
    {
        var sc = new TestClass();
        await sc.MyMethodAsync<object>();
    }
}
public static class SyncClass
{
    public static void MyMethod<T>(this TestClass obj) { }
}

public static class AsyncClass
{
    public static async Task MyMethodAsync<T>(this TestClass obj) { }
}";

            var expectedDiagnostic = AsyncInsteadVerify.Diagnostic(TaskAsyncInsteadAnalyzer.DiagnosticId)
                .WithLocation(8, 9)
                .WithArguments("MyMethod", "MyMethodAsync");

            await AsyncInsteadVerify.VerifyCodeFixAsync(testCode, expectedDiagnostic, fixedCode);
        }
    }
}