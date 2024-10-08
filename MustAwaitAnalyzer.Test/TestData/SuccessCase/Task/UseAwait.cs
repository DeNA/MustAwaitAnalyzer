using System.Threading.Tasks;

namespace SuccessCase
{
    public class Task1
    {
        async Task Task_NoAsync() => await Task.Run(() => {});

        public async Task UseAsync()
        {
            await Task_NoAsync();
        }
    }
}
