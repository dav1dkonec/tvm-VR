using CoreTvm;

public sealed class EditingSession
{
    public SequenceData Sequence { get; set; }
    public EditingOptions Options { get; set; } = new EditingOptions();
    public int CurrentFrameIndex { get; set; }
    public bool IsDirty { get; set; }
    public EditRequest LastEditRequest { get; set; }
}
