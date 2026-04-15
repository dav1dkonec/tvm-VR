using TvmVr2.Api.Enums;

namespace TvmVr2.Core.Abstractions
{
    public interface IMethodInput
    {
        MethodKind MethodKind { get; }
    }
}
