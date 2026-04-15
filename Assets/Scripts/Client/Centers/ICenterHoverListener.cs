/// <summary>
/// Interface for objects interested in center hover events
/// </summary>
public interface ICenterHoverListener
{
    /// <summary>
    /// Receive hover notification
    /// </summary>
    /// <param name="center">Center</param>
    /// <param name="hovering">True on enter, false on exit</param>
    void Notify(CenterUI center, bool hovering);
}
