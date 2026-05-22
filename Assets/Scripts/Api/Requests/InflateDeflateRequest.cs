using TvmVr2.Api.Enums;

namespace TvmVr2.Api.Requests
{
    /// <summary>
    /// Request for inflate/deflate editing.
    /// </summary>
    public sealed class InflateDeflateRequest : EditOperationRequest
    {
        /// <summary>
        /// Creates an inflate/deflate request.
        /// </summary>
        public InflateDeflateRequest()
            : base(MethodKind.InflateDeflate)
        {
        }

        /// <summary>
        /// User selected reference center index.
        /// </summary>
        public int SelectedCenterIndex { get; set; } = -1;

        /// <summary>
        /// Deformation strength.
        /// </summary>
        public float Strength { get; set; }

        /// <summary>
        /// Inflate or deflate mode.
        /// </summary>
        public InflateDeflateMode Mode { get; set; }
    }
}
