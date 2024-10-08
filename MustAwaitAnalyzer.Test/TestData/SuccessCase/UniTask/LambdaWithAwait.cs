using System.Threading.Tasks;
using System;
using Cysharp.Threading.Tasks;

namespace SuccessCase
{
    public class LambdaWithAwait
    {
        Func<UniTask> Example() => (async () => await new UniTask());

        public void UseAsync()
        {
            Example(); // DENA008 is not reported since the return type is Func<Task<int>>, not Task<int>.
        }
    }
}
