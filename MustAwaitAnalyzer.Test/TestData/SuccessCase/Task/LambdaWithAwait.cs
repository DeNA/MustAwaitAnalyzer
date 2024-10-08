using System.Threading.Tasks;
using System;

namespace SuccessCase.TaskCase
{
    public class LambdaWithAwait
    {
        Func<Task<int>> Example() => (async () => await Task.Run(() => 0));

        public void UseAsync()
        {
            Example(); // DENA008 is not reported since the return type is Func<Task<int>>, not Task<int>.
        }

    }
}
