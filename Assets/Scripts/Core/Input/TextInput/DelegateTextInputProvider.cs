using System;

namespace Core.Input.TextInput
{
    public sealed class DelegateTextInputProvider : ITextInputProvider
    {
        private readonly Func<TextInputRequest, bool> _isAvailable;
        private readonly Func<TextInputRequest, Action<TextInputResult>, IDisposable> _open;

        public DelegateTextInputProvider(Func<TextInputRequest, bool> isAvailable,
            Func<TextInputRequest, Action<TextInputResult>, IDisposable> open)
        {
            _isAvailable = isAvailable ?? throw new ArgumentNullException(nameof(isAvailable));
            _open = open ?? throw new ArgumentNullException(nameof(open));
        }

        public bool IsAvailable(TextInputRequest request) => _isAvailable(request);
        public IDisposable Open(TextInputRequest request, Action<TextInputResult> completed) => _open(request, completed);
    }
}
