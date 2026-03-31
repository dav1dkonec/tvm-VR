using MathNet.Numerics.LinearAlgebra;

/// <summary>
/// Kabsch algorithm helper methods
/// </summary>
public class Kabsch
{
    /// <summary>
    /// Creates a matrix with the center positions placed as rows
    /// </summary>
    /// <param name="centerIndex">Index of affected center to use</param>
    /// <param name="nearestCenters">Indices of neighbor centers to use</param>
    /// <param name="centers">An array of centers</param>
    /// <returns>Matrix with the center positions placed as rows</returns>
    public static Matrix<float> MatrixFrom(int centerIndex, int[] nearestCenters, System.Numerics.Vector3[] centers)
    {
        var M = Matrix<float>.Build;
        var A = M.Dense(nearestCenters.Length + 1, 3);

        A[0, 0] = centers[centerIndex].X;
        A[0, 1] = centers[centerIndex].Y;
        A[0, 2] = centers[centerIndex].Z;

        for (int i = 0; i < nearestCenters.Length; i++)
        {
            A[i + 1, 0] = centers[nearestCenters[i]].X;
            A[i + 1, 1] = centers[nearestCenters[i]].Y;
            A[i + 1, 2] = centers[nearestCenters[i]].Z;
        }

        return A;
    }

    /// <summary>
    /// Kabsch algorithm
    /// </summary>
    /// <param name="P">Matrix of points P</param>
    /// <param name="Q">Matrix of points Q</param>
    /// <returns>Optimal rotation matrix</returns>
    public static Matrix<float> GetRotation(Matrix<float> P, Matrix<float> Q)
    {
        var M = Matrix<float>.Build;

        var H = P.TransposeThisAndMultiply(Q);
        var svd = H.Svd();
        var V = svd.VT.Transpose();
        var UT = svd.U.Transpose();
        var det = (V * UT).Determinant();
        var d = UnityEngine.Mathf.Sign(det);

        var E = M.Dense(3, 3);
        E[0, 0] = 1;
        E[1, 1] = 1;
        E[2, 2] = d;

        var R = V * E * UT;

        return R;
    }

    /// <summary>
    /// System.Numerics vector to MathNET
    /// </summary>
    /// <param name="translation">Translation vector in System.Numerics</param>
    /// <returns>Translation vector in MathNET</returns>
    public static Vector<float> GetDirection(System.Numerics.Vector3 translation)
    {
        var D = Vector<float>.Build.Dense(3);
        D[0] = translation.X;
        D[1] = translation.Y;
        D[2] = translation.Z;
        return D;
    }

    /// <summary>
    /// Adds a vector to each row of the matrix A
    /// </summary>
    /// <param name="A"><Matrix A</param>
    /// <param name="avg">Vector to add</param>
    public static void Add(Matrix<float> A, Vector<float> avg)
    {
        for (int i = 0; i < A.RowCount; i++)
        {
            for (int j = 0; j < A.ColumnCount; j++)
            {
                A[i, j] += avg[j];
            }
        }
    }

    /// <summary>
    /// Subtracts a vector from each row of the matrix A
    /// </summary>
    /// <param name="A"><Matrix A</param>
    /// <param name="avg">Vector to subtract</param>
    public static void Subtract(Matrix<float> A, Vector<float> avg)
    {
        for (int i = 0; i < A.RowCount; i++)
        {
            for (int j = 0; j < A.ColumnCount; j++)
            {
                A[i, j] -= avg[j];
            }
        }
    }

    /// <summary>
    /// Finds and returns the average of the rows of the matrix A
    /// </summary>
    /// <param name="A">Matrix to take the row average of</param>
    /// <returns>Average of the rows of the input matrix</returns>
    public static Vector<float> Avg(Matrix<float> A)
    {
        var V = Vector<float>.Build;
        var avg = V.Dense(A.ColumnCount);

        for (int i = 0; i < A.RowCount; i++)
        {
            for (int j = 0; j < A.ColumnCount; j++)
            {
                avg[j] += A[i, j];
            }
        }

        for (int j = 0; j < A.ColumnCount; j++)
        {
            avg[j] /= A.RowCount;
        }

        return avg;
    }
}
