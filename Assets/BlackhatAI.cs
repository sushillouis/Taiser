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
        InitScoring();
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
    }

    public void EndWave()
    {
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

    public int totalMaliciousCount;
    public int totalMaliciousFilteredCount; //over all destinations
    public int totalMaliciousUnFilteredCount; //over all destinations
    public int totalCurrentFilteredThreshold;



    public void CheckChangeMalRule2()
    {
        totalMaliciousFilteredCount = 0;
        totalMaliciousCount = 0;
        totalMaliciousUnFilteredCount = 0;
        foreach(TDestination destination in NewGameMgr.inst.Destinations) {
            totalMaliciousFilteredCount += destination.maliciousFilteredCount;
            totalMaliciousUnFilteredCount += destination.maliciousUnfilteredCount; //is also malicious - filtered
            totalMaliciousCount += destination.maliciousCount;
        }
        if(totalMaliciousFilteredCount > totalCurrentFilteredThreshold) {
            SetMalPacketRule();
            totalCurrentFilteredThreshold += threshold;
            Debug.Log("Blackhat: Changed mal Rule: " + maliciousRule.ToString());
            NewAudioMgr.inst.source.PlayOneShot(NewAudioMgr.inst.MaliciousRuleChanged);
        }
        wscore = totalMaliciousFilteredCount / (totalMaliciousCount + 1f);
        bscore = totalMaliciousUnFilteredCount / (totalMaliciousCount + 1f);
        NewGameMgr.inst.SetScores(bscore, wscore);

    }

    public void CheckChangeMalRule()
    {
        foreach(TDestination destination in NewGameMgr.inst.Destinations) {
            if(destination.maliciousFilteredCount > currentFilteredThreshold[destination.myId]) {
                SetMalPacketRule();
                currentFilteredThreshold[destination.myId] = destination.maliciousFilteredCount + threshold;
                Debug.Log("Dest: " + destination.gameName + ": Changed mal Rule: " + maliciousRule.ToString());
            }
            blackhatScores[destination.myId] = destination.maliciousUnfilteredCount / (destination.maliciousCount + 1f);
            whitehatScores[destination.myId] = destination.maliciousFilteredCount / (destination.maliciousCount + 1f);
            //+ 1f in denominator to not divide by 0 and get promoted to float result
        }
        bscore = ScoreCombine(blackhatScores);
        wscore = ScoreCombine(whitehatScores);
        NewGameMgr.inst.SetScores(bscore, wscore);
        //NewGameMgr.inst.SetScores(score(blackhatScore), score(whitehatScore));
    }

    public float ScoreCombine(List<float> scores)
    {
        float score = 0;
        foreach(float s in scores) {
            score += s;
        }
        return 100f * score / scores.Count;
    }

    public void InitScoring()
    {
        currentFilteredThreshold.Clear();
        blackhatScores.Clear();
        whitehatScores.Clear();
        foreach(TDestination dest in NewGameMgr.inst.Destinations) {
            currentFilteredThreshold.Add(threshold);
            blackhatScores.Add(0);
            whitehatScores.Add(0);
        }
    }

    public List<int> currentFilteredThreshold = new List<int>();
    public int threshold = 20;

    public List<float> blackhatScores = new List<float>();
    public List<float> whitehatScores = new List<float>();
    public float bscore;
    public float wscore;

}
