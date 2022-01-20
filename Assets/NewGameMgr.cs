using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class LightWeightPacket
{
    public PacketSize size;
    public PacketColor color;
    public PacketShape shape;
    public bool isMalicious
    {
        get { return isEqual(BlackhatAI.inst.maliciousRule); }
        //set { }
    }

    //    {
    //        return isEqual(BlackhatAI.inst.maliciousRule);
    //    }
    public bool isEqual(LightWeightPacket other)
    {
        return (color == other.color && shape == other.shape && size == other.size);
    }
    public override string ToString()
    {
        return "" + size.ToString() + ", " + color.ToString() + ", " + shape.ToString();
    }
    public void copy(LightWeightPacket other)
    {
        color = other.color;
        shape = other.shape;
        size = other.size;
        //isMalicious = other.isMalicious;
    }


}

[System.Serializable]
public class TPath
{
    public TSource source;
    public TDestination destination;
    public List<Waypoint> waypoints;
}

[System.Serializable]
public class SourcePathDebugMap
{
    public TSource source;
    public List<TPath> paths;
}

public class NewGameMgr : MonoBehaviour
{

    public static NewGameMgr inst;
    private void Awake()
    {
        inst = this;
        GatherSources();
    }

    // Start is called before the first frame update
    void Start()
    {
        State = GameState.Start;
        Initialize();

    }
    public void Initialize()
    {
        InitSizeColorDictionary();
        TRandom = new System.Random(RandomSeed);

        State = GameState.Start;
    }
    // Update is called once per frame

    void Update()
    {
        //if(Time.frameCount % spawnInterval == 0 && spawnCount++ < maxSpawns)
        //    InitTest2();

    }
    //----------------------------------------------------------------------------------------------------
    //----------------------------------------------------------------------------------------------------

    int spawnCount = 0;
    public TSource source;
    public TDestination destination;
    /// <summary>
    /// Deprecated hard. No longer works.
    /// </summary>
    public void InitTest()
    {
        if (Time.frameCount % 50 == 0 && spawnCount < 20) {
            TPacket tp = NewEntityMgr.inst.CreatePacket(PacketShape.Cube);
            tp.transform.parent = source.transform;
            tp.InitPath(Paths[0]);
            Debug.Log("Packet from pool: " + tp.Pid);
            tp.transform.parent = Paths[0].source.transform;
            tp.SetNextVelocityOnPath();
            spawnCount += 1;
        }
    }

    public float maliciousFraction = 0.5f;
    int spawnInterval = 10;
    public int RandomSeed = 1234;
    int maxSpawns = 40;
    public System.Random TRandom;

    /// <summary>
    /// Deprecated hard. No longer works. Do not use. 
    /// </summary>
    public void InitTest2() //spawn a wave
    {
        TPacket tp = SpawnRandomPacket();
        spawnCount += 1;
        //Debug.Log("Spawned: " + tp.Shape + ", " + tp.TColor + ", " + tp.Size + ", Bad: " + tp.isMalicious);
        int pathIndex = TRandom.Next(0, Paths.Count);
        TPath tPath = Paths[pathIndex];

        tp.transform.parent = tPath.source.transform;

        tp.InitPath(tPath);//
        tp.SetNextVelocityOnPath();
    }

    //----------------------------------------------------------------------------------------------------
    //----------------------------------------------------------------------------------------------------

    public TPath FindRandomPath(TSource source)
    {
        //SourcePathDebugMap map = SourcePathDebugList.Find(p => source.myId == p.source.myId);
        //Debug.Log("Paths for source: " + source.myId + ", path count: " + map.paths.Count);
        //int index = TRandom.Next(0, map.paths.Count);
        //return map.paths[index];
        
        int index = TRandom.Next(0, SourcePathDictionary[source].Count);//Must ensure key exists
        return (SourcePathDictionary[source])[index]; //dictionary access
    }
    
    public void PrintSourceD(Dictionary<TSource, List<TPath>> spd)
    {
        foreach(TSource key in spd.Keys) {
            List<TPath> paths = spd[key];
            foreach(TPath path in paths) {
                Debug.Log("Path: source: " + path.source.myId + ", dest: " + path.destination.myId
                    + ", wpCount: " + path.waypoints.Count);
            }
        }
    }

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

    public Vector3 XDirection = Vector3.zero;
    public Vector3 ZDirection = new Vector3(0, 90, 0);

    public TPacket SpawnRandomPacket()
    {
        int shapeIndex = TRandom.Next(0, PacketShapes.Count);
        PacketShape shape = PacketShapes[shapeIndex];
        int colorIndex = TRandom.Next(0, PacketColors.Count);
        PacketColor color = PacketColors[colorIndex];
        int sizeIndex = TRandom.Next(0, PacketSizes.Count); 
        PacketSize size = PacketSizes[sizeIndex];

        TPacket tp = NewEntityMgr.inst.CreatePacket(shape, color, size);
        //Cannot test standalone now
        //tp.packet.copy(BlackhatAI.inst.malRule);
        // isMalicious = (TRandom.NextDouble() < maliciousFraction);

        return tp;
    }
    //---------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------

    public int NumberOfPaths = 1;
    public List<TPath> Paths = new List<TPath>();

    public List<TSource> Sources = new List<TSource>();
    public List<TDestination> Destinations = new List<TDestination>();


    public GameObject SourcesRoot;
    public GameObject DestinationsRoot;

    [ContextMenu("GatherSourcesDestinations")]
    public void GatherSources()
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
    //-------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------
    //--- When a packet button in the examining panel is clicked

    public void OnPacketClicked(LightWeightPacket packet)
    {
        DisplayPacketInformation(packet); // expand on this
    }

    public void DisplayPacketInformation(LightWeightPacket packet)
    {
        RuleTextList[0].text = packet.size.ToString();
        RuleTextList[0].fontSize = FontSizes[(int) packet.size];
        RuleTextList[1].text = packet.color.ToString();
        RuleTextList[1].color = TextColors[(int) packet.color];
        RuleTextList[2].text = packet.shape.ToString();
    }
    public List<int> FontSizes = new List<int>();
    public List<Color> TextColors = new List<Color>();

    public List<Text> RuleTextList = new List<Text>();
    public GameObject RuleTextListRoot;
    [ContextMenu("SetupButtonArray")]
    public void SetupButtonArray()
    {
        RuleTextList.Clear();
        foreach(Text t in RuleTextListRoot.GetComponentsInChildren<Text>()) {
            RuleTextList.Add(t);
        }
    }

    //-------------------------------------------------------------------------------------
    //-------------------------------------------------------------------------------------
    // PROBLEM: Double indirection to handle design issues

    public Text FilterRuleSpecTitle;
    public void OnAttackableDestinationClicked(TDestination destination)
    {
        PacketButtonMgr.inst.OnAttackableDestinationClicked(destination); // multiple things are happening
        RuleSpecButtonMgr.inst.CurrentDestination = destination;
        FilterRuleSpecTitle.text = destination.gameName;
        State = GameState.PacketExamining;
    }

    public Text whitehatScoreText;
    public Text blackhatScoreText;
    public void SetScores(float blackhatScore, float whitehatScore)
    {
        whitehatScoreText.text = whitehatScore.ToString("0.0");
        blackhatScoreText.text = blackhatScore.ToString("0.0");
    }

    //-------------------------------------------------------------------------------------
    public void OnBackButton()
    {
        State = PriorState;
    }

    public void OnReady()
    {

        AudioManager.instance.uiSoundFXPlayer.PlayTrackImmediate("SettingsUpdated");

        //if(NetworkingManager.instance)
        //    NetworkingManager.instance.setReady(true);
        //else
        //    GameManager.instance.StartNextWave();

        State = GameState.Watching;
        //InitTest();
    }


}
