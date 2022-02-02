using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RuleSpecButtonMgr : MonoBehaviour
{
    public static RuleSpecButtonMgr inst;
    public void Awake()
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
    
    [System.Serializable]
    public class ButtonList
    {
        public List<Button> buttons = new List<Button>();
    }

    public GameObject ButtonsRoot;
    public List<ButtonList> RuleButtons2d = new List<ButtonList>();
    public int nrows = 3;
    public int ncols = 3;

    [ContextMenu("SetupButtons")]
    public void SetupButtons()
    {
        RuleButtons2d.Clear();
        for(int i = 0; i < nrows; i++) {
            RuleButtons2d.Add(new ButtonList());
        }
        int row = 0;
        int index = 0;
        foreach(Button b in ButtonsRoot.GetComponentsInChildren<Button>()) {
            string bt = b.GetComponentInChildren<Text>().text.ToLower();
            if(bt != "Size".ToLower() && bt != "Color".ToLower() && bt != "Shape".ToLower()) {
                row = index / nrows;
                RuleButtons2d[row].buttons.Add(b);
                index++;
            }
        }
    }

    public TDestination CurrentDestination;//Set by NewGameMgr OnAttackableDestinationClicked
    public LightWeightPacket RuleSpecFromPlayer = new LightWeightPacket();
    public void OnSizeClick(int size)
    {
        RuleSpecFromPlayer.size = (PacketSize) size;
    }
    public void OnColorClick(int color)
    {
        RuleSpecFromPlayer.color = (PacketColor) color;
    }
    public void OnShapeClick(int shape)
    {
        RuleSpecFromPlayer.shape = (PacketShape) shape;
    }

    //public void DepApplyUserRule(LightWeightPacket lwp)
    //{
    //    CurrentDestination.FilterOnRule(lwp);
    //}

    /// <summary>
    /// Called from TaiserInGameStartPanel from SetFirewall button
    /// </summary>
    public void ApplyCurrentUserRule()
    {
        CurrentDestination.FilterOnRule(RuleSpecFromPlayer);
        //CurrentDestination.ResetButton();
        CurrentDestination.ResetMaliciousCube(RuleSpecFromPlayer);
        NewGameMgr.inst.State = NewGameMgr.GameState.InWave;
    }

}
