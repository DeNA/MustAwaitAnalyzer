using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.Linq;

namespace SuccessCase
{
    public class VariablePatterns
    {
        public async void M1()
        {
            var task = GetTask1(out var i);
            await task;
        }
        
        public async void M2()
        {
            var task1 = GetTask2(out var task2);
            await task1;
            await task2;
        }
        
        public async void M3()
        {
            using var task = Task.Run(() => {});
            await task;
        }
        
        public static Task _task;
        public async void M4()
        {
            _task = Task.Run(() => {});
            await _task;
        }
        
        public async Task M5(List<int> iterator)
        {
            var list = new List<Task>();
            foreach (var i in iterator)
            {
                list.Add(Task.Run(() => {}));
            }
        
            await Task.WhenAll(list);
        }
        
        public async void M6(bool b)
        {
            var tasks = new List<Task>();
            if (b)
            {
                tasks.Add(Task.Run(() => {}));
                await Task.WhenAll(tasks);                
            }
        }
        
        public async void M7()
        {
            await LocalFunc();
            return;
            
            async Task LocalFunc()
            {
                var list = new List<Task>();
                list.Add(Task.Run(() => {}));
                list.Add(Task.Run(() => {}));
                await Task.WhenAll(list);
            }
        }
        
        public async void M8(bool b)
        {
            var task = b ? Task.Run(() => {}) : GetTask1(out var i);
            await task;
        }
        
        public Task GetTask1(out int i)
        {
            i = 0;
            return Task.CompletedTask;
        }
        
        public Task GetTask2(out Task task)
        {
            task = Task.CompletedTask;
            return Task.CompletedTask;
        }
    }
}
