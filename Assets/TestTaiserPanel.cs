using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTaiserPanel : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        panel = GetComponent<TaiserPanel>();
    }

    public TaiserPanel panel;
    public bool test;
    // Update is called once per frame
    void Update()
    {
        if(Time.frameCount % 100 == 0)
            panel.isVisible = test;
    }


}
