using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TDestination : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        maliciousCount = 0;
        maliciousFilteredCount = 0;
        maliciousUnfilteredCount = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public int myId;
    public Button button;
    public string gameName;

    public void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("TDestination Collided with " + collision.gameObject.name);
        TPacket packet = collision.transform.parent.gameObject.GetComponent<TPacket>();
        if(null != packet) {
            packetCount += 1;
            //if packet is not malicious
            if(!packet.isMalicious) {
                TLogPacket(packet);
            } else if (packet.isMalicious && !isPacketFiltered(packet)) {
                maliciousCount += 1;
                maliciousUnfilteredCount += 1;
                GrowButton();
                TLogPacket(packet);
            } else if (packet.isMalicious && isPacketFiltered(packet)) {
                maliciousCount += 1;
                maliciousFilteredCount += 1;
            }
            NewEntityMgr.inst.ReturnPoolPacket(packet); // return to pool: reparent, set velocity to zero
        }
    }

    public Vector3 scaleDelta;
    public Vector3 maxScale;

    public void GrowButton()
    {
        if(button?.transform.localScale.x < maxScale.x)
            button.transform.localScale += scaleDelta;
    }
    public void ResetButton()
    {
        button.transform.localScale = Vector3.one;
    }

    public int QueueSizeLimit = 21;
    public List<LightWeightPacket> PacketQueue = new List<LightWeightPacket>();

    public void TLogPacket(TPacket taiserPacket)
    {
        LightWeightPacket packet = new LightWeightPacket();
        packet.color = taiserPacket.TColor;
        packet.shape = taiserPacket.Shape;
        packet.size = taiserPacket.Size;
        packet.isMalicious = taiserPacket.isMalicious;
        AddFIFOSizeLimitedQueue(PacketQueue, packet, QueueSizeLimit);
        // limit is what can be displayed in the button list 
    }
    void AddFIFOSizeLimitedQueue(List<LightWeightPacket> packetList, LightWeightPacket packet, int limit)
    {
        if(packetList.Count >= limit)
            packetList.RemoveAt(0);
        packetList.Add(packet);
    }

    public void OnAttackableDestinationClicked()
    {
        if(PacketQueue.Count > 0) {
            Debug.Log("Destination: " + myId + " clicked, Packet Queue[0]: " + PacketQueue[0].ToString());
            NewGameMgr.inst.OnAttackableDestinationClicked(this);
        }
    }

    public LightWeightPacket CurrentFilter;
    public bool isCurrentFilterValid = false; //set to false at the beginning of every wave
    public void FilterOnRule(LightWeightPacket lwp)
    {
        isCurrentFilterValid = true;
        CurrentFilter = lwp;
    }
    public int maliciousFilteredCount = 0;
    public int maliciousCount = 0;
    public int maliciousUnfilteredCount = 0;
    public int packetCount = 0;
    public bool isPacketFiltered(TPacket packet)
    {
        return isCurrentFilterValid && 
            (packet.Size == CurrentFilter.size && 
            packet.TColor == CurrentFilter.color && 
            packet.Shape == CurrentFilter.shape);
    }

    public void ResetCounts()
    {
        maliciousCount = 0;
        maliciousFilteredCount = 0;
    }

}
