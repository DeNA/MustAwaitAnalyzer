#pragma warning disable CS1998
using System.Threading.Tasks;

namespace SuccessCase
{
    public class Program
    {
        public async static Task<int> M()
        {
            var p = new Program();
            return p.Example().Result;
        }

        public async Task<int> Example()
        {
            return await new Task<int>(null);
        }
    }
}
