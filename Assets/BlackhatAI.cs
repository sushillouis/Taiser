using System.Collections.Generic;
using UnityEngine;

public class BlackhatAI : MonoBehaviour
{
    public static BlackhatAI inst;
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

    public LightWeightPacket CreateRandomRuleForDestination(TDestination destination)
    {
        LightWeightPacket lwp = new LightWeightPacket();
        lwp.shape = (PacketShape) NewGameMgr.inst.TRandom.Next(0, NewGameMgr.inst.PacketShapes.Count);
        lwp.color = (PacketColor) NewGameMgr.inst.TRandom.Next(0, NewGameMgr.inst.PacketColors.Count);
        lwp.size =  (PacketSize) NewGameMgr.inst.TRandom.Next(0, NewGameMgr.inst.PacketSizes.Count);
        lwp.destination = null;
        return lwp;
    }

    public LightWeightPacket CreateNonMaliciousPacketRuleForDestination(TDestination destination) //SetCurrentPacketRule()
    {
        LightWeightPacket lwp = CreateRandomRuleForDestination(destination);
        while(lwp.isEqual(destination.MaliciousRule)) lwp = CreateRandomRuleForDestination(destination); //any packet except the malicious packet
        return lwp;
    }



    public LightWeightPacket CreateMaliciousPacketRuleForDestination(TDestination destination = null)
    {
        LightWeightPacket lwp = CreateRandomRuleForDestination(destination);
        InstrumentMgr.inst.AddRecord(TaiserEventTypes.SetNewMaliciousRule.ToString(), destination.inGameName); // For each destination
        EffectsMgr.inst.NewRule(destination, lwp);
        //NewAudioMgr.inst.source.PlayOneShot(NewAudioMgr.inst.MaliciousRuleChanged);
        return lwp;
    }

}

