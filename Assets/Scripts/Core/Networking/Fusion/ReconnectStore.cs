using Core.Resources;
using System;
using Core.Networking.Core;
using UnityEngine;

namespace Core.Networking.Fusion
{
    public sealed class ReconnectStore
    {
        private const string SavedSessionNameKey = "Core_SavedSessionName";
        private const string SavedSessionTimeKey = "Core_SavedSessionTime";
        private const string SavedSessionRegionKey = "Core_SavedSessionRegion";
        private const string SavedSessionVersionKey = "Core_SavedSessionVersion";
        private readonly TimeSpan _savedSessionTimeToLive;
        private bool _remember = true;
        public SessionLocator Current { get; private set; }
        public ReconnectStore(TimeSpan timeToLive) => _savedSessionTimeToLive = timeToLive;

        internal void Restore()
        {
            if (HasSavedSession)
            {
                Current = SavedSession;
            }
        }

        internal void Connected(SessionLocator session, bool remember)
        {
            Current = session;
            _remember = remember;
            if (remember)
            {
                SaveSession(session);
            }
        }

        internal void ApplyLeave(LeaveMode mode)
        {
            if (mode == LeaveMode.ForgetSession)
            {
                Current = null;
                ClearSavedSessionName();
            }
            else if (ShouldRememberSession())
            {
                SaveSession(Current);
            }
        }

        public bool HasSavedSession
        {
            get
            {
                var savedSessionName = PlayerPrefs.GetString(SavedSessionNameKey, null);
                if (string.IsNullOrWhiteSpace(savedSessionName))
                {
                    return false;
                }

                var savedBinary = PlayerPrefs.GetString(SavedSessionTimeKey, null);
                if (HasInvalidSavedSession(savedBinary, out var binary))
                {
                    ClearSavedSessionName();
                    return false;
                }

                DateTime savedAt;
                try
                {
                    savedAt = DateTime.FromBinary(binary);
                }
                catch (ArgumentException)
                {
                    ClearSavedSessionName();
                    return false;
                }

                if (HasExpiredSavedSession(savedAt))
                {
                    ClearSavedSessionName();
                    return false;
                }

                return true;
            }
        }

        public string SavedSessionName => PlayerPrefs.GetString(SavedSessionNameKey, null);

        private SessionLocator SavedSession
        {
            get
            {
                var name = SavedSessionName;
                var stringValue = PlayerPrefs.GetString(SavedSessionRegionKey, "");
                return new(name, stringValue);
            }
        }

        private void SaveSession(SessionLocator session)
        {
            if (!session.IsComplete)
            {
                return;
            }

            PlayerPrefs.SetString(SavedSessionNameKey, session.Name);
            PlayerPrefs.SetString(SavedSessionRegionKey, session.Region);
            PlayerPrefs.SetInt(SavedSessionVersionKey, 1);
            var valueText = DateTime.UtcNow.ToBinary().ToString();
            PlayerPrefs.SetString(SavedSessionTimeKey, valueText);
            PlayerPrefs.Save();
        }

        private void ClearSavedSessionName()
        {
            PlayerPrefs.DeleteKey(SavedSessionNameKey);
            PlayerPrefs.DeleteKey(SavedSessionTimeKey);
            PlayerPrefs.DeleteKey(SavedSessionRegionKey);
            PlayerPrefs.DeleteKey(SavedSessionVersionKey);
            PlayerPrefs.Save();
        }

        private bool ShouldRememberSession()
        {
            return _remember && Current?.IsComplete == true;
        }

        private bool HasInvalidSavedSession(string savedBinary, out long binary)
        {
            binary = default;
            if (PlayerPrefs.GetInt(SavedSessionVersionKey, 0) != 1)
            {
                return true;
            }

            var savedRegion = PlayerPrefs.GetString(SavedSessionRegionKey, "");
            return string.IsNullOrWhiteSpace(savedRegion) || string.IsNullOrEmpty(savedBinary) ||
                   !long.TryParse(savedBinary, out binary);
        }

        private bool HasExpiredSavedSession(DateTime savedAt)
        {
            return savedAt > DateTime.UtcNow || DateTime.UtcNow - savedAt > _savedSessionTimeToLive;
        }
    }
}
