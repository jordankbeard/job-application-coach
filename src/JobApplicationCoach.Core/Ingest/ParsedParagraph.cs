namespace JobApplicationCoach.Core.Ingest;

public sealed record ParsedParagraph(
    string Content,
    ParagraphRole Role,
    int SequenceIndex);

public enum ParagraphRole
{
    SectionHeading,
    Body,
    Unknown
}
