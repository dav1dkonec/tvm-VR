using TvmVr2.Api.Enums;

namespace TvmVr2.Core.Abstractions
{
    /// <summary>
    /// Input passed to an editing method.
    /// </summary>
    public interface IMethodInput
    {
        /// <summary>
        /// Target editing method.
        /// </summary>
        MethodKind MethodKind { get; }
    }
}
