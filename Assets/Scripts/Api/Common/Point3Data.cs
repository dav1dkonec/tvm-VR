namespace TvmVr2.Api.Common
{
    /// <summary>
    /// Three-dimensional point data used in edit requests.
    /// </summary>
    public struct Point3Data
    {
        /// <summary>
        /// X coordinate.
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Y coordinate.
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Z coordinate.
        /// </summary>
        public float Z { get; set; }

        /// <summary>
        /// Creates point data from coordinates.
        /// </summary>
        public Point3Data(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
