using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//malicious ==> mal

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
        SetCurrentPacketRule();
        SetMalPacketRule();
        //InitScoring();
        //AudioManager.instance.uiSoundFXPlayer.PlayTrackImmediate("SettingsUpdated");
        
    }


    public void DoWave()
    {
        if(packetCount < 300) {
            CheckChangeMalRule2();
            foreach(TSource src in NewGameMgr.inst.Sources) { //only valid sources
                if(Flip(spawnProbabilityPerSource)) {
                    if(Flip(malPacketProbability)) {
                        SpawnMalPacket(src);
                    } else {
                        SpawnRandomPacket(src);
                    }
                    packetCount++;
                }
            }
        } else {
            EndWave();//empty all sources before you declare end of wave
        }
    }
    public void StartWave()
    {
        packetCount = 0;
        SetMalPacketRule();
        InstrumentMgr.inst.AddRecord(TaiserEventTypes.StartWave.ToString());
    }

    public void EndWave()
    {
        InstrumentMgr.inst.AddRecord(TaiserEventTypes.EndWave.ToString());
        NewGameMgr.inst.State = NewGameMgr.GameState.FlushingSourcesToEndWave;
    }
    //-----------------------------------------------------------------------------------------
    //-----------------------------------------------------------------------------------------
    public float spawnProbabilityPerSource;
    public int perWaveMaxSpawns;
    public float malPacketProbability;

    public LightWeightPacket maliciousRule = new LightWeightPacket();
    public LightWeightPacket currentRule = new LightWeightPacket();
    public List<int> malSizeWeights;
    public List<int> malColorWeights;
    public List<int> malShapeWeights;
    
    public bool Flip (float prob)
    {
        return (NewGameMgr.inst.TRandom.NextDouble() < prob);
    }

    public int packetCount = 0;
    
    //// Update is called once per frame
    void Update()
    {
        //DoWave();

    }


    public LightWeightPacket CreateRandomRule()
    {
        LightWeightPacket lwp = new LightWeightPacket();
        lwp.shape = (PacketShape) NewGameMgr.inst.TRandom.Next(0, NewGameMgr.inst.PacketShapes.Count);
        lwp.color = (PacketColor) NewGameMgr.inst.TRandom.Next(0, NewGameMgr.inst.PacketColors.Count);
        lwp.size =  (PacketSize) NewGameMgr.inst.TRandom.Next(0, NewGameMgr.inst.PacketSizes.Count);
        //lwp.isMalicious = false;
        return lwp;
    }

    public void SetCurrentPacketRule()
    {
        LightWeightPacket lwp = CreateRandomRule();
        while(lwp.isEqual(maliciousRule)) lwp = CreateRandomRule(); //any packet except the malicious packet
        currentRule = lwp; //shallow copy
    }


    public void SetMalPacketRule()
    {
        maliciousRule = CreateRandomRule(); //shallow copy
        PacketButtonMgr.inst.ResetHighlightColor();
        InstrumentMgr.inst.AddRecord(TaiserEventTypes.SetNewMaliciousRule.ToString());
        //maliciousRule.isMalicious = true; // bad rule, bad, bad
    }

    public void SpawnMalPacket(TSource src)
    {
        //SetMalPacketRule();
        src.SpawnPacket(maliciousRule);

    }

    public void SpawnRandomPacket(TSource src)
    {
        SetCurrentPacketRule();
        src.SpawnPacket(currentRule);
    }

    public int totalCurrentFilteredThreshold;
    public int threshold = 20;
    public void CheckChangeMalRule2()
    {
        if(NewGameMgr.inst.totalMaliciousFilteredCount > totalCurrentFilteredThreshold) {
            SetMalPacketRule();
            totalCurrentFilteredThreshold += threshold;
            Debug.Log("Blackhat: Changed mal Rule: " + maliciousRule.ToString());
            NewAudioMgr.inst.source.PlayOneShot(NewAudioMgr.inst.MaliciousRuleChanged);
        }
    }



    //public void InitScoring()
    //{
    //    currentFilteredThreshold.Clear();
    //    blackhatScores.Clear();
    //    whitehatScores.Clear();
    //    foreach(TDestination dest in NewGameMgr.inst.Destinations) {
    //        currentFilteredThreshold.Add(threshold);
    //        blackhatScores.Add(0);
    //        whitehatScores.Add(0);
    //    }
    //}

    //public List<int> currentFilteredThreshold = new List<int>();


    //public List<float> blackhatScores = new List<float>();
    //public List<float> whitehatScores = new List<float>();

}



