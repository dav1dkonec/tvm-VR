using TvmVr2.Api.Enums;

namespace TvmVr2.Core.Abstractions
{
    public interface IEditingMethodHandler
    {
        MethodKind SupportedMethod { get; }
        IMethodResult Execute(IMethodInput input);
    }
}
