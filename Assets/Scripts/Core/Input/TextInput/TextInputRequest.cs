using System;

namespace Core.Input.TextInput
{
    public readonly struct TextInputRequest
    {
        public string Title { get; }
        public string InitialText { get; }
        public int CharacterLimit { get; }
        public bool Multiline { get; }
        public bool Password { get; }

        public TextInputRequest(string title, string initialText, int characterLimit = 0, bool multiline = false, bool password = false)
        {
            Title = title ?? string.Empty;
            InitialText = initialText ?? string.Empty;
            CharacterLimit = Math.Max(0, characterLimit);
            Multiline = multiline;
            Password = password;
        }
    }
}
