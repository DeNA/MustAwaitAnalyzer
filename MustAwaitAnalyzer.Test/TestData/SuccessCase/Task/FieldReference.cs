using System.Threading.Tasks;

namespace SuccessCase
{
    public class FieldReference
    {
        private Task<int> _task = (Task<int>)Task.CompletedTask;

        public async Task<int> M1()
        {
            var f = new FieldReference();
            return await f._task;
        }
    }
}
