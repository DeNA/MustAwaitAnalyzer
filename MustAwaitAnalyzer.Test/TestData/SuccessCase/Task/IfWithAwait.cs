using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SuccessCase
{
    public class C {
        public async void M() 
        {
            if (await M2())
            {
            }
        }

        public async Task<bool> M2()
        {
            return await (Task<bool>)Task.Run(() => {});
        }
    }
}

