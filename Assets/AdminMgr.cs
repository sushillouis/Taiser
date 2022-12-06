using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;



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
        int i = 0;
        foreach(AdminSliderPanelHandler aspl in SliderRoot.GetComponentsInChildren<AdminSliderPanelHandler>()) {
            AdminSliderPanelHandlers.Add(aspl);
            aspl.ParameterName = (ParameterNames) i;
            aspl.VariableNameText.text = aspl.ParameterName.ToString();
            //Debug.Log(aspl.VariableNameText.text);
            i = i + 1;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        AssignSliders();
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
            sb.AppendLine(((int) aspl.ParameterName).ToString() +  ", " 
                + aspl.ParameterName.ToString() + ", " 
                + aspl.SliderValueText.text);
        }
        Utils.inst.WriteFileToServer("Parameters.csv", sb.ToString());
    }

    public bool shouldUseDefaultParameters = false;
    public string defaultParameterFilename = "DefaultParameters.csv";
    public static string parameterFilename = "Parameters.csv";
    [ContextMenu("ReadParamsFromServer")]
    public void ReadParamsFromServer()
    {
        if(shouldUseDefaultParameters) {
            parameterFilename = defaultParameterFilename;
        }
        //Debug.Log("----------------------->" + parameterFilename);
        //Debug.Log("Hmmm");
        string tmp = Utils.inst.ReadFileFromServer(parameterFilename);
        StartCoroutine("ExtractParams");
    }

    /// <summary>
    /// Reads Parameters from webserver.../Parameters.csv and sets admin sliders
    /// </summary>
    /// <returns></returns>
    IEnumerator ExtractParams()
    {
        yield return new WaitForSeconds(5.0f); // For the file read in Utils.inst.ReadFileFromServer to finish
        int i = 0;
        using(StringReader sr = new StringReader(Utils.inst.FileContent)) {
            string line;
            while((line = sr.ReadLine()) != null) {
                string[] cells = line.Split(',');
                bool isInt = AdminSliderPanelHandlers[i].isInt;
                if(isInt) {
                    AdminSliderPanelHandlers[i].AdminSlider.value = int.Parse(cells[2]);
                } else {
                    AdminSliderPanelHandlers[i].AdminSlider.value = float.Parse(cells[2]);
                }

                Debug.Log(AdminSliderPanelHandlers[i].VariableNameText.text + ": " + AdminSliderPanelHandlers[i].AdminSlider.value);
                i = i + 1;
            }
        }


    }


}
