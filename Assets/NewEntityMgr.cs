using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NewEntityMgr : MonoBehaviour
{
    public static NewEntityMgr inst;
    private void Awake()
    {
        inst = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        Packets.Clear();
        InitPools();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    //public TSource SourcePrefab;
    //public TDestination DestinationPrefab;

    public TPacket CapsulePrefab;
    public TPacket CubePrefab;
    public TPacket SpherePrefab;

    public TSource PacketPoolParent;

    public List<TPacket> Packets = new List<TPacket>();
    public static uint packetId = 0;
    public TPacket InstantiatePacket(PacketShape shape, PacketColor color = PacketColor.Blue,
        PacketSize size = PacketSize.Large)    {

        //Debug.Log("Creating packet with default parent");
        TPacket tPacket = null;
        switch(shape) {
            case PacketShape.Capsule:
                tPacket = Instantiate<TPacket>(CapsulePrefab, PacketPoolParent.transform);
                break;
            case PacketShape.Cube:
                tPacket = Instantiate<TPacket>(CubePrefab, PacketPoolParent.transform);
                break;
            case PacketShape.Sphere:
                tPacket = Instantiate<TPacket>(SpherePrefab, PacketPoolParent.transform);
                break;
            default:
                Debug.LogWarning("Unknown packet shape: " + shape.ToString());
                tPacket = Instantiate<TPacket>(CapsulePrefab, PacketPoolParent.transform);
                break;
        }
        if(null != tPacket) {
            tPacket.Init(packetId++, color, size);
            Packets.Add(tPacket);
        }
        return tPacket;
    }

    public List<TPacket> CubePacketPool;
    public List<TPacket> SpherePacketPool;
    public List<TPacket> CapsulePacketPool;
    public Dictionary<PacketShape, List<TPacket>> PacketPools = new Dictionary<PacketShape, List<TPacket>>();
    public int PoolLimit = 10;

    [ContextMenu("InitPools")]
    public void InitPools()
    {
        Debug.Log("initializing pools");
        foreach(PacketShape shape in System.Enum.GetValues(typeof(PacketShape))) {
            PacketPools.Add(shape, new List<TPacket>());
            FillPool(shape, PacketPools[shape], PoolLimit);
        }
        CubePacketPool = new List<TPacket>(PacketPools[PacketShape.Cube]);
        SpherePacketPool = new List<TPacket>(PacketPools[PacketShape.Sphere]);
        CapsulePacketPool = new List<TPacket>(PacketPools[PacketShape.Capsule]);
    }

    public void FillPool(PacketShape shape, List<TPacket> pool, int limit)
    {
        pool.Clear();
        for(int i = 0; i < limit; i++) {
            pool.Add(InstantiatePacket(shape));
        }
    }

    public TPacket CreatePacket(PacketShape shape, PacketColor color = PacketColor.Blue,
        PacketSize size = PacketSize.Large)
    {
        TPacket packet = null;

        List<TPacket> packetPool = PacketPools[shape];
        if(packetPool.Count > 0) {
            packet = packetPool[0];
            packet.ReInit(color, size); //only for pool packet, InstantiatePacket calls Init for new packets
            packetPool.RemoveAt(0);
        } else {
            Debug.Log("Creating new packet for empty pool");
            packet = InstantiatePacket(shape, color, size);
            PacketPools[shape].Add(packet);
        }
        return packet;
    }
    
    public void ReturnPoolPacket(TPacket packet)
    {
        PacketPools[packet.packet.shape].Add(packet);
        packet.transform.SetParent(PacketPoolParent.transform);
        packet.normalizedHeading = Vector3.zero;
        packet.transform.localPosition = Vector3.zero;

    }



}
