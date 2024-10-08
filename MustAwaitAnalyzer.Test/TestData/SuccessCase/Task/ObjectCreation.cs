using System.Threading.Tasks;
using System;

public class Foo
{
    Action<object> action = (object obj) =>
    {
    };

    public async Task Hoge()
    {
        await new Task(action, "a");
    }
}
