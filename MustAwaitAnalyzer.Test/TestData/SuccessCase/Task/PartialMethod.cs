using System;
using System.Threading.Tasks;

namespace MustAwaitAnalyzer.Test.SuccessCase
{
    interface IStudent
    {
        Task GetName();
    }

    partial class PartialMethod : IStudent
    {
        public virtual partial Task GetName();
    }

    public partial class PartialMethod
    {
        public virtual partial async Task GetName()
        {
            await Task.Run(() => {});
        }
    }
}
