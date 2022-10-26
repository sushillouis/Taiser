using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

public class AdminMgr : MonoBehaviour
{
    public static AdminMgr inst;

    private void Awake()
    {
        inst = this;
    }

    public List<AdminSliderPanelHandler> AdminSliderPanelHandlers = new List<AdminSliderPanelHandler>();
    public RectTransform SliderRoot;
    [ContextMenu("AssignSliders")]
    public void AssignSliders()
    {
        AdminSliderPanelHandlers.Clear();
        foreach(AdminSliderPanelHandler aspl in SliderRoot.GetComponentsInChildren<AdminSliderPanelHandler>()) {
            AdminSliderPanelHandlers.Add(aspl);
            Debug.Log(aspl.VariableNameText.text);
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        ReadParamsFromServer();
    }


    // Update is called once per frame
    void Update()
    {
        
    }



    [ContextMenu("WriteParamsToServer")]
    public void WriteParamsToServer() // called from Store button - linked in editor
    {
        StringBuilder sb = new StringBuilder();
        foreach(AdminSliderPanelHandler aspl in AdminSliderPanelHandlers) {
            sb.AppendLine(aspl.VariableNameText.text + ", " + aspl.SliderValueText.text);
        }
        Utils.inst.WriteFileToServer("Parameters.csv", sb.ToString());
    }

    [ContextMenu("ReadParamsFromServer")]
    public void ReadParamsFromServer()
    {
        string tmp = Utils.inst.ReadFileFromServer("Parameters.csv");
        StartCoroutine("ExtractParams");
    }

    /// <summary>
    /// Reads Parameters from http://www.cse.unr.edu/~sushil/Exp/Parameters.csv and sets admin sliders
    /// </summary>
    /// <returns></returns>
    IEnumerator ExtractParams()
    {
        yield return new WaitForSeconds(5.0f);
        int i = 0;
        using (StringReader sr = new StringReader(Utils.inst.FileContent)) {
            string line;
            while((line = sr.ReadLine()) != null) {
                string[] cells = line.Split(',');
                bool isInt = AdminSliderPanelHandlers[i].isInt;
                if(isInt) {
                    AdminSliderPanelHandlers[i].AdminSlider.value = int.Parse(cells[1]);
                } else {
                    AdminSliderPanelHandlers[i].AdminSlider.value = float.Parse(cells[1]);
                }

                Debug.Log(AdminSliderPanelHandlers[i].VariableNameText.text + ": " + AdminSliderPanelHandlers[i].AdminSlider.value);
                i = i + 1;
            }
        }


    }


}
