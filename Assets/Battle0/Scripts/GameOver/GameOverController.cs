﻿using System.Collections;
using Photon.Pun;
using Prg.Scripts.Common.Unity.Window;
using UnityEngine;

namespace Battle0.Scripts.GameOver
{
    public class GameOverController : MonoBehaviour
    {
        private const float DefaultTimeout = 2.0f;
        private const float DefaultPollingInterval = 0.3f;

        [SerializeField] private GameOverView _view;
        [SerializeField] private float _timeOutDelay;
        [SerializeField] private float _pollingInterval;
        private int _playerCount;
        
        private void OnEnable()
        {
            _playerCount = PhotonBattle.GetPlayerCountForRoom();
            Debug.Log($"{name}");
            _view.Reset();
            if (!PhotonNetwork.InRoom)
            {
                _view.EnableContinueButton();
                _view.WinnerInfo1 = RichText.Yellow("Game was interrupted");
                return;
            }
            _view.WinnerInfo1 = RichText.Yellow("Checking results");
            if (_timeOutDelay == 0)
            {
                _timeOutDelay = DefaultTimeout;
            }
            if (_pollingInterval == 0)
            {
                _pollingInterval = DefaultPollingInterval;
            }
            _view.RestartButtonOnClick = RestartButtonClick;
            _view.ContinueButtonOnClick = ContinueButtonClick;
            WindowManager.Get().RegisterGoBackHandlerOnce(() =>
            {
                CloseRoomForLobby();
                return WindowManager.GoBackAction.Continue;
            });
            StartCoroutine(WaitForWinner());
        }

        private IEnumerator WaitForWinner()
        {
            yield return null;
            var timeOutTime = _timeOutDelay + Time.time;
            while (PhotonNetwork.InRoom)
            {
                if (Time.time > timeOutTime)
                {
                    _view.WinnerInfo1 = RichText.Yellow("No scores found");
                    break;
                }
                var score = PhotonBattle.GetRoomScore();
                if (!score.IsValid)
                {
                    yield return null;
                    continue;
                }
                var playerPos = PhotonBattle.GetPlayerPos(PhotonNetwork.LocalPlayer);
                var myTeam = PhotonBattle.GetTeamNumber(playerPos);
                UpdateGameOverTexts(myTeam, score);
                break;
            }
            _view.EnableContinueButton();
            if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                if (_playerCount == PhotonNetwork.CurrentRoom.PlayerCount)
                {
                    _view.EnableRestartButton();
                    StartCoroutine(BusyPlayerPolling());
                }
            }
        }
    
        private void UpdateGameOverTexts(int myTeam, PhotonBattle.RoomScore score)
        {
            Debug.Log($"myTeam {myTeam} score {score}");
            Debug.Log(PhotonNetwork.CurrentRoom.GetDebugLabel());
            Debug.Log(PhotonNetwork.LocalPlayer.GetDebugLabel());
            if (score.WinningTeam == myTeam
                && (score.WinType == PhotonBattle.WinTypeScore || score.WinType == PhotonBattle.WinTypeResign))
            {
                _view.WinnerInfo1 = RichText.Yellow("YOU WIN");
                return;
            }
            _view.WinnerInfo1 = RichText.Brown("YOU LOSE");
        }

        private IEnumerator BusyPlayerPolling()
        {
            var delay = new WaitForSeconds(_pollingInterval);
            while (PhotonNetwork.InRoom)
            {
                if (_playerCount != PhotonNetwork.CurrentRoom.PlayerCount)
                {
                    _view.DisableRestartButton();
                    yield break;
                }
                yield return delay;
            }
        }

        private static void RestartButtonClick()
        {
            Debug.Log($"click {PhotonNetwork.NetworkClientState}");
            if (PhotonNetwork.InRoom)
            {
                PhotonBattle.ResetRoomScores(PhotonNetwork.CurrentRoom);
            }
        }

        private static void ContinueButtonClick()
        {
            Debug.Log($"click {PhotonNetwork.NetworkClientState}");
            CloseRoomForLobby();
        }

        private static void CloseRoomForLobby()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                // We disable scene sync in order to prevent Photon sending scene load events to other clients because this room is finished now.
                // - PhotonLobby should set it automatically again if/when needed.
                PhotonNetwork.AutomaticallySyncScene = false;
            }
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
        }
    }
}
