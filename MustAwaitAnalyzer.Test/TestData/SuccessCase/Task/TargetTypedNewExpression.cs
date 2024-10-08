using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MustAwaitAnalyzer.Test.SuccessCase
{
    public class TargetTypedNewExpression
    {
        Func<object, int> func = null;


        public async Task<int> Hoge()
        {
            return await new Task<int>(func, "a");
        }

        public async void M()
        {
            Dictionary<string, int> field = new()
            {
                { "item1", await Hoge() }
            };
        }
    }
}
