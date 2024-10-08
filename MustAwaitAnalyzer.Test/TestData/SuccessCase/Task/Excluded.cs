using System.Threading.Tasks;

namespace SuccessCase
{
    public class TaskExcluded
    {
        public Task M1()
        {
            return Task.CompletedTask; // CompletedTask is excluded.
        }

        public Task<int> M2()
        {
            return Task.FromResult(0); // FromResult is excluded.
        }
    }
}
