namespace TvmVr2.Api.Enums
{
    /// <summary>
    /// Editing method identifier.
    /// </summary>
    public enum MethodKind
    {
        /// <summary>
        /// Basic center translation method.
        /// </summary>
        BasicTranslate = 0,

        /// <summary>
        /// Inflate/deflate volume editing method.
        /// </summary>
        InflateDeflate = 1,

        /// <summary>
        /// Sequence looping method.
        /// </summary>
        LoopSequence = 2
    }
}
