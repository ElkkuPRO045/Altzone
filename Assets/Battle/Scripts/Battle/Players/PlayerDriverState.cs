using Altzone.Scripts.Config;
using Altzone.Scripts.Model;
using Photon.Pun;
using Prg.Scripts.Common.Unity.Attributes;
using UnityEngine;

namespace Battle.Scripts.Battle.Players
{
    internal class PlayerDriverState : MonoBehaviour, IPlayerDriverState
    {
        [SerializeField, ReadOnly] private int _currentPoseIndex;
        [SerializeField, ReadOnly] private int _currentShieldResistance;
        [SerializeField, ReadOnly] private double _lastBallHitTime;

        private IPlayerDriver _playerDriver;
        private CharacterModel _characterModel;
        private IBallManager _ballManager;
        private float _stunDuration;
        private bool _isDisableShieldStateChanges;
        private bool _isDisableBallSpeedChanges;

        public double LastBallHitTime => _lastBallHitTime;

        public void ResetState(IPlayerDriver playerDriver, CharacterModel characterModel)
        {
            _playerDriver = playerDriver;
            _characterModel = characterModel;
            _ballManager = Context.BallManager;

            var runtimeGameConfig = RuntimeGameConfig.Get();
            var variables = runtimeGameConfig.Variables;
            _stunDuration = variables._playerShieldHitStunDuration;
            var features = runtimeGameConfig.Features;
            _isDisableShieldStateChanges = features._isDisableShieldStateChanges;
            _isDisableBallSpeedChanges = features._isDisableBallSpeedChanges;

            _currentPoseIndex = 0;
            _currentShieldResistance = characterModel.Resistance;
            _lastBallHitTime = PhotonNetwork.Time;
        }

        public void CheckRotation(Vector2 position)
        {
            var doRotate = position.y > 0;
            Debug.Log($"{name} playerPos {_playerDriver.PlayerPos} position {position} rotate {doRotate}");
            if (doRotate)
            {
                _playerDriver.Rotate(true);
            }
        }

        public void OnShieldCollision(out string debugString)
        {
            if (!_isDisableBallSpeedChanges)
            {
                if (PhotonNetwork.IsMasterClient)
                {
                    _ballManager.SetBallSpeed(_characterModel.Attack);
                }
            }
            if (_isDisableShieldStateChanges)
            {
                debugString = string.Empty;
                return;
            }
            if (_currentPoseIndex == _playerDriver.MaxPoseIndex)
            {
                Debug.Log($"stun pose {_currentPoseIndex} shield {_currentShieldResistance}");
                _playerDriver.SetPlayMode(BattlePlayMode.Ghosted);
                _playerDriver.SetStunned(_stunDuration);
                debugString = "STUN";
                return;
            }
            if (_currentShieldResistance > 0)
            {
                _currentShieldResistance -= 1;
                Debug.Log($"hit pose {_currentPoseIndex} shield {_currentShieldResistance}");
                _playerDriver.SetPlayMode(BattlePlayMode.Ghosted);
                _playerDriver.SetShieldResistance(_currentShieldResistance);
                debugString = "HIT";
                return;
            }
            _currentShieldResistance = _characterModel.Resistance;
            _currentPoseIndex += 1;
            Debug.Log($"bend pose {_currentPoseIndex} shield {_currentShieldResistance}");
            _playerDriver.SetPlayMode(BattlePlayMode.Ghosted);
            _playerDriver.SetStunned(_stunDuration);
            _playerDriver.SetCharacterPose(_currentPoseIndex);
            _playerDriver.SetShieldResistance(_currentShieldResistance);
            debugString = "BEND";
        }

        public void OnHeadCollision()
        {
            Debug.Log($"pose {_currentPoseIndex} shield {_currentShieldResistance}");
            _lastBallHitTime = PhotonNetwork.Time;
            _playerDriver.SetPlayMode(BattlePlayMode.Ghosted);
        }

        public override string ToString()
        {
            return $"pose={_currentPoseIndex} res={_currentShieldResistance}";
        }
    }
}