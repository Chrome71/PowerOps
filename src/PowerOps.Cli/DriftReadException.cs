namespace PowerOps.Cli;

/// <summary>A live read of one environment failed. The message must never contain the <c>--env</c> value.</summary>
public sealed class DriftReadException(string message) : Exception(message);