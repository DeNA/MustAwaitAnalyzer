using System;
using System.Threading.Tasks;

namespace MustAwaitAnalyzer.Test.SuccessCase
{
    public class UsingWithAwait
    {
        public async void M1()
        {
            using (await M2())
            {
            }
        }

        public async Task<IDisposable> M2()
        {
            return await (Task<IDisposable>)Task.Run(() => {});
        }
    }
}
