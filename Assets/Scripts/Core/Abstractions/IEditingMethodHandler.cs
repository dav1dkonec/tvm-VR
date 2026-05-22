using TvmVr2.Api.Enums;

namespace TvmVr2.Core.Abstractions
{
    /// <summary>
    /// Handler for one editing method.
    /// </summary>
    public interface IEditingMethodHandler
    {
        /// <summary>
        /// Supported editing method.
        /// </summary>
        MethodKind SupportedMethod { get; }

        /// <summary>
        /// Executes the editing method.
        /// </summary>
        IMethodResult Execute(IMethodInput input);
    }
}
