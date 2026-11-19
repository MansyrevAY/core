using System;
using Core.Input.TextInput;
using Core.UI.Windows;
using UnityEditor;
using UnityEngine;

namespace Core.UI.Editor.TextInput
{
    public sealed class EditorTextInputProvider : ITextInputProvider
    {
        public bool IsAvailable(TextInputRequest request) => EditorApplication.isPlaying && !EditorApplication.isCompiling;

        public IDisposable Open(TextInputRequest request, Action<TextInputResult> completed)
        {
            var window = ScriptableObject.CreateInstance<EditorTextInputWindow>();
            window.Begin(request, completed);
            return window;
        }
    }

    [InitializeOnLoad]
    internal static class EditorTextInputInstaller
    {
        private static IDisposable _registration;

        static EditorTextInputInstaller()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
            if (EditorApplication.isPlaying) EditorApplication.update += TryInstall;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.update -= TryInstall;
                EditorApplication.update += TryInstall;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Cleanup();
            }
        }

        private static void TryInstall()
        {
            if (!EditorApplication.isPlaying)
            {
                Cleanup();
                return;
            }

            var windows = UnityEngine.Object.FindFirstObjectByType<WindowsService>();
            if (!windows || windows.Input == null) return;
            _registration = windows.TextInput.Register(new EditorTextInputProvider(), -1000);
            EditorApplication.update -= TryInstall;
        }

        private static void Cleanup()
        {
            EditorApplication.update -= TryInstall;
            _registration?.Dispose();
            _registration = null;
        }
    }
}
