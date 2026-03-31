/// <summary>
/// Interface for objects interested in center selection events
/// </summary>
public interface ICenterSelectionListener
{
    /// <summary>
    /// Receive selection notification
    /// </summary>
    /// <param name="center">Center</param>
    /// <param name="selecting">True on enter, false on exit</param>
    void Notify(CenterUI center, bool selecting);
}
