namespace FirecrackerSharp.Tty.CompletionTracking;

/// <summary>
/// A completion tracker is a mechanism of the TTY client used for detecting when a certain write (primary or intermittent)
/// completes. This is needed first of all in order to achieve thread safety via semaphores used internally, and second
/// of all to keep track of the completion of these writes in order to duly queue up new writes succeeding them.
///
/// A completion tracker can also transform the original text of the write operation to add or remove elements that
/// contribute to the tracking process. A completion tracker used for a primary write (intermittent writes are
/// unsupported for this specific action) can also override whether a given streamed-in line gets sent to the output
/// buffer (if one has been configured) or not.
///
/// The check can either happen in a background thread via an async task, or (as is recommended) reactively by checking
/// every streamed-in line as they come in.
/// </summary>
public interface ICompletionTracker
{
    /// <summary>
    /// The <see cref="CompletionTrackerContext"/> assigned to this tracker for the currently tracked write.
    /// </summary>
    CompletionTrackerContext? Context { get; set; }
    
    /// <summary>
    /// Transform the input text of the write operation according to the tracker's needs.
    /// </summary>
    /// <param name="inputText">The original input text</param>
    /// <returns>The transformed input text</returns>
    public string TransformInput(string inputText);

    /// <summary>
    /// Whether the given line should be sent to the output buffer (if one has been configured).
    /// </summary>
    /// <param name="line">The streamed-in line</param>
    /// <returns>Whether the line should be forwarded to the buffer</returns>
    public bool ShouldCapture(string line);

    /// <summary>
    /// Checks for completion by reacting to the given streamed-in line.
    /// </summary>
    /// <param name="line">The streamed-in line</param>
    /// <returns>Whether to register completion</returns>
    public bool CheckReactively(string line);

    /// <summary>
    /// Returns null if no passive check is queued or an async task to be run in the background to check for completion.
    /// </summary>
    /// <returns>Null or a background task</returns>
    public Task<bool>? CheckPassively();
}