using System.Collections;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace SuccessCase
{
    public class UniTaskList
    {
        private UniTask<int> _uniTask = UniTask.FromResult(0);
        private UniTask<int> P { get; }
        
        public async UniTask M1()
        {
            var list = new List<UniTask>();
            list.Add(UniTask.Run(() => {}));
            list.Add(UniTask.Run(() => {}));
            await UniTask.WhenAll(list);
        }
        
        public async UniTask M2()
        {
            var list = new List<UniTask>();
            var task1 = UniTask.Run(() => {});
            var task2 = UniTask.Run(() => {});
            list.Add(task1);
            list.Add(task2);
            await UniTask.WhenAll(list);
        }
        
        public async UniTask M3()
        {
            var t = new UniTaskList();
            var list = new List<UniTask>();
            list.Add(t._uniTask);
            await UniTask.WhenAll(list);
        }
        
        public async UniTask M4()
        {
            var t = new UniTaskList();
            var list = new List<UniTask>();
            list.Add(t.P);
            await UniTask.WhenAll(list);
        }
        
        public async UniTask M5()
        {
            var list = new List<UniTask>();
            var task = new UniTask();
            list.Add(task);
            await UniTask.WhenAll(list);
        }
        
        public async UniTask M6()
        {
            var task1 = UniTask.Run(() => {});
            var task2 = UniTask.Run(() => {});
            await UniTask.WhenAll(task1, task2);
        }
        
        public async UniTask M7()
        {
            var list = new List<UniTask>()
            {
                UniTask.Run(() => {}),
                UniTask.Run(() => {})
            };
            await list;
        }

        public IEnumerator M10()
        {
            return ToCoroutine(async () =>
            {
                var list = new List<UniTask>();
                list.Add(UniTask.Run(() => {}));
                list.Add(UniTask.Run(() => {}));
                await UniTask.WhenAll(list);
            });
        }
        
        public static IEnumerator ToCoroutine(Func<UniTask> taskFactory)
        {
            return default;
        }
        
        public async UniTask MX1()
        {
            var list = new List<UniTask>();
            list.Add(UniTask.Run(() => {}));
            list.Add(UniTask.Run(() => {}));
            await list;
        }
        
        public async UniTask MX2()
        {
            var task1 = UniTask.Run(() => {});
            var task2 = UniTask.Run(() => {});
            await (task1, task2);
        }
        
    }
    
}
