using Core.Resources;
using System;
using System.Text;
using UnityEngine;

namespace Core.Networking.Fusion
{
    public sealed class LocalPlayerIdentity
    {
        private const string PlayerIdKey = "Core_PlayerId";

        private readonly string _override;

        public LocalPlayerIdentity(string identity = null)
        {
            _override = identity;
        }

        public long Id => Extensions.StableConnectionTokenId(GetConnectionToken());

        internal byte[] GetConnectionToken()
        {
            if (!string.IsNullOrWhiteSpace(_override))
            {
                return Encoding.UTF8.GetBytes(_override);
            }
            var identityText = PlayerPrefs.GetString(PlayerIdKey, "");
            if (string.IsNullOrEmpty(identityText))
            {
                identityText = Guid.NewGuid().ToString();
                PlayerPrefs.SetString(PlayerIdKey, identityText);
                PlayerPrefs.Save();
            }

            var mppmName = GetMppmInstanceName();
            if (ShouldUseEditorProcessIdentity(mppmName))
            {
                mppmName = $"EditorProcess-{System.Diagnostics.Process.GetCurrentProcess().Id}";
            }

            var utf8Encoding = Encoding.UTF8;
            var connectionIdentity = string.IsNullOrEmpty(mppmName) ? identityText : $"{identityText}:{mppmName}";
            return utf8Encoding.GetBytes(connectionIdentity);
        }

        private static string GetMppmInstanceName()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (arguments[index] == "-name")
                {
                    return arguments[index + 1];
                }
            }

            return null;
        }

        private bool ShouldUseEditorProcessIdentity(string mppmName)
        {
            return string.IsNullOrEmpty(mppmName) && Application.isEditor;
        }
    }
}
