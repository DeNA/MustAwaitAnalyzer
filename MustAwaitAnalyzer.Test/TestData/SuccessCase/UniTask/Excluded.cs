using Cysharp.Threading.Tasks;

namespace SuccessCase
{
    public class UniTaskExcluded
    {
        public UniTask M1()
        {
            return UniTask.CompletedTask; // CompletedTask is excluded.
        }

        public UniTask<int> M2()
        {
            return UniTask.FromResult(0); // FromResult is excluded.
        }
    }
}
