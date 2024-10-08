using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cysharp.Threading.DummyTasks;
using Cysharp.Threading.DummyTasks.CompilerServices;

namespace SuccessCase
{
    public class UseDummyTask
    {
        async UniTaskDummy Task_NoAsync() => await UniTaskDummy.CompletedTask;

        public async UniTaskDummy UseAsync()
        {
            await Task_NoAsync();
        }
    }
}

namespace Cysharp.Threading.DummyTasks
{
    [SuppressMessage("", "DENA008")]
    [AsyncMethodBuilder(typeof(AsyncUniTaskDummyMethodBuilder))]
    public class UniTaskDummy : INotifyCompletion
    {
        public static UniTaskDummy CompletedTask => new UniTaskDummy();

        public void OnCompleted(Action continuation)
        {
        }

        public bool IsCompleted { get; private set; }

        public void GetResult()
        {
        }

        public static UniTaskDummy ReturnUniTask()
        {
            return new UniTaskDummy();
        }
    }

    [SuppressMessage("", "DENA008")]
    public static class UniTaskExtension
    {
        public static UniTaskDummy GetAwaiter(this UniTaskDummy uniTask)
        {
            return uniTask;
        }
    }
}

namespace Cysharp.Threading.DummyTasks.CompilerServices
{
    [SuppressMessage("", "DENA008")]
    public struct AsyncUniTaskDummyMethodBuilder
    {
        public static AsyncUniTaskDummyMethodBuilder Create() => new AsyncUniTaskDummyMethodBuilder();

        public UniTaskDummy Task
        {
            [DebuggerHidden, MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return UniTaskDummy.CompletedTask;
            }
        }


        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetException(Exception exception)
        {
        }

        public void SetResult()
        {
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter,
            ref TStateMachine stateMachine)
        {
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter,
            ref TStateMachine stateMachine)
        {
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine)
        {
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }
    }

    public struct AsyncUniTaskDummyMethodBuilder<T>
    {
    }
}
