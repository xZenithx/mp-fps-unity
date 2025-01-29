using Unity.Netcode;

public struct DamageData : INetworkSerializable
{
    public NetworkObjectReference Source;
    public float Damage;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Source);
        serializer.SerializeValue(ref Damage);
    }
}