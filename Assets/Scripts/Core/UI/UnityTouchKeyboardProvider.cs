using System;
using System.Threading;
using Core.Input.TextInput;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Core.UI
{
    public sealed class UnityTouchKeyboardProvider : ITextInputProvider
    {
        public bool IsAvailable(TextInputRequest request) => Application.isPlaying && TouchScreenKeyboard.isSupported;

        public IDisposable Open(TextInputRequest request, Action<TextInputResult> completed)
        {
            var keyboard = TouchScreenKeyboard.Open(request.InitialText, TouchScreenKeyboardType.Default,
                false, request.Multiline, request.Password, false, request.Title, request.CharacterLimit);
            if (keyboard == null) return null;
            var session = new KeyboardSession(keyboard, completed);
            session.RunAsync().Forget();
            return session;
        }

        private sealed class KeyboardSession : IDisposable
        {
            private readonly TouchScreenKeyboard _keyboard;
            private readonly Action<TextInputResult> _completed;
            private readonly CancellationTokenSource _cancellation = new();
            private bool _disposed;

            public KeyboardSession(TouchScreenKeyboard keyboard, Action<TextInputResult> completed)
            {
                _keyboard = keyboard;
                _completed = completed;
            }

            public async UniTask RunAsync()
            {
                var token = _cancellation.Token;
                try
                {
                    while (_keyboard.status == TouchScreenKeyboard.Status.Visible)
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                    }

                    if (!token.IsCancellationRequested)
                    {
                        var result = _keyboard.status == TouchScreenKeyboard.Status.Done
                            ? TextInputResult.Accept(_keyboard.text) : TextInputResult.Cancelled;
                        _completed(result);
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                }
                catch (Exception error)
                {
                    _completed(TextInputResult.Cancelled);
                    Debug.LogException(error);
                }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _cancellation.Cancel();
                _keyboard.active = false;
                _cancellation.Dispose();
            }
        }
    }
}
