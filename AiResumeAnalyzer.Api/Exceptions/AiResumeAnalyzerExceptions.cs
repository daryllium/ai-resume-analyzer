namespace AiResumeAnalyzer.Api.Exceptions;

public abstract class AiResumeAnalyzerException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class AiModelException(string message, Exception? innerException = null)
    : AiResumeAnalyzerException(message, innerException);

public sealed class ExtractionException(string message, Exception? innerException = null)
    : AiResumeAnalyzerException(message, innerException);
