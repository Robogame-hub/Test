using UnityEngine;

namespace TankGame.Core
{
    /// <summary>
    /// Интерфейс для компонентов, которые синхронизируются по сети
    /// </summary>
    public interface INetworkSyncable
    {
        void Serialize(NetworkWriter writer);
        void Deserialize(NetworkReader reader);
    }

    // Заглушки для сетевой сериализации (можно заменить на Mirror/Netcode)
    public class NetworkWriter
    {
        public void WriteFloat(float value) { }
        public void WriteVector3(Vector3 value) { }
        public void WriteQuaternion(Quaternion value) { }
        public void WriteBool(bool value) { }
    }

    public class NetworkReader
    {
        public float ReadFloat() { return 0f; }
        public Vector3 ReadVector3() { return Vector3.zero; }
        public Quaternion ReadQuaternion() { return Quaternion.identity; }
        public bool ReadBool() { return false; }
    }
}

