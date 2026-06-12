namespace JobApplicationCoach.Functions.Models;

public sealed record PipelineRequest(
    string SessionId,
    byte[] CvContent,
    string CvFileName,
    byte[] JdContent,
    string JdFileName);
