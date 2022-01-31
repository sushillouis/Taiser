using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PacketButtonMgr : MonoBehaviour
{
    public static PacketButtonMgr inst;
    private void Awake()
    {
        inst = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    //-------------------------------------------------------------------------------------
    //public GameObject PacketButtonPrefab;
    public Transform PacketButtonParent;
    public List<PacketButtonClickHandler> packetButtons = new List<PacketButtonClickHandler>();
    
    [ContextMenu("LinkButtonList")]//For editor setup of buttons
    public void LinkButtonList()
    {
        packetButtons.Clear();
        foreach(PacketButtonClickHandler pbch in PacketButtonParent.GetComponentsInChildren<PacketButtonClickHandler>()) {
            packetButtons.Add(pbch);
        }
        Debug.Log("Number of buttons: " + packetButtons.Count);
    }

    public void ResetPacketButtons()
    {
        foreach(PacketButtonClickHandler pbch in packetButtons) {
            pbch.SetGreenHighlightColor();//togreen
            pbch.transform.parent.gameObject.SetActive(false);
        }
    }

    //gameplay packetButtons.count <= PacketQueue limit
    public void OnAttackableDestinationClicked(TDestination destination)
    {
        int index = 0;
        ResetPacketButtons(); // make all taiser button panels invisible
        destination.PacketQueue.Reverse();
        foreach (LightWeightPacket lwp in destination.PacketQueue) { 
            //Debug.Log("Button for: " + lwp.size + ", " + lwp.color + ", " + lwp.shape);
            packetButtons[index].packet.color = lwp.color;
            packetButtons[index].packet.shape = lwp.shape;
            packetButtons[index].packet.size = lwp.size;

            //packetButtons[index].packet.isMalicious = lwp.isMalicious; // more understandable when reading than using setter property
            packetButtons[index].SetHighlightColor();
            packetButtons[index].transform.parent.gameObject.SetActive(true); //make this button panel visible

            index += 1;
            if(index >= packetButtons.Count) // only have this many buttons available!
                break;
        }
        destination.PacketQueue.Clear(); // Once you click on a button, you lose all packets
    }

    public void ResetHighlightColor()
    {
        foreach(PacketButtonClickHandler pbch in packetButtons) {
            if(pbch.isActiveAndEnabled)
                pbch.SetHighlightColor();
        }
    }


}
