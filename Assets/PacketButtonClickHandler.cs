using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PacketButtonClickHandler : MonoBehaviour
{

    public Button button;
    private void Awake()
    {
        button = GetComponent<Button>();
        buttonColorBlock = button.colors;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public NewPacket packet;
    public void OnPacketButtonClick()
    {
        NewGameMgr.inst.OnPacketClicked(packet);

    }
    public ColorBlock buttonColorBlock;
    public void SetHighlightColor()
    {
        if(packet.isMalicious) {
            ColorBlock bcb = button.colors;
            bcb.highlightedColor = Color.red;
            button.colors = bcb;
        }
    }

}
