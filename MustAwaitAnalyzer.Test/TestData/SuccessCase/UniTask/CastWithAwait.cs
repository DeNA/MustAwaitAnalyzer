using Cysharp.Threading.Tasks;

namespace SuccessCase
{
    public class UniTask1Cast
    {
        async UniTask Task_NoAsync() => await UniTask.Run(() => {});

        public async UniTask UseAsync()
        {
            await (UniTask)Task_NoAsync();
        }
    }
}
