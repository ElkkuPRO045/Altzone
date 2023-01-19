using System;

namespace Altzone.Scripts.Model
{
    [Serializable]
    public class PlayerDataModel
    {
        public int _id;
        public int _backpackCapacity;

        public PlayerDataModel(int id, int backpackCapacity)
        {
            _backpackCapacity = backpackCapacity;
        }
    }
}