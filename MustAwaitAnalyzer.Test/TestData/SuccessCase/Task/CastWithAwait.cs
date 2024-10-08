using System.Threading.Tasks;

namespace SuccessCase
{
    public class Task1Cast
    {
        async Task Task_NoAsync() => await Task.Run(() => {});

        public async Task UseAsync()
        {
            await (Task<int>)Task_NoAsync();
        }
    }
}
