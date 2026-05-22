using TvmVr2.Api.Common;
using TvmVr2.Api.Enums;

namespace TvmVr2.Api.Requests
{
    /// <summary>
    /// Request for basic center translation.
    /// </summary>
    public sealed class BasicTranslateRequest : EditOperationRequest
    {
        /// <summary>
        /// Creates a basic translate request.
        /// </summary>
        public BasicTranslateRequest()
            : base(MethodKind.BasicTranslate)
        {
        }

        /// <summary>
        /// Edited center index.
        /// </summary>
        public int CenterIndex { get; set; }

        /// <summary>
        /// Target center position.
        /// </summary>
        public Point3Data TargetPosition { get; set; }
    }
}
