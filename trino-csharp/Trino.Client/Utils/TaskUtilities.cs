using System.Threading.Tasks;

namespace Trino.Client
{
    /// <summary>
    /// If hosted within a ASP.NET server then waiting can cause a deadlock, this forces the task creation on the threadpool thread.
    /// </summary>
    internal static class TaskUtilities
    {
        internal static T SafeResult<T>(this Task<T> a)
        {
            return a.ConfigureAwait(false).GetAwaiter().GetResult();
        }
    }
}
