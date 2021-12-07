using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PacketLogger : Core.Utilities.SingletonPun<PacketLogger> {
    // Callback for logging events
    public delegate void PacketLogEventCallback(string packetColor, string packetSize, string packetShape, string destination);
    public static PacketLogEventCallback logEvent;

    // Enum storing the type of score event to process
    // Only one event right now, but who knows?
    [System.Serializable]
    public enum PacketLogEvent
    {
        PacketMeetsDestination
    }

    public Destination[] destinations; // Adjusted in the editor per-map

    void OnEnable() {  }
    void OnDisable() {  }



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ProcessPacketLogEvent(PacketRule.Details details, bool isMalicious, Destination destination) {
        
        // TODO
        // Intercept packet information
        // Update corresponding destination packet log

    }

    public void PopulatePacketView(GameObject packetViewScreen, Destination dest) {
        
        // Given a destination, populate the packet view screen

    }

    // Managing packet selection will be delegated to a different manager, probably
}
