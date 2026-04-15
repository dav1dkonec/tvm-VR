namespace CoreTvm
{
    // Minimalni implementation for wiring the new architecture
    // into runtime without changing deformation behavior yet.
    public sealed class PassthroughEditingCore : IEditingCoreApi
    {
        public EditResult ApplyEdit(SequenceData input, EditRequest request, EditingOptions options)
        {
            return new EditResult
            {
                Sequence = input,
                AffectedFrames = new[] { request.FrameIndex }
            };
        }

        public SequenceData RebuildSurface(SequenceData input, EditingOptions options)
        {
            return input;
        }
    }
}
