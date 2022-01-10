using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Role
{
    Whitehat = 0,
    Blackhat,
    Observer
}

public enum PlayerType
{
    AI = 0,
    Human
}
//later we might add PlayerSide, side1 could have both Whitehats and Blackhats

public class PlayerMgr : MonoBehaviour
{

    public static PlayerMgr inst;
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


}
