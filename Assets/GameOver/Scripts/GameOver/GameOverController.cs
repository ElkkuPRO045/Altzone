﻿using System.Collections;
using Altzone.Scripts.Battle;
using Photon.Pun;
using Prg.Scripts.Common.Photon;
using Prg.Scripts.Common.Unity.Window;
using UnityEngine;

namespace GameOver.Scripts.GameOver
{
    public class GameOverController : MonoBehaviour
    {
        private const float DefaultTimeout = 2.0f;

        [SerializeField] private GameOverView _view;
        [SerializeField] private float _timeOutDelay;

        private void OnEnable()
        {
            _view.Reset();
            if (!PhotonNetwork.InRoom)
            {
                _view.EnableContinueButton();
                _view.WinnerInfo1 = RichText.Yellow("Game was interrupted");
                return;
            }
            _view.WinnerInfo1 = RichText.Yellow("Checking results");
            if (_timeOutDelay == 0f)
            {
                _timeOutDelay = DefaultTimeout;
            }
            _view.RestartButtonOnClick = RestartButtonClick;
            _view.ContinueButtonOnClick = ContinueButtonClick;
            WindowManager.Get().RegisterGoBackHandlerOnce(() =>
            {
                CloseRoomForLobby();
                return WindowManager.GoBackAction.Continue;
            });
            Debug.Log($"OnEnable {PhotonNetwork.CurrentRoom.GetDebugLabel()}");
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
                var winnerTeam = PhotonWrapper.GetRoomProperty(PhotonBattle.TeamWinKey, PhotonBattle.NoTeamValue);
                if (winnerTeam == PhotonBattle.NoTeamValue)
                {
                    yield return null;
                    continue;
                }
                var blueScore = PhotonWrapper.GetRoomProperty(PhotonBattle.TeamBlueScoreKey, 0);
                var redScore = PhotonWrapper.GetRoomProperty(PhotonBattle.TeamRedScoreKey, 0);
                // It is possible that we can have equal score and winning team - but that can not be true!
                var isScoreValid = blueScore != redScore;
                if (winnerTeam == PhotonBattle.TeamBlueValue)
                {
                    _view.WinnerInfo1 = isScoreValid ? RichText.Blue("Team BLUE") : RichText.Yellow("DRAW!");
                    _view.WinnerInfo2 = $"{blueScore} - {redScore}";
                }
                else if (winnerTeam == PhotonBattle.TeamRedValue)
                {
                    _view.WinnerInfo1 = isScoreValid ? RichText.Red("Team RED") : RichText.Yellow("DRAW!");
                    _view.WinnerInfo2 = $"{redScore} - {blueScore}";
                }
                else
                {
                    _view.WinnerInfo1 = RichText.Yellow("DRAW!");
                    _view.WinnerInfo2 = string.Empty;
                }
                break;
            }
            _view.EnableContinueButton();
            if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
            {
                _view.EnableRestartButton();
            }
        }

        private void Update()
        {
            StartCoroutine(BusyPolling());
        }

        private IEnumerator BusyPolling()
        {
            var delay = new WaitForSeconds(0.3f);
            var room = PhotonNetwork.CurrentRoom;
            var playerCount = room.PlayerCount;
            for (int i = 0; i < room.PlayerCount; i++)
            {
                yield return delay;
                if (playerCount != room.PlayerCount)
                {
                    _view.DisableRestartButton();
                    if (PhotonNetwork.IsMasterClient && PhotonNetwork.InRoom)
                        PhotonNetwork.CurrentRoom.IsOpen = false;
                    break;
                }
            }
        }

        private void RestartButtonClick()
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