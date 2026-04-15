namespace TvmVr2.Api.Common
{
    public struct Point3Data
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public Point3Data(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
