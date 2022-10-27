using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

//-------------------------------------------------------------------------------------------
[System.Serializable]
public class LightWeightPacket
{
    public PacketSize size;
    public PacketColor color;
    public PacketShape shape;
    public TDestination destination = null;

    public LightWeightPacket(LightWeightPacket lwp = null)
    {
        if(lwp != null)
            copy(lwp);
    }

    public bool isMalicious
    {
        get {
            return isEqual(destination.MaliciousRule);
        }
    }

    public bool isEqual(LightWeightPacket other)
    {
        return (color == other.color && shape == other.shape && size == other.size);
    }
    public override string ToString()
    {
        return "" + size.ToString() + ", " + color.ToString() + ", " + shape.ToString() + 
            (destination == null ? "." :  ", " + destination.inGameName);
    }

    public void copy(LightWeightPacket other)
    {
        color = other.color;
        shape = other.shape;
        size = other.size;
        destination = other.destination;
    }
}
//-------------------------------------------------------------------------------------------
[System.Serializable]
public class TPath
{
    public TSource source;
    public TDestination destination;
    public List<Waypoint> waypoints;
}
//-------------------------------------------------------------------------------------------
[System.Serializable]
public class SourcePathDebugMap
{
    public TSource source;
    public List<TPath> paths;
}
//-------------------------------------------------------------------------------------------

[System.Serializable]
public enum Difficulty
{
    Novice = 0,
    Intermediate,
    Advanced
}

[System.Serializable]
public class DifficultyParameters
{
    public Difficulty levelName;
    public int initTime;
    public int meanTimeInterval;
    public int timeSpread;
}


public enum ParameterNames
{
    MaxWaves = 0,
    Penalty,
    MaliciousPacketProbability,
    IntervalBetweenPackets,
    MaxNumberOfPackets,
    MinIntervalBetweenRuleChanges,
    MaxIntervalBetweenRuleChanges,
    //
    AICorrectProbability,
    HumanCorrectProbability,
    MinHumanAdviceTimeInSeconds,
    MaxHumanAdviceTimeInSeconds,
    MinAIAdviceTimeInSeconds,
    MaxAIAdviceTimeInSeconds,
    //
    AIRandomSeed,
    HumanRandomSeed,
}

[System.Serializable]
public class ParameterHolder
{
    public ParameterNames parameterName;
    public float parameterValue = -1;
}

//-------------------------------------------------------------------------------------------
public class NewGameMgr : MonoBehaviour
{

    public static NewGameMgr inst;
    private void Awake()
    {
        inst = this;
        GatherSourcesDestinations();
    }

    // Start is called before the first frame update
    void Start()
    {
        Initialize();
        

    }
    public void Initialize()
    {
        InitSizeColorDictionary();
        TRandom = new System.Random(RandomSeed);
        HidePrototypes();

        ReadGameParametersFromServer();
        StartWave();

    }
    
    public List<ParameterHolder> Parameters = new List<ParameterHolder>();

    public void ReadGameParametersFromServer()
    {
        string content = Utils.inst.ReadFileFromServer("Parameters.csv");
        StartCoroutine("WaitAndExtractParamsFromString");
    }

    IEnumerator WaitAndExtractParamsFromString()
    {
        yield return new WaitForSeconds(2.0f);//Can do better than this.
        Parameters.Clear();
        using(StringReader sr = new StringReader(Utils.inst.FileContent)) {
            string line;
            while((line = sr.ReadLine()) != null) {
                string[] cells = line.Split(',');
                ParameterHolder ph = new ParameterHolder();
                ph.parameterName = (ParameterNames) int.Parse(cells[0].Trim());
                ph.parameterValue = float.Parse(cells[2].Trim());
                Parameters.Add(ph);
            }
        }
        if(Parameters.Count > 0) 
            SetGameParameters();
    }
    
    public void SetGameParameters()
    {
        foreach(ParameterHolder ph in Parameters) {
            switch(ph.parameterName) {
                case ParameterNames.MaxWaves:
                    maxWaves = (int) ph.parameterValue;
                    break;
                case ParameterNames.Penalty:
                    penalty = (int) ph.parameterValue;
                    break;
                case ParameterNames.MaliciousPacketProbability:
                    foreach(TSource source in Sources) {
                        source.malPacketProbability = ph.parameterValue;
                    }
                    break;
                case ParameterNames.IntervalBetweenPackets:
                    Sources[0].timeInterval = ph.parameterValue;
                    Sources[1].timeInterval = ph.parameterValue * 2;
                    Sources[2].timeInterval = ph.parameterValue * 2;
                    break;
                case ParameterNames.MaxNumberOfPackets:
                    Sources[0].maxPackets = (int) ph.parameterValue;
                    Sources[1].maxPackets = (int) ph.parameterValue / 2;
                    Sources[2].maxPackets = (int) ph.parameterValue / 2;
                    break;
                case ParameterNames.MinIntervalBetweenRuleChanges:
                    foreach(TDestination destination in Destinations) {
                        destination.minTimeInterval = (int) ph.parameterValue;
                    }
                    break;
                case ParameterNames.MaxIntervalBetweenRuleChanges:
                    foreach(TDestination destination in Destinations) {
                        destination.maxTimeInterval = (int) ph.parameterValue;
                    }
                    break;
                //----------------------------------------------------
                case ParameterNames.AICorrectProbability:
                    RuleSpecButtonMgr.inst.AICorrectAdviceProbability = ph.parameterValue;
                    break;
                case ParameterNames.HumanCorrectProbability:
                    RuleSpecButtonMgr.inst.HumanCorrectAdviceProbability = ph.parameterValue;
                    break;
                case ParameterNames.MinHumanAdviceTimeInSeconds:
                    RuleSpecButtonMgr.inst.MinHumanTime = ph.parameterValue;
                    break;
                case ParameterNames.MaxHumanAdviceTimeInSeconds:
                    RuleSpecButtonMgr.inst.MaxHumanTime = ph.parameterValue;
                    break;
                case ParameterNames.MinAIAdviceTimeInSeconds:
                    RuleSpecButtonMgr.inst.MinAITime = ph.parameterValue;
                    break;
                case ParameterNames.MaxAIAdviceTimeInSeconds:
                    RuleSpecButtonMgr.inst.MaxAITime = ph.parameterValue;
                    break;
                //----------------------------------------------------
                case ParameterNames.AIRandomSeed:
                    RuleSpecButtonMgr.inst.AIRandomizerSeed = (int) ph.parameterValue;//4321
                    break;
                case ParameterNames.HumanRandomSeed:
                    RuleSpecButtonMgr.inst.HumanRandomizerSeed = (int) ph.parameterValue;//6789
                    break;

                default:
                    Debug.Log("Unknown game parameter name: " + ph.parameterName + ": " + ph.parameterValue);
                    break;
            }
        }
    }

    [ContextMenu("TestFlip")]
    public void TestFlip()
    {
        int count = 0;
        int max = 100;
        for(int i = 0; i < max; i++) {
            if(Flip(0.8f)) count++;
        }
        Debug.Log("Flip Prob: " + count / (float) max);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateScore();
        if(endedSources >= Sources.Count) { //}|| isCorrectIndex >= isCorrectList.Count) {
            EndWave();
        }

        if(UnityEngine.InputSystem.Keyboard.current.f10Key.wasReleasedThisFrame)
            Time.timeScale = 1.0f - Time.timeScale;

            //if(State == GameState.InWave) {
            //BlackhatAI.inst.DoWave();
            //}
            //if(State == GameState.FlushingSourcesToEndWave)
            //if(Sources[0].gameObject.GetComponentsInChildren<TPacket>().Length <= 0)
            //EndWave();

        }

    //-------------------------------------------------------------------------------------------------
    /// <summary>
    /// Deprecated
    /// </summary>
    /*
     * public Difficulty difficulty;
    public List<DifficultyParameters> difficultyParamaters = new List<DifficultyParameters>();
    public void SetDifficulty(Difficulty level)
    {
        Debug.Log("Setting difficulty to: " + level);
        difficulty = level;
        DifficultyParameters parms = difficultyParamaters.Find(x => x.levelName == level);
        foreach(TDestination destination in Destinations) {
            destination.minTimeInterval = parms.meanTimeInterval;
            destination.maxTimeInterval = parms.timeSpread;
            destination.initTime = parms.initTime;
        }

    }
    */
    //-------------------------------------------------------------------------------------------------


    public int RandomSeed = 1234;
    //public float AICorrectAdviceProbability = 0.8f;
    public System.Random TRandom;

    //----------------------------------------------------------------------------------------------------
    /// <summary>
    /// Returns true with probability prob
    /// </summary>
    /// <param name="prob">Probability of returning true</param>
    /// <returns></returns>
    public bool Flip(float prob)
    {
        return (TRandom.NextDouble() < prob);
    }



    //----------------------------------------------------------------------------------------------------
    public int maxWaves = 3;
    public int currentWaveNumber = 0;

    public void StartWave()
    {
        State = GameState.WaveStart;
        //SetDifficulty(NewLobbyMgr.gameDifficulty);

        Debug.Log("Startwave: " + currentWaveNumber);
        InstrumentMgr.inst.AddRecord(TaiserEventTypes.StartWave.ToString());
        SetWaveNumberEffect(Color.green);
        CountdownLabel.text = timerSecs.ToString("0");
        InvokeRepeating("CountdownLabeller", 0.1f, 1.1f);
    }

    int timerSecs = 5;
    public Text CountdownLabel;

    void CountdownLabeller()
    {
        //Debug.Log("Calling invoke repeating: timesecs: " + timerSecs);
        if(timerSecs <= 0) {
            CancelInvoke("CountdownLabeller");
            State = GameState.InWave;
            StartWaveAtSources();
            StartWaveAtDestinations();
            timerSecs = 5;
        } else {
            NewAudioMgr.inst.PlayOneShot(NewAudioMgr.inst.Countdown);
            timerSecs -= 1;
            CountdownLabel.text = timerSecs.ToString("0");
        }
        
    }

    public void StartWaveAtSources()
    {
        foreach(TSource ts in Sources) {
            ts.StartWave();
        }
    }

    public void StartWaveAtDestinations()
    {
        foreach(TDestination destination in Destinations) {
            destination.StartWave();
        }
    }

    public void EndWaveAtSources()
    {
        foreach(TSource ts in Sources) {
            ts.EndWave();
        }

    }

    public void EndWaveAtDestinations()
    {
        foreach(TDestination destination in Destinations) {
            destination.EndWave();
        }

    }

    void PauseDestinationsMaliciousRuleCreationClocks()
    {
        foreach(TDestination destination in Destinations) {
            destination.PauseMaliciousClock();
        }
    }

    void UnPauseDestinationsMaliciousRuleCreationClocks()
    {
        foreach(TDestination destination in Destinations) {
            destination.UnPauseMaliciousClock();
        }
    }
    /// <summary>
    /// Before you end a wave, make sure that each source has called EndSpawningAtSource
    /// by counting number of times this method is called by sources.
    /// If this is > number of sources, reset to 0 and actually end the wave
    /// </summary>
    public int endedSources = 0;
    public void EndSpawningAtSources()
    {
        endedSources += 1;
        if(endedSources >= Sources.Count) {
            endedSources = 0; // not needed, done in EndWave->ResetVars()
            EndWave();
        }
    }

    public void ResetVars()
    {
        endedSources = 0;
        //isCorrectIndex = 0;
    }

    public Text VictoryOrDefeatText;
    public Text AnotherWaveAwaitsMessageText;
    public void EndWave()
    {
        //Debug.Log("Ending Wave: " + currentWaveNumber + ", isCorrectIndex: " + isCorrectIndex + ", endedSrcs: " + endedSources);
        Debug.Log("Ending Wave: " + currentWaveNumber + ", endedSrcs: " + endedSources);
        State = GameState.WaveEnd;
        ResetVars();
        SetWaveEndScores();
        EndWaveAtSources();
        //We call EndWaveAtDestinations in WaitToStartNextWave to give packets time to reach destinations
        PauseDestinationsMaliciousRuleCreationClocks();

        if(WhitehatScore > BlackhatScore) { 
            VictoryOrDefeatText.text = "Victory!";
            NewAudioMgr.inst.PlayOneShot(NewAudioMgr.inst.Winning, 1.0f);
        } else {
            VictoryOrDefeatText.text = "Defeat!";
            NewAudioMgr.inst.PlayOneShot(NewAudioMgr.inst.Losing, 5.0f);
        }
        currentWaveNumber += 1;
        if(currentWaveNumber >= maxWaves)
            AnotherWaveAwaitsMessageText.text = "You are done for now. Take a break.";
        else
            AnotherWaveAwaitsMessageText.text = "Get Ready for Wave " + (1+currentWaveNumber).ToString("0")
                + " of " + maxWaves.ToString("0");

        Invoke("WaitToStartNextWave", 5f);
    }

    public RectTransform blackhatBarPanel;
    public Vector3 waveEndBlackhatScoreScaler = Vector3.one;
    public RectTransform whiteHatBarPanel;
    public Vector3 waveEndWhitehatScoreScaler = Vector3.one;
    public void SetWaveEndScores()
    {
        //SetScores(BlackhatScore, WhitehatScore);
        //waveEndBlackhatScoreScaler.y = BlackhatScore;
        //blackhatBarPanel.localScale = waveEndBlackhatScoreScaler;
        //waveEndWhitehatScoreScaler.y = WhitehatScore;
        //whiteHatBarPanel.localScale = waveEndWhitehatScoreScaler;

        WhitehatSlider.maxValue += 50;
        BlackhatSlider.maxValue += 50;


    }

    public List<RectTransform> WaveNumberIndicatorCirclePanels = new List<RectTransform>();
    public RectTransform WaveNumberIndicatorRoot;

    [ContextMenu("FindWaveNumberIndicators")]
    public void FindWaveNumberIndicators()
    {
        WaveNumberIndicatorCirclePanels.Clear();
        foreach(RectTransform rt in WaveNumberIndicatorRoot.GetComponentsInChildren<RectTransform>()) {
            WaveNumberIndicatorCirclePanels.Add(rt);
        }
    }

    public void SetWaveNumberEffect(Color col)
    {
        WaveNumberIndicatorCirclePanels[currentWaveNumber].GetComponent<Image>().color = col;
    }

    void WaitToStartNextWave()
    {
        EndWaveAtDestinations(); //Give a chance for all packets to get to destinations
        SetWaveEndScores();
        Debug.Log("Waiting to start next wave: " + currentWaveNumber);
        if(currentWaveNumber < maxWaves) {
            StartWave();//Startwave unpauses destination clocks
        } else {
            InstrumentMgr.inst.WriteSession();
            State = GameState.Menu;
            ResetGame();
        }
    }

    void ResetGame()
    {
        foreach(TDestination destination in Destinations) {
            destination.Reset();
        }
        foreach(TSource source in Sources) {
            source.Reset();
        }
        penaltyCount = 0;
    }

    

    //----------------------------------------------------------------------------------------------------
    public List<GameObject> ToHide = new List<GameObject>();
    public void HidePrototypes()
    {
        foreach(GameObject go in ToHide) {
            foreach(MeshRenderer mr in go.GetComponentsInChildren<MeshRenderer>()) {
                if(!mr.gameObject.name.Contains("CubePath"))
                    mr.enabled = false;
            }
        }
    }
    
  
    //public void PrintSourceD(Dictionary<TSource, List<TPath>> spd)
    //{
    //    foreach(TSource key in spd.Keys) {
    //        List<TPath> paths = spd[key];
    //        foreach(TPath path in paths) {
    //            Debug.Log("Path: source: " + path.source.myId + ", dest: " + path.destination.myId
    //                + ", wpCount: " + path.waypoints.Count);
    //        }
    //    }
    //}

    //-------------------------------------------------------------------------------------
    //-------Packet properties for all packets
    public List<PacketShape> PacketShapes = new List<PacketShape>();
    public List<PacketColor> PacketColors = new List<PacketColor>();
    public List<PacketSize> PacketSizes = new List<PacketSize>();

    public Color blue;
    public Color green;
    public Color pink;
    public Dictionary<PacketColor, Color> ColorVector = new Dictionary<PacketColor, Color>();

    public Vector3 smallScale = new Vector3(0.3f, 0.3f, 0.3f);
    public Vector3 mediumScale = new Vector3(0.7f, 0.7f, 0.7f);
    public Vector3 largeScale = Vector3.one;
    public Dictionary<PacketSize, Vector3> SizesVector = new Dictionary<PacketSize, Vector3>();
    public void InitSizeColorDictionary()
    {
        SizesVector.Clear();
        SizesVector.Add(PacketSize.Small, smallScale);
        SizesVector.Add(PacketSize.Medium, mediumScale);
        SizesVector.Add(PacketSize.Large, largeScale);
        ColorVector.Clear();
        ColorVector.Add(PacketColor.Blue, blue);
        ColorVector.Add(PacketColor.Green, green);
        ColorVector.Add(PacketColor.Pink, pink);
    }

    public Color testColor;
    [ContextMenu("TestSetColor")]
    public void TestSetColor()
    {
        transform.GetComponentInChildren<Renderer>().material.color = testColor;
    }

    public Vector3 XOrientation = Vector3.zero;
    public Vector3 ZOrientation = new Vector3(0, 90, 0);

    //---------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------

    //public int NumberOfPaths = 1;
    public List<TPath> Paths = new List<TPath>();

    public List<TSource> Sources = new List<TSource>();
    public List<TDestination> Destinations = new List<TDestination>();


    public GameObject SourcesRoot;
    public GameObject DestinationsRoot;

    [ContextMenu("GatherSourcesDestinations")]
    public void GatherSourcesDestinations()
    {
        Sources.Clear();
        int index = 0;
        foreach(TSource ts in SourcesRoot.GetComponentsInChildren<TSource>()) {
            Sources.Add(ts);
            ts.myId = index++;
        }
        Destinations.Clear();
        index = 0;
        foreach(TDestination td in DestinationsRoot.GetComponentsInChildren<TDestination>()) {
            Destinations.Add(td);
            td.myId = index++;
        }
        SetSourcePaths();//must do this every time something changes in 
        //sources or destinations so making it an automatic call
    }
    public List<SourcePathDebugMap> SourcePathDebugList = new List<SourcePathDebugMap>();
    public Dictionary<TSource, List<TPath>> SourcePathDictionary = new Dictionary<TSource, List<TPath>>();
    [ContextMenu("SetSourcePaths")]
    public void SetSourcePaths()
    {
        SourcePathDebugList.Clear();//Lists show in editor for debugging
        SourcePathDictionary.Clear();
        foreach(TSource ts in Sources) {
            if(IsSourceInPaths(ts)) { 
                SourcePathDebugMap spl = new SourcePathDebugMap();
                spl.source = ts;
                spl.paths = new List<TPath>();
                SourcePathDebugList.Add(spl);

                SourcePathDictionary.Add(ts, new List<TPath>());
            }
        }
        foreach(TPath tp in Paths) {
            SourcePathDebugList.Find(s => s.source.myId == tp.source.myId).paths.Add(tp);
            SourcePathDictionary[tp.source].Add(tp);
        }
        //
    }

    public bool IsSourceInPaths(TSource ts)
    {
        foreach(TPath path in Paths) {
            if(path.source.myId == ts.myId)
                return true;
        }
        return false;
    }
    //-------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------

    //-------------------------------------------------------------------------------------
    public enum GameState
    {
        Start = 0,
        WaveStart,
        InWave,
        FlushingSourcesToEndWave,
        WaveEnd,
        PacketExamining,
        BeingAdvised,
        Menu,
        Paused,
        None
    }
    public TaiserPanel PacketExaminerPanel;
    public TaiserPanel StartPanel;
    public TaiserPanel WatchingPanel;
    public TaiserPanel WaveStartPanel;
    public TaiserPanel WaveEndPanel;
    public TaiserPanel MenuPanel;

    public GameState _state;
    public GameState PriorState;
    public GameState State
    {
        get { return _state; }
        set
        {
            PriorState = _state;
            _state = value;

            WaveStartPanel.isVisible = (_state == GameState.WaveStart);
            WaveEndPanel.isVisible = (_state == GameState.WaveEnd);

            PacketExaminerPanel.isVisible = (_state == GameState.PacketExamining);
            StartPanel.isVisible = (_state == GameState.Start);
            WatchingPanel.isVisible = (_state == GameState.InWave || _state == GameState.FlushingSourcesToEndWave);
            ScorePanel.gameObject.SetActive(_state == GameState.WaveEnd
                || _state == GameState.InWave
                || _state == GameState.FlushingSourcesToEndWave);
            //add menu panel
            MenuPanel.isVisible = (_state == GameState.Menu);

            if(_state != GameState.PacketExamining)
                UnExamineAllDestinations();

        }
    }
    //-------------------------------------------------------------------------------------

    public void UnExamineAllDestinations()
    {
        foreach(TDestination destination in Destinations) {
            destination.isBeingExamined = false;
        }
    }

    

    public void ApplyFirewallRule(TDestination destination, LightWeightPacket packet, bool isAdvice)
    {
        if(packet == null) return; //------------------------------------------

        destination.FilterOnRule(packet);

        if(packet.isEqual(destination.MaliciousRule)) {
            if(isAdvice)
                InstrumentMgr.inst.AddRecord(TaiserEventTypes.AdvisedFirewallCorrectAndSet.ToString());
            else
                InstrumentMgr.inst.AddRecord(TaiserEventTypes.UserBuiltFirewallCorrectAndSet.ToString());
            EffectsMgr.inst.GoodFilterApplied(destination, packet);
            //NewAudioMgr.inst.PlayOneShot(NewAudioMgr.inst.GoodFilterRule);
        } else {
            if(isAdvice)
                InstrumentMgr.inst.AddRecord(TaiserEventTypes.AdvisedFirewallIncorrectAndSet.ToString());
            else
                InstrumentMgr.inst.AddRecord(TaiserEventTypes.UserBuiltFirewallIncorrectAndSet.ToString());
            EffectsMgr.inst.BadFilterApplied(destination, packet);
            if(shouldApplyPenalty)
                ApplyScorePenalty();
            //NewAudioMgr.inst.source.PlayOneShot(NewAudioMgr.inst.BadFilterRule);
        }

        destination.isBeingExamined = false;
        State = GameState.InWave;
    }

    //-------------------------------------------------------------------------------------
    //public RectTransform WhitehatWatchingScorePanel;
    //public RectTransform BlackhatWatchingScorePanel;
    public float minScaley = 0.0f;
    public void SetScores(float blackhatScore, float whitehatScore)
    {
        //SetBars(BlackhatWatchingScorePanel, blackhatScore, WhitehatWatchingScorePanel, whitehatScore);
        SetSliders();

    }

    public RectTransform ScorePanel;
    public Slider WhitehatSlider;
    public Text WhitehatCountText;
    public Slider BlackhatSlider;
    public Text BlackhatCountText;

    public Slider WaveProgressSlider;
    public void SetSliders()
    {
        WhitehatSlider.value = totalMaliciousFilteredCount;
        WhitehatCountText.text = totalMaliciousFilteredCount.ToString("00");
        BlackhatSlider.value = (int) BlackhatScore; // totalMaliciousUnFilteredCount;
        BlackhatCountText.text = ((int) BlackhatScore).ToString("00");//totalMaliciousUnFilteredCount.ToString("00");
        WaveProgressSlider.value = Sources[0].packetCount / (float) Sources[0].maxPackets;

    }

    public Vector3 inWaveWhitehatScaler = Vector3.one;
    public Vector3 inWaveBlackhatScaler = Vector3.one;
    public void SetBars(RectTransform blackhatBarPanel, float blackhatScore, 
        RectTransform whitehatBarPanel, float whitehatScore)
    {
        inWaveWhitehatScaler.x = whitehatScore + minScaley;
        whitehatBarPanel.localScale = inWaveWhitehatScaler;
        inWaveBlackhatScaler.x = blackhatScore + minScaley;
        blackhatBarPanel.localScale = inWaveBlackhatScaler;
    }

    public float WhitehatScore = 0;
    public float BlackhatScore = 0;

    public int totalMaliciousCount;
    public int totalMaliciousFilteredCount; //over all destinations
    public int totalMaliciousUnFilteredCount; //over all destinations

    public void UpdateScore()
    {
        totalMaliciousFilteredCount = 0;
        totalMaliciousCount = 0;
        totalMaliciousUnFilteredCount = 0;
        foreach(TDestination destination in Destinations) {
            totalMaliciousFilteredCount += destination.maliciousFilteredCount;
            totalMaliciousUnFilteredCount += destination.maliciousUnfilteredCount; //is also malicious - filtered
            totalMaliciousCount += destination.maliciousCount;
        }

        //WhitehatScore = totalMaliciousFilteredCount / (totalMaliciousCount + 0.000001f);
        //BlackhatScore = totalMaliciousUnFilteredCount / (totalMaliciousCount + 0.000001f);
        WhitehatScore = totalMaliciousFilteredCount;
        BlackhatScore = totalMaliciousUnFilteredCount + (penalty * penaltyCount);
        SetScores(BlackhatScore, WhitehatScore);
    }

    public bool shouldApplyPenalty = true;
    public int penalty = 20;
    public int penaltyCount = 0;
    public RectTransform BlackFillArea;
    public void ApplyScorePenalty()
    {
        penaltyCount++;
        SetSliders();
        WhitehatSlider.maxValue += penalty/2;
        BlackhatSlider.maxValue += penalty/2;
        BlackFillArea.GetComponent<Image>().color = Color.red;
        StartCoroutine(ToggleFillColor());
    }

    public bool colorToggle = true;

    IEnumerator ToggleFillColor()
    {
        for(int i = 0; i < penalty; i++) {
            if(colorToggle) {
                BlackFillArea.GetComponent<Image>().color = Color.red;
            } else {
                BlackFillArea.GetComponent<Image>().color = Color.black;
            }
            colorToggle = !colorToggle;
            yield return new WaitForSeconds(0.25f);
        }
    }

    //-------------------------------------------------------------------------------------
    public void OnMenuBackButton()
    {
        State = PriorState;
    }

    public void QuitToWindows()
    {
        Application.Quit();
    }
    public void QuitRoom()
    {
        //InstrumentMgr.inst.WriteSession();
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void OnMenuButtonClicked()
    {
        InstrumentMgr.inst.AddRecord(TaiserEventTypes.Menu.ToString());
        State = GameState.Menu;
    }

   



    public TPath FindRandomPath(TSource source)
    {
        //SourcePathDebugMap map = SourcePathDebugList.Find(p => source.myId == p.source.myId);
        //Debug.Log("Paths for source: " + source.myId + ", path count: " + map.paths.Count);
        //int index = TRandom.Next(0, map.paths.Count);
        //return map.paths[index];

        int index = TRandom.Next(0, SourcePathDictionary[source].Count);//Must ensure key exists
        return (SourcePathDictionary[source])[index]; //dictionary access
    }

    //----------------------------------------------------------------------------------------------------
    //----------------------------------------------------------------------------------------------------

}
