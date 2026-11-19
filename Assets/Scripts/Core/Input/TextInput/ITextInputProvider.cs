using System;

namespace Core.Input.TextInput
{
    public interface ITextInputProvider
    {
        bool IsAvailable(TextInputRequest request);

        // All calls and completion callbacks run on Unity's main thread. Return null when opening fails.
        // Disposing the returned lease must close/cancel the platform keyboard and detach its callbacks.
        IDisposable Open(TextInputRequest request, Action<TextInputResult> completed);
    }
}
