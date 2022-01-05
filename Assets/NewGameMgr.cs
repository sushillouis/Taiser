using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class NewPacket
{
    public PacketRule.Color color;
    public PacketRule.Shape shape;
    public PacketRule.Size  size;
    public bool isMalicious;
    
}

public class NewGameMgr : MonoBehaviour
{

    public static NewGameMgr inst;
    private void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        State = GameState.Start;
        Initialize();
    }
    public void Initialize()
    {
        //Init one queue for each destination. Lists show up in the inspector for debugging so...
        PacketQueues.Add(new List<NewPacket>());
        PacketQueues.Add(new List<NewPacket>());
    }
    // Update is called once per frame
    void Update()
    {

    }

    //-------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------

    public enum GameState
    {
        Start = 0,
        Watching,
        PacketExamining,
        BeingAdvised,
        Menu,
        None
    }
    public TaiserPanel PacketExaminerPanel;
    public TaiserPanel StartPanel;
    public TaiserPanel WatchingPanel;

    public GameState _state;
    public GameState PriorState;
    public GameState State
    {
        get { return _state; }
        set
        {
            PriorState = _state;
            _state = value;

            PacketExaminerPanel.isVisible = (_state == GameState.PacketExamining);
            StartPanel.isVisible = (_state == GameState.Start);
            WatchingPanel.isVisible = (_state == GameState.Watching);
            //add menu panel

        }
    }



    public void OnPacketClicked(NewPacket packet)
    {
        DisplayPacketInformation(packet);

    }

    public void OnBackButton()
    {
        State = GameState.Watching;
    }


    public List<Destination> destinations = new List<Destination>();
    public GameObject PacketButtonPrefab;
    public Transform PacketButtonParent;
    public List<GameObject> packetButtons = new List<GameObject>();
    public void OnAttackableDestinationClicked(int attackId)
    {
        packetButtons.Clear(); //Bug: packet buttons need to be destroyed or reused (performance)
        Destination d = destinations[attackId];
        d.DestinationButton.GetComponent<RectTransform>().sizeDelta = d.minSize;//reset button to min size
        foreach(NewPacket np in PacketQueues[d.DestinationId]) { 
            GameObject go = Instantiate(PacketButtonPrefab, PacketButtonParent);
            PacketButtonClickHandler pbch = go.GetComponentInChildren<PacketButtonClickHandler>();
            pbch.packet.color = np.color;
            pbch.packet.size = np.size;
            pbch.packet.shape = np.shape;
            pbch.packet.isMalicious = np.isMalicious;
            pbch.SetHighlightColor();// red if malicious else green
            packetButtons.Add(go);
        }

        PacketQueues[d.DestinationId].Clear(); //?? for gameplay: should wait for at least one malicious packet to appear

        State = GameState.PacketExamining;

    }

    public NewPacket tmpTest;
    public List<List<NewPacket>> PacketQueues = new List<List<NewPacket>>();
    public int QueueSizeLimit;
    public void LogPacket(PacketRule.Details packetDetails, bool isMalicious, int destinationId)
    {
        NewPacket packet = new NewPacket();
        packet.color = packetDetails.color;
        packet.shape = packetDetails.shape;
        packet.size = packetDetails.size;
        packet.isMalicious = isMalicious;
        AddFIFOSizeLimitedQueue(PacketQueues[destinationId], packet, QueueSizeLimit);
    }

    void AddFIFOSizeLimitedQueue(List<NewPacket> packetList, NewPacket packet, int limit)
    {
        if (packetList.Count >= limit)
            packetList.RemoveAt(0);

        packetList.Add(packet);

    }

    public void DisplayPacketInformation(NewPacket packet)
    {
        Debug.Log("Displaying packet information\n" + "Color: " + packet.color
            + ", Shape: " + packet.shape + ", Size: " + packet.size);
    }

    public void OnReady()
    {
        AudioManager.instance.uiSoundFXPlayer.PlayTrackImmediate("SettingsUpdated");

        if(NetworkingManager.instance)
            NetworkingManager.instance.setReady(true);
        else
            GameManager.instance.StartNextWave();
        State = GameState.Watching;
    }


}
