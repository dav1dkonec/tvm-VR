namespace TvmVr2.Core.Abstractions
{
    public interface IMethodResult
    {
        bool Success { get; }
        string ErrorMessage { get; }
    }
}
