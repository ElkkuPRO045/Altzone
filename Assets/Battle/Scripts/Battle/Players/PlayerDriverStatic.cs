using System;
using Altzone.Scripts.Battle;
using Altzone.Scripts.Config;
using Altzone.Scripts.Model;
using Battle.Scripts.Ui;
using Prg.Scripts.Common.PubSub;
using Prg.Scripts.Common.Unity.Attributes;
using UnityEngine;
using UnityEngine.Assertions;

namespace Battle.Scripts.Battle.Players
{
    /// <summary>
    /// Static <c>PlayerDriver</c> implementation.
    /// </summary>
    /// <remarks>
    /// Set our ExecutionOrder a bit lower to let other components initialize properly before us.<br />
    /// Note that this (class) is strictly for testing purposes!
    /// </remarks>
    [DefaultExecutionOrder(100)]
    internal class PlayerDriverStatic : PlayerDriver, IPlayerDriver, IPlayerActorCollision
    {
        [Serializable]
        internal class Settings
        {
            public string _nickName;
            public int _playerPos = PhotonBattle.PlayerPosition1;
            public int _teamNumber = PhotonBattle.TeamBlueValue;
            public Defence _playerMainSkill = Defence.Deflection;
            public bool _isLocal;
        }

        [Serializable]
        internal class DebugSettings
        {
            public PlayerActorBase _playerPrefab;
        }

        [Header("Settings"), SerializeField] private Settings _settings;

        [Header("Debug Settings"), SerializeField] private DebugSettings _debug;
        
        [Header("Live Data"), SerializeField, ReadOnly] private int _actorNumber;
        
        private CharacterModel _characterModel;
        private IPlayerActor _playerActor;
        private IPlayerDriverState _state;
        private IGridManager _gridManager;
        private bool _isApplicationQuitting;
        private bool _isDestroyed;
        private double _movementDelay;
        public bool IsMoving { get; set; }

        private void Awake()
        {
            print("++");
            Assert.IsTrue(PhotonBattle.IsValidGameplayPos(_settings._playerPos), "PhotonBattle.IsValidGameplayPos(_playerPos)");
            Application.quitting += () => _isApplicationQuitting = true;
            _gridManager = Context.GetGridManager;
            var runtimeGameConfig = RuntimeGameConfig.Get();
            var variables = runtimeGameConfig.Variables;
            _movementDelay = variables._playerMovementNetworkDelay;
        }

        private void OnEnable()
        {
            Debug.Log($"{name}");
            if (string.IsNullOrWhiteSpace(_settings._nickName))
            {
                _settings._nickName = name;
            }
            var gameplayManager = Context.PlayerManager;
            _actorNumber = -(gameplayManager.PlayerCount + 1);
            _characterModel = Storefront.Get().GetCharacterModel((int)_settings._playerMainSkill);
            _playerActor = PlayerActorBase.InstantiatePrefabFor(this, _characterModel.MainDefence, _debug._playerPrefab);
            {
                // This code block should be shared with all PlayerDriver implementations
                _playerActor.Setup(_characterModel.Speed, _characterModel.Resistance);
                _state = GetPlayerDriverState(this);
                var playerWorldPosition = _state.ResetState(this, _playerActor, _characterModel, _playerActor.Transform.position);
                _state.CheckRotation(playerWorldPosition);
                ConnectDistanceMeter(this, GetComponent<PlayerDistanceMeter>());
            }
            gameplayManager.RegisterPlayer(this);
            if (!_settings._isLocal)
            {
                return;
            }
            var playerInputHandler = PlayerInputHandler.Get();
            var playArea = Context.GetBattlePlayArea.GetPlayerPlayArea(_settings._playerPos);
            playerInputHandler.SetPlayerDriver(this, _playerActor.Transform, playArea);
        }

        private void OnDestroy()
        {
            if (_isApplicationQuitting || _playerActor == null)
            {
                return;
            }
            _isDestroyed = true;
            print("xx");
            Debug.Log($"{name}");
            DisconnectDistanceMeter(this, GetComponent<PlayerDistanceMeter>());
            var gameplayManager = Context.PlayerManager;
            gameplayManager?.UnregisterPlayer(this, _playerActor.GameObject);
            if (!_settings._isLocal)
            {
                return;
            }
            var playerInputHandler = PlayerInputHandler.Get();
            playerInputHandler?.ResetPlayerDriver();
        }

        private void SetWaitingState(bool isWaitingForAnswer)
        {
            _state.SetIsWaitingForAnswer(isWaitingForAnswer);
        }

        #region IPlayerActorCollision

        void IPlayerActorCollision.OnShieldCollision(Collision2D collision)
        {
            // This call can invalidate current collider!
            _state.OnShieldCollision(out var hitType);
            this.Publish(new UiEvents.ShieldCollision(collision, this, hitType));
        }

        void IPlayerActorCollision.OnHeadCollision(Collision2D collision)
        {
            // This call can invalidate current collider!
            _state.OnHeadCollision();
            this.Publish(new UiEvents.HeadCollision(collision, this));
            this.Publish(new UiEvents.RestartBattle(this));
        }

        #endregion

        #region IPlayerInfo (readonly)

        int IPlayerInfo.ActorNumber => _actorNumber;

        int IPlayerInfo.TeamNumber => _settings._teamNumber;

        bool IPlayerInfo.IsLocal => _settings._isLocal;

        Vector2 IPlayerInfo.Position => _playerActor.Transform.position;

        double IPlayerInfo.LastBallHitTime => _state.LastBallHitTime;

        #endregion

        #region IPlayerDriver

        string IPlayerDriver.NickName => _settings._nickName;

        int IPlayerDriver.PeerCount => 0;

        bool IPlayerDriver.IsValid => !_isDestroyed && _playerActor != null;

        int IPlayerDriver.PlayerPos => _settings._playerPos;

        int IPlayerDriver.MaxPoseIndex => _playerActor.MaxPoseIndex;

        CharacterModel IPlayerDriver.CharacterModel => _characterModel;

        Transform IPlayerDriver.PlayerTransform => _playerActor.Transform;

        BattlePlayMode IPlayerDriver.BattlePlayMode => _playerActor.BattlePlayMode;

        IPlayerActorCollision IPlayerDriver.PlayerActorCollision => this;

        void IPlayerDriver.Rotate(bool isUpsideDown)
        {
            _playerActor.Rotate(isUpsideDown);
        }

        void IPlayerDriver.FixCameraRotation(Camera gameCamera)
        {
            _playerActor.FixCameraRotation(gameCamera);
        }

        void IPlayerDriver.MoveTo(Vector2 targetPosition)
        {
            _playerActor.MoveTo(targetPosition);
        }

        void IPlayerDriver.SetCharacterPose(int poseIndex)
        {
            _playerActor.SetCharacterPose(poseIndex);
        }

        void IPlayerDriver.SetPlayMode(BattlePlayMode playMode)
        {
            _playerActor.SetPlayMode(playMode);
        }

        void IPlayerDriver.SetShieldVisibility(bool state)
        {
            _playerActor.SetShieldVisibility(state);
        }

        void IPlayerDriver.SetShieldResistance(int resistance)
        {
            _playerActor.SetShieldResistance(resistance);
        }

        void IPlayerDriver.SetStunned(float duration)
        {
            _playerActor.SetBuff(PlayerBuff.Stunned, duration);
        }

        void IPlayerDriver.PlayerActorDestroyed()
        {
            if (_isDestroyed)
            {
                return;
            }
            Debug.Log($"{name}");
            _playerActor = null;
            DisconnectDistanceMeter(this, GetComponent<PlayerDistanceMeter>());
        }

        void IPlayerDriver.SetSpaceFree(GridPos gridPos)
        {
            var row = gridPos.Row;
            var col = gridPos.Col;
            _gridManager.SetGridState(row, col, true);
            Debug.Log($"Grid space free: row: {row}, col: {col}");
        }

         public void SetSpaceTaken(GridPos gridPos)
        {
            var row = gridPos.Row;
            var col = gridPos.Col;
            _gridManager.SetGridState(row, col, false);
            Debug.Log($"Grid space taken: row: {row}, col: {col}");
        }

        void IPlayerDriver.SendMoveRequest(GridPos gridPos)
        {
            if (!_state.CanRequestMove) { return; }
            _state.SetIsWaitingForAnswer(true);
            ProcessMoveRequest(gridPos);
        }

        private void ProcessMoveRequest(GridPos gridPos)
        {
            var row = gridPos.Row;
            var col = gridPos.Col;
            if (!_gridManager.GridState(row, col))
            {
                Debug.Log($"Grid check failed. row: {row}, col: {col}");
                SetWaitingState(false);
                return;
            }
            SetSpaceTaken(gridPos);
            _state.DelayedMove(gridPos, (float)_movementDelay);
        }
        #endregion
    }
}
