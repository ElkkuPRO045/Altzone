using System;
using System.Collections;
using System.Diagnostics;
using Altzone.Scripts.Config;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace Battle.Test.Scripts.Battle.Ball
{
    internal enum BallState : byte
    {
        NoTeam = 0,
        RedTeam = 1,
        BlueTeam = 2,
        Ghosted = 3,
        Hidden = 4,
    }

    internal interface IBallManager
    {
        void SetBallPosition(Vector2 position);

        void SetBallVelocity(Vector2 velocity);

        void SetBallState(BallState ballState);
    }

    internal class BallManager : MonoBehaviourPunCallbacks, IBallManager, IPunObservable
    {
        [Serializable]
        internal class DebugSettings
        {
            public bool _isShowBallText;
            public TextMeshPro _ballText;
            public bool _isShowTrailRenderer;
            public TrailRenderer _trailRenderer;
        }

        private static readonly BallState[] BallStates =
            { BallState.NoTeam, BallState.RedTeam, BallState.BlueTeam, BallState.Ghosted, BallState.Hidden };

        private static readonly bool[] ColliderStates = { true, true, true, false, false };

        public static BallManager Get() => FindObjectOfType<BallManager>();

        [Header("Settings"), SerializeField] private GameObject _ballCollider;
        [SerializeField] private GameObject _spriteNoTeam;
        [SerializeField] private GameObject _spriteRedTeam;
        [SerializeField] private GameObject _spriteBlueTeam;
        [SerializeField] private GameObject _spriteGhosted;
        [SerializeField] private GameObject _spriteHidden;

        [Header("Live Data"), SerializeField] private BallState _ballState;

        [Header("Photon Networking"), SerializeField] private Vector2 _networkPosition;
        [SerializeField] private float _networkLag;

        [Header("Debug Settings"), SerializeField] private DebugSettings _debug;

        private PhotonView _photonView;
        private Rigidbody2D _rigidbody;
        private GameObject[] _sprites;

        private float _ballMoveSpeedMultiplier;
        private float _ballMinMoveSpeed;
        private float _ballMaxMoveSpeed;
        private float _ballLerpSmoothingFactor;
        private float _ballTeleportDistance;

        private void Awake()
        {
            Debug.Log($"{name}");
            _photonView = PhotonView.Get(this);
            _rigidbody = GetComponent<Rigidbody2D>();
            var variables = RuntimeGameConfig.Get().Variables;
            _ballMoveSpeedMultiplier = variables._ballMoveSpeedMultiplier;
            _ballMinMoveSpeed = variables._ballMinMoveSpeed;
            _ballMaxMoveSpeed = variables._ballMaxMoveSpeed;
            _ballLerpSmoothingFactor = variables._ballLerpSmoothingFactor;
            _ballTeleportDistance = variables._ballTeleportDistance;
            _sprites = new[] { _spriteNoTeam, _spriteRedTeam, _spriteBlueTeam, _spriteGhosted, _spriteHidden };
            SetDebug();
            _SetBallState(BallState.Ghosted);
            UpdateBallText();
        }

        private void SetDebug()
        {
            if (_debug._ballText == null)
            {
                _debug._isShowBallText = false;
            }
            else if (!_debug._isShowBallText)
            {
                _debug._ballText.gameObject.SetActive(false);
            }
            if (_debug._trailRenderer == null)
            {
                _debug._isShowTrailRenderer = false;
            }
            else if (!_debug._isShowTrailRenderer)
            {
                _debug._trailRenderer.gameObject.SetActive(false);
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            if (!_photonView.ObservedComponents.Contains(this))
            {
                // If not set in Editor
                // - and this helps to avoid unnecessary warnings when view starts to serialize itself "too early" for other views not yet ready.
                _photonView.ObservedComponents.Add(this);
            }
            UpdateBallText();
        }

        public override void OnDisable()
        {
            base.OnDisable();
            StopAllCoroutines();
            _ballVelocityTracker = null;
            _ballNetworkTracker = null;
        }

        private void MasterClientSwitched()
        {
            Debug.Log($"{name}");
            StopAllCoroutines();
            _ballVelocityTracker = null;
            _ballNetworkTracker = null;
            UpdateBallText();
        }
        
        private void _SetBallState(BallState ballState)
        {
            _ballState = ballState;
            var stateIndex = (int)ballState;
            _ballCollider.SetActive(ColliderStates[stateIndex]);
            for (var i = 0; i < BallStates.Length; ++i)
            {
                _sprites[i].SetActive(BallStates[i] == ballState);
            }
            var isDebugVisible = _ballState != BallState.Hidden;
            if (_debug._isShowBallText)
            {
                _debug._ballText.gameObject.SetActive(isDebugVisible);
            }
            if (_debug._isShowTrailRenderer)
            {
                _debug._trailRenderer.gameObject.SetActive(isDebugVisible);
            }
        }

        #region Debugging

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private void UpdateBallText()
        {
            if (!_debug._isShowBallText)
            {
                return;
            }
            _debug._ballText.text = $"{_rigidbody.velocity.magnitude:0.00}";
        }

        private Coroutine _ballVelocityTracker;
        private Vector2 _currentDebugVelocity;

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private void TrackBallVelocity()
        {
            if (_ballVelocityTracker == null)
            {
                _ballVelocityTracker = StartCoroutine(BallVelocityTracker());
            }
        }

        private IEnumerator BallVelocityTracker()
        {
            Debug.Log($"{name} velocity {_currentDebugVelocity} <- {_rigidbody.velocity}");
            _currentDebugVelocity = _rigidbody.velocity;
            for (;;)
            {
                yield return null;
                var velocity = _rigidbody.velocity;
                if (velocity == Vector2.zero)
                {
                    _ballVelocityTracker = null;
                    _currentDebugVelocity = Vector2.zero;
                    yield break;
                }
                if (velocity != _currentDebugVelocity)
                {
                    var prevSqr = velocity.sqrMagnitude;
                    var curSqr = _currentDebugVelocity.sqrMagnitude;
                    if (!Mathf.Approximately(prevSqr, curSqr))
                    {
                        var velocityChange = -(1 - prevSqr / curSqr) * 100;
                        Debug.Log(
                            $"{name} velocity {_currentDebugVelocity} <- {velocity} sqr {prevSqr:0.00} <- {curSqr:0.00} = {velocityChange:0.00}%");
                    }
                    _currentDebugVelocity = velocity;
                }
                UpdateBallText();
            }
        }

        #endregion

        #region IBallManager

        void IBallManager.SetBallPosition(Vector2 position)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Assert.IsTrue(PhotonNetwork.InRoom, "PhotonNetwork.InRoom");
                return;
            }
            _rigidbody.position = position;
            _photonView.RPC(nameof(TestBallPosition), RpcTarget.Others, position);
            UpdateBallText();
        }

        void IBallManager.SetBallVelocity(Vector2 velocity)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Assert.IsTrue(PhotonNetwork.InRoom, "PhotonNetwork.InRoom");
                return;
            }
            var speed = Mathf.Clamp(Mathf.Abs(velocity.magnitude), _ballMinMoveSpeed, _ballMaxMoveSpeed);
            _rigidbody.velocity = velocity.normalized * speed * _ballMoveSpeedMultiplier;
            _photonView.RPC(nameof(TestBallVelocity), RpcTarget.Others, velocity);
            UpdateBallText();
            TrackBallVelocity();
        }

        void IBallManager.SetBallState(BallState ballState)
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Assert.IsTrue(PhotonNetwork.InRoom, "PhotonNetwork.InRoom");
                return;
            }
            _SetBallState(ballState);
            _photonView.RPC(nameof(TestSetBallState), RpcTarget.Others, ballState);
            UpdateBallText();
        }

        #endregion

        #region IPunObservable

        private Coroutine _ballNetworkTracker;

        void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(_rigidbody.position);
                stream.SendNext(_rigidbody.velocity);
                return;
            }
            _networkPosition = (Vector2)stream.ReceiveNext();
            var networkVelocity = (Vector2)stream.ReceiveNext();
            if (_rigidbody.velocity != networkVelocity)
            {
                _rigidbody.velocity = networkVelocity;
                UpdateBallText();
            }
            _networkLag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));
            _networkPosition += networkVelocity * _networkLag;

            if (_ballNetworkTracker != null)
            {
                return;
            }
            _ballNetworkTracker = StartCoroutine(BallNetworkTracker());
            _ballCollider.SetActive(false);
        }

        private IEnumerator BallNetworkTracker()
        {
            for (;;)
            {
                var position = _rigidbody.position;
                var isTeleport = Mathf.Abs(position.x - _networkPosition.x) > _ballTeleportDistance ||
                                 Mathf.Abs(position.y - _networkPosition.y) > _ballTeleportDistance;
                _rigidbody.position = isTeleport
                    ? _networkPosition
                    : Vector2.MoveTowards(position, _networkPosition, Time.deltaTime * _ballLerpSmoothingFactor);
                yield return null;
            }
        }

        #endregion

        #region Photon

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            if (newMasterClient.Equals(PhotonNetwork.LocalPlayer))
            {
                MasterClientSwitched();
            }
        }

        #endregion

        #region Photon RPC

        // NOTE! When adding new RPC method check that the name is unique in PhotonServerSettings Rpc List!

        [PunRPC]
        private void TestBallPosition(Vector2 position)
        {
            _rigidbody.position = position;
        }

        [PunRPC]
        private void TestBallVelocity(Vector2 velocity)
        {
            _rigidbody.velocity = velocity;
        }

        [PunRPC]
        private void TestSetBallState(BallState ballState)
        {
            _SetBallState(ballState);
        }

        #endregion
    }
}