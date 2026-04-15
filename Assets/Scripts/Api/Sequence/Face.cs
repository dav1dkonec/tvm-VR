/// <summary>
/// Represents the face of a mesh
/// </summary>
public class Face
{

    /// <summary>
    /// Index of first vertex
    /// </summary>
    public int V1 { get; set; }
    /// <summary>
    /// Index of second vertex
    /// </summary>
    public int V2 { get; set; }
    /// <summary>
    /// Index of third vertex
    /// </summary>
    public int V3 { get; set; }

    /// <summary>
    /// Initializes a face
    /// </summary>
    /// <param name="v1">Index of first vertex</param>
    /// <param name="v2">Index of second vertex</param>
    /// <param name="v3">Index of third vertex</param>
    public Face(int v1, int v2, int v3)
    {
        V1 = v1;
        V2 = v2;
        V3 = v3;
    }
}
