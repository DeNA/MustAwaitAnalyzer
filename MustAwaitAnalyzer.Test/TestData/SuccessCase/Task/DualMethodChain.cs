using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MyExtensions;
using System;


namespace SuccessCase
{
    public static class DualMethodChain
    {
        static async UniTask HogeAsync()
        {
            await Task.Run(() => {});
        }

        static void TerminateCase()
        {
            HogeAsync().GetAwaiter().Terminate(); 
        }
        
        static void ForgetCase()
        {
            HogeAsync().GetAwaiter().Forget(); 
        }

        static void LambdaForgetCase()
        {
            L1(() => HogeAsync().GetAwaiter().Forget());
        }

        static void LambdaSimpleCase()
        {
            L2(task => { task.GetAwaiter().Forget();});
        }
        
        static void L1(Action a)
        {
            
        }

        static void L2(Action<UniTask> a)
        {
            
        }

    }
}

namespace MyExtensions
{
    public static class MyExtensions
    {
        public static void Terminate(this UniTask.Awaiter uniTask)
        {
        }
        public static void Forget(this UniTask.Awaiter uniTask)
        {
        }
    }
}

