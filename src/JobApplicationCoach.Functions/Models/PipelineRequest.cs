namespace JobApplicationCoach.Functions.Models;

public sealed record PipelineRequest(
    string SessionId,
    string CvContentBase64,
    string CvFileName,
    string JdContentBase64,
    string JdFileName);
