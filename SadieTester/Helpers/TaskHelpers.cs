namespace SadieTester.Helpers;

public static class TaskHelpers
{
    public static async Task WithTimeout(Task task, int ms)
    {
        var timeout = Task.Delay(ms);
        var completed = await Task.WhenAny(task, timeout);

        if (completed == task)
        {
            await task;
        }
    }
}