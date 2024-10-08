using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace SuccessCase
{
    public class TaskList
    {
        private Task<int> _task = (Task<int>)Task.CompletedTask;
        private Task<int> P { get; }
        
        public async Task M1()
        {
            var list = new List<Task>();
            list.Add(Task.Run(() => {}));
            list.Add(Task.Run(() => {}));
            await Task.WhenAll(list);
        }
        
        public async Task M2()
        {
            var list = new List<Task>();
            var task1 = Task.Run(() => {});
            var task2 = Task.Run(() => {});
            list.Add(task1);
            list.Add(task2);
            await Task.WhenAll(list);
        }
        
        public async Task M3()
        {
            var t = new TaskList();
            var list = new List<Task>();
            list.Add(t._task);
            await Task.WhenAll(list);
        }
        
        public async Task M4()
        {
            var t = new TaskList();
            var list = new List<Task>();
            list.Add(t.P);
            await Task.WhenAll(list);
        }
        
        public async Task M5()
        {
            var list = new List<Task>();
            var task1 = new Task(() => {});
            list.Add(task1);
            await Task.WhenAll(list);
        }
        
        public async Task M6()
        {
            var task1 = Task.Run(() => {});
            var task2 = Task.Run(() => {});
            await Task.WhenAll(task1, task2);
        }
        
        public async Task M7()
        {
            var list = new List<Task>
            {
                Task.Run(() => {}),
                Task.Run(() => {})
            };
            await Task.WhenAll(list);
        }
        
        public async Task M8()
        {
            var list = new List<Task>();
            list.Add(CreateTask(i => {}));
            list.Add(CreateTask(j => {}));
            await Task.WhenAll(list);
        }
        
        public Task CreateTask(Action<int> action)
        {
            return Task.CompletedTask;
        }

        public async Task M10()
        {
            var list = new List<Task>()
            {
                Task.Run(() => {}),
                TaskFunc(async i => await Task.Run(() => {}))
            };
            await Task.WhenAll(list);
        }

        public Task TaskFunc(Func<int, Task> func)
        {
            return Task.CompletedTask;
        }
        
        public async Task M11()
        {
            await Task.WhenAll(new List<Task>
            {
                Task.Run(() => {}),
                Task.Run(() => {})
            });
        }

    }
    
}
