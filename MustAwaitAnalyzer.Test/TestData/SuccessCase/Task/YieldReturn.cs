using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MustAwaitAnalyzer.Test.SuccessCase
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
