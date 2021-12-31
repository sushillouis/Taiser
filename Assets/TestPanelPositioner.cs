using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestPanelPositioner : MonoBehaviour
{

    public PanelPositioner panel;
    // Start is called before the first frame update
    void Start()
    {
        panel = GetComponent<PanelPositioner>();
    }

    // Update is called once per frame
    public bool test;
    void Update()
    {
        if(Time.frameCount % 100 == 0)
            panel.isVisible = test;
    }
}
