using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//----------------------------------------------------
//should use csv helper
//https://joshclose.github.io/CsvHelper/
//----------------------------------------------------

[System.Serializable]
public class TaiserSession
{
    public string name;
    public Role role;
    public System.DateTime dayAndTime;
    public float whitehatScore;
    public float blackhatScore;
    public List<TaiserRecord> records;
}

[System.Serializable]
public class TaiserRecord
{
    /// <summary>
    /// From start of game
    /// </summary>
    public float secondsFromStart;
    public string eventName;
    public List<string> eventModifiers;
}

public class InstrumentMgr : MonoBehaviour
{
    public static InstrumentMgr inst;
    public void Awake()
    {
        inst = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        CreateOrFindTaiserFolder();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public string TaiserFolder;
    public string sessionFilename;

    public void CreateOrFindTaiserFolder()
    {
        TaiserFolder = System.IO.Path.Combine(Application.dataPath, "Taiser");
        System.IO.Directory.CreateDirectory(TaiserFolder);
    }

    public void AddRecord(string eventName, List<string> modifiers)
    {

    }
}
