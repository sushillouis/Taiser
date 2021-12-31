using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelPositioner : MonoBehaviour
{
    // Start is called before the first frame update
    public RectTransform panel;
    void Start()
    {
        panel = GetComponent<RectTransform>();
        holdingPosition = panel.anchoredPosition;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public Vector2 holdingPosition;
    public Vector2 showPosition;

    public bool _isVisible = false;
    public bool isVisible
    {
        get { return _isVisible; }
        set
        {
            _isVisible = value;
            if(_isVisible)
                panel.anchoredPosition = showPosition;
            else
                panel.anchoredPosition = holdingPosition;

        }
    }

}
