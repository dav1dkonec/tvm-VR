namespace CoreTvm
{
    public readonly struct FaceData
    {
        public int V1 { get; }
        public int V2 { get; }
        public int V3 { get; }

        public FaceData(int v1, int v2, int v3)
        {
            V1 = v1;
            V2 = v2;
            V3 = v3;
        }
    }
}
