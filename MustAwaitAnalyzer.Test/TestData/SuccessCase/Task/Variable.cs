using System.Threading.Tasks;

namespace SuccessCase
{
    public class Variable
    {
        public async Task M1()
        {
            var task = Task.Run(() => {});
            await task;
        }
    }
}
