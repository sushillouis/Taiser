using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TSource : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public int myId;
    public string gameName;
    public void SpawnPacket(LightWeightPacket lwp)
    {
        //if(lwp.isMalicious)
        //Debug.Log("Source: " + myId + " Spawning Malicious packet: " + lwp.ToString());
        TPath path = NewGameMgr.inst.FindRandomPath(this);
        TPacket tp = NewEntityMgr.inst.CreatePacket(lwp.shape, lwp.color, lwp.size);//from pool

        tp.transform.parent = this.transform;
        tp.InitPath(path); // set heading changes at waypoints
        if(tp.NextHeadings[0] == Vector3.zero)
            Debug.LogError("No path for this packet: " + tp.Pid);
        tp.SetNextVelocityOnPath(); //start moving

    }

    public bool Equals(TSource tsA, TSource tsB)
    {
        return tsA.myId == tsB.myId;
    }
    public int GetHashCode(TSource x)
    {
        return x.myId.GetHashCode();
    }
}
