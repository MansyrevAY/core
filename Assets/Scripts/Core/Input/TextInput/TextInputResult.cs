namespace Core.Input.TextInput
{
    public readonly struct TextInputResult
    {
        public bool Submitted { get; }
        public string Text { get; }

        private TextInputResult(bool submitted, string text)
        {
            Submitted = submitted;
            Text = text ?? string.Empty;
        }

        public static TextInputResult Accept(string text) => new(true, text);
        public static TextInputResult Cancelled => new(false, string.Empty);
    }
}
