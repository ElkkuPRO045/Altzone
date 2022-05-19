using Altzone.Scripts.Battle;
using Battle.Scripts.Battle.Factory;
using UnityEngine;

namespace Battle.Test.Scripts.Battle.Players
{
    /// <summary>
    /// <c>PlayerActor</c> for local and remote instances.
    /// </summary>
    /// <remarks>
    /// This class manages local visual representation of player actor.
    /// </remarks>
    internal class PlayerActor : MonoBehaviour
    {
        [SerializeField] private PlayerDriver _playerDriver;

        public static PlayerActor Instantiate(PlayerDriver playerDriver, PlayerActor playerPrefab)
        {
            var player = playerDriver.Player;
            Debug.Log($"{player.GetDebugLabel()} {playerPrefab}");
            
            var playerPos = PhotonBattle.GetPlayerPos(player);
            var instantiationPosition = Context.GetPlayerPlayArea.GetPlayerStartPosition(playerPos);
            var playerTag = $"{playerPos}:{player.NickName}";

            var playerActor = Instantiate(playerPrefab, instantiationPosition, Quaternion.identity);
            playerActor.name = playerActor.name.Replace("Clone", playerTag);
            playerActor.SetPlayerDriver(playerDriver);
            return playerActor;
        }
        
        private void Awake()
        {
            // Wait until PlayerDriver is assigned. 
            enabled = false;
        }

        private void SetPlayerDriver(PlayerDriver playerDriver)
        {
            // No we are good to go.
            _playerDriver = playerDriver;
            enabled = true;
        }

        private void OnEnable()
        {
            var player = _playerDriver.Player;
            Debug.Log($"{player.GetDebugLabel()}");
        }
    }
}