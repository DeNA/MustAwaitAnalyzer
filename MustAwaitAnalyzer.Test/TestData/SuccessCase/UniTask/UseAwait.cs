using Cysharp.Threading.Tasks;

namespace SuccessCase
{
    public class UseAwait
    {
        async UniTask UniTask_NoAsync() => await UniTask.Run(() => {});

        public async UniTask UseAsync()
        {
            await UniTask_NoAsync();
        }
    }
}
