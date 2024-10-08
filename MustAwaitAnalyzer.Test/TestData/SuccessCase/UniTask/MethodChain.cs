using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Dummy;


namespace SuccessCase
{
    public static class MethodChain
    {
        static async UniTask HogeAsync()
        {
            await Task.Run(() => {});
        }

        static void TerminateCase()
        {
            HogeAsync().Terminate();
        }

        static async void ForgetCase()
        {
            HogeAsync().Forget();
            await HogeAsync().Forget(1).Preserve();
        }
        
    }
}

namespace Dummy
{
    public static class UniTaskExtension
    {
        public static void Terminate(this UniTask task)
        {
        }

        static async UniTask UniTask_NoAsync() => await UniTask.CompletedTask;

        public static async UniTask Forget(this UniTask task, int x)
        {
            await UniTask_NoAsync();
        }
    }
}
