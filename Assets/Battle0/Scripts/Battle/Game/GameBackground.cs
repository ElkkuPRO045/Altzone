using UnityEngine;
using UnityEngine.Assertions;

namespace Battle0.Scripts.Battle.Game
{
    /// <summary>
    /// Helper for game background to find it and change it if required.
    /// </summary>
    internal class GameBackground : MonoBehaviour, IBattleBackground
    {
        [Header("Settings"), SerializeField] private GameObject _gameBackground;
        [SerializeField] private Sprite[] _backgroundSprites;
        [SerializeField] private SpriteRenderer _renderer;

        public GameObject Background => _gameBackground;

        public bool IsRotated => _gameBackground.transform.rotation.z != 0f;

        public void SetBackgroundImageByIndex(int index)
        {
            Assert.IsTrue(index >= 0 && index < _backgroundSprites.Length, "index >= 0 && index < _backgroundSprites.Length");
            _renderer.sprite = _backgroundSprites[index];
        }
    }
}