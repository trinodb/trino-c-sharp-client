using System.Threading.Tasks;

namespace Trino.Client.Utils
{
    /// <summary>
    /// Imitates the IAsyncEnumerator interface, which is not available in .NET Standard 2.0.
    /// </summary>
    internal interface IAsyncEnumeratorPlaceholder<T>
    {
        Task<bool> MoveNextAsync();
        T Current { get; }
    }
}
