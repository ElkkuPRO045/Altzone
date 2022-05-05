﻿using System;
using Altzone.Scripts.Config;
using Altzone.Scripts.Model;
using Photon.Pun;
using Prg.Scripts.Common.Photon;
using UnityEngine;

namespace Lobby.Scripts.InChooseModel
{
    /// <summary>
    /// UI controller for <c>CharacterModel</c> view.
    /// </summary>
    public class ModelController : MonoBehaviour
    {
        [SerializeField] private ModelView _view;

        private void Start()
        {
            Debug.Log("Start");
            _view.Reset();
            _view.Title = $"Choose your character\r\nfor {Application.productName} {PhotonLobby.GameVersion}";
            var playerDataCache = RuntimeGameConfig.Get().PlayerDataCache;
            _view.PlayerName = playerDataCache.PlayerName;
            _view.ContinueButtonOnClick = ContinueButtonOnClick;
            var currentCharacterId = playerDataCache.CharacterModelId;
            var characters = Storefront.Get().GetAllCharacterModels();
            characters.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            _view.SetCharacters(characters, currentCharacterId);
        }

        private void ContinueButtonOnClick()
        {
            Debug.Log("click");
            // Save player settings if changed before continuing!
            var playerDataCache = RuntimeGameConfig.Get().PlayerDataCache;
            if (_view.PlayerName != playerDataCache.PlayerName ||
                _view.CurrentCharacterId != playerDataCache.CharacterModelId)
            {
                Debug.Log("player.BatchSave");
                playerDataCache.BatchSave(() =>
                {
                    playerDataCache.PlayerName = _view.PlayerName;
                    playerDataCache.CharacterModelId = _view.CurrentCharacterId;
                });
            }
            if (PhotonNetwork.NickName != playerDataCache.PlayerName)
            {
                // Fix player name if it has been changed.
                PhotonNetwork.NickName = playerDataCache.PlayerName;
            }
        }
    }
}