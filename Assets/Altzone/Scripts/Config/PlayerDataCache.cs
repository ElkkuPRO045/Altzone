using System;
using System.Collections;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Altzone.Scripts.Model;
using UnityEngine;

namespace Altzone.Scripts.Config
{
    /// <summary>
    /// Player data cache - a common storage for player related data that is persisted somewhere (locally).
    /// </summary>
    [Serializable]
    public class PlayerDataCache
    {
        protected const string DefaultPlayerName = "Player";
        protected const int DefaultModelId = (int)Defence.Introjection;
        protected const SystemLanguage DefaultLanguage = SystemLanguage.Finnish;

        [SerializeField] protected string _playerName;
        [SerializeField] protected string _clanName;

        /// <summary>
        /// Player name.
        /// </summary>
        /// <remarks>
        /// This should be validated and sanitized before accepting a new value.
        /// </remarks>
        public string PlayerName
        {
            get => _playerName;
            set
            {
                _playerName = value ?? string.Empty;
                Save();
            }
        }

        public bool HasPlayerName => !string.IsNullOrWhiteSpace(_playerName);

        /// <summary>
        /// Clan name.
        /// </summary>
        public string ClanName => _clanName;
        
        [SerializeField] protected int _characterModelId;

        /// <summary>
        /// Player character model id.
        /// </summary>
        public int CharacterModelId
        {
            get => _characterModelId;
            set
            {
                _characterModelId = value;
                Save();
            }
        }

        /// <summary>
        /// Player character model.
        /// </summary>
        /// <remarks>
        /// This is guaranteed to be valid reference all the time even <c>CharacterModelId</c> is invalid.
        /// </remarks>
        public CharacterModel GetCharacterModelForUi() =>
            Storefront.Get().GetCharacterModel(_characterModelId) ??
            Storefront.Get().GetCharacterModel(DefaultModelId) ??
            new CharacterModel(-1, "Ö", Defence.Introjection, 3, 3, 3, 3);

        [SerializeField] protected int _clanId;

        /// <summary>
        /// Player clan id.
        /// </summary>
        public int ClanId
        {
            get => _clanId;
            set
            {
                _clanId = value;
                Save();
            }
        }

        [SerializeField] protected string _playerGuid;

        /// <summary>
        /// Unique string to identify this player across devices and systems.
        /// </summary>
        /// <remarks>
        /// When new player is detected this could be set and persisted in all external systems in order to identify this player unambiguously.
        /// </remarks>
        public string PlayerGuid
        {
            get => _playerGuid;
            set
            {
                _playerGuid = value ?? string.Empty;
                Save();
            }
        }

        [SerializeField] protected SystemLanguage _language;

        /// <summary>
        /// Player's UNITY language.
        /// </summary>
        /// <remarks>
        /// We use <c>Localizer</c> to do actual storing of the language.
        /// </remarks>
        public SystemLanguage Language
        {
            get => _language;
            set
            {
                _language = value;
                Save();
            }
        }

        [SerializeField] protected bool _isTosAccepted;

        /// <summary>
        /// Is Terms Of Service accepted?
        /// </summary>
        public bool IsTosAccepted
        {
            get => _isTosAccepted;
            set
            {
                _isTosAccepted = value;
                Save();
            }
        }

        [SerializeField] protected bool _isDebugFlag;

        /// <summary>
        /// Debug FLag for debugging and diagnostics purposes.
        /// </summary>
        /// <remarks>
        /// Should be hidden in final product.
        /// </remarks>
        public bool IsDebugFlag
        {
            get => _isDebugFlag;
            set
            {
                _isDebugFlag = value;
                Save();
            }
        }

        public string GetPlayerInfoLabel()
        {
            var characterModelName = GetCharacterModelForUi().Name;
            if (ClanId > 0)
            {
                var clan = Storefront.Get().GetClanModel(ClanId);
                if (clan != null)
                {
                    return $"{PlayerName}[{clan.Tag}] {characterModelName}";
                }
            }
            return $"{PlayerName} {characterModelName}";
        }

        /// <summary>
        /// Protected <c>Save</c> method to handle single property change.
        /// </summary>
        protected virtual void Save()
        {
            // Placeholder for actual implementation in derived class.
        }

        /// <summary>
        /// Protected <c>InternalSave</c> to handle actual saving somewhere.
        /// </summary>
        protected virtual void InternalSave()
        {
            // Placeholder for actual implementation in derived class.
        }

        [Conditional("UNITY_EDITOR")]
        public void DebugResetPlayer()
        {
            // Actually can not delete at this level - just invalidate everything!
            PlayerName = string.Empty;
            CharacterModelId = DefaultModelId;
            ClanId = -1;
            IsTosAccepted = false;
            InternalSave();
        }

        [Conditional("UNITY_EDITOR")]
        public void DebugSavePlayer()
        {
            InternalSave();
        }

        public override string ToString()
        {
            // This is required for actual implementation to detect changes in our changeable properties!
            return
                $"Name {PlayerName}, Model {CharacterModelId}, Clan {ClanId}, ToS {(IsTosAccepted ? 1 : 0)}, Lang {Language}, Guid {PlayerGuid}";
        }
    }

    /// <summary>
    /// <c>PlayerDataCache</c> implementation using UNITY <c>PlayerPrefs</c> as backing storage.
    /// </summary>
    public class PlayerDataCacheLocal : PlayerDataCache
    {
        private readonly MonoBehaviour _host;
        private Coroutine _delayedSave;

        public PlayerDataCacheLocal(MonoBehaviour host)
        {
            _host = host;
            _playerName = PlayerPrefs.GetString(PlayerPrefKeys.PlayerName, string.Empty);
            _characterModelId = PlayerPrefs.GetInt(PlayerPrefKeys.CharacterModelId, DefaultModelId);
            _clanId = PlayerPrefs.GetInt(PlayerPrefKeys.ClanId, -1);
            _playerGuid = PlayerPrefs.GetString(PlayerPrefKeys.PlayerGuid, string.Empty);
            _language = (SystemLanguage)PlayerPrefs.GetInt(PlayerPrefKeys.LanguageCode, (int)DefaultLanguage);
            _isTosAccepted = PlayerPrefs.GetInt(PlayerPrefKeys.TermsOfService, 0) == 1;
            _isDebugFlag = PlayerPrefs.GetInt(PlayerPrefKeys.IsDebugFlag, 0) == 1;
            // TODO: Clan is not implemented yet.
            if (_clanId == -1 || string.IsNullOrWhiteSpace(_clanName))
            {
                _clanName = "Best Clan";
            }
            if (!string.IsNullOrWhiteSpace(PlayerGuid) && !string.IsNullOrWhiteSpace(_playerName))
            {
                return;
            }
            // Create and save these settings immediately on this device!
            if (string.IsNullOrWhiteSpace(PlayerGuid))
            {
                _playerGuid = CreatePlayerHandle();
                PlayerPrefs.SetString(PlayerPrefKeys.PlayerGuid, PlayerGuid);
            }
            if (string.IsNullOrWhiteSpace(_playerName))
            {
                _playerName = DefaultPlayerName;
                PlayerPrefs.SetString(PlayerPrefKeys.PlayerName, PlayerName);
            }
            PlayerPrefs.Save();
        }

        protected override void InternalSave()
        {
            PlayerPrefs.SetString(PlayerPrefKeys.PlayerName, PlayerName);
            PlayerPrefs.SetInt(PlayerPrefKeys.CharacterModelId, CharacterModelId);
            PlayerPrefs.SetInt(PlayerPrefKeys.ClanId, ClanId);
            PlayerPrefs.SetString(PlayerPrefKeys.PlayerGuid, PlayerGuid);
            PlayerPrefs.SetInt(PlayerPrefKeys.TermsOfService, (int)_language);
            PlayerPrefs.SetInt(PlayerPrefKeys.TermsOfService, IsTosAccepted ? 1 : 0);
            PlayerPrefs.SetInt(PlayerPrefKeys.IsDebugFlag, IsDebugFlag ? 1 : 0);
        }

        private static string CreatePlayerHandle()
        {
            // Create same GUID for same device if possible
            // - guid can be used to identify third party cloud game services
            // - we want to keep it constant for single device even this data is wiped e.g. during testing
            var deviceId = SystemInfo.deviceUniqueIdentifier;
            if (deviceId == SystemInfo.unsupportedIdentifier)
            {
                return Guid.NewGuid().ToString();
            }
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.Unicode.GetBytes(deviceId));
                return new Guid(hash).ToString();
            }
        }

        protected override void Save()
        {
            if (_host == null)
            {
                // Can not delay, using UNITY default functionality save on exit
                return;
            }
            if (_delayedSave != null)
            {
                // NOP - already delayed.
                return;
            }
            // Save all changed player prefs on next frame.
            _delayedSave = _host.StartCoroutine(DelayedSave());
        }

        private IEnumerator DelayedSave()
        {
            // By default Unity writes preferences to disk during OnApplicationQuit().
            // - you can force them to disk using PlayerPrefs.Save().
            yield return null;
            Debug.Log("PlayerPrefs.Save");
            InternalSave();
            PlayerPrefs.Save();
            _delayedSave = null;
        }
    }
}