using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaiserPanel : MonoBehaviour
{
    // Start is called before the first frame update
    public RectTransform panel;
    private void Awake()
    {
        panel = GetComponent<RectTransform>(); //redundant
        holdingPosition = panel.anchoredPosition;

    }
    void Start()
    {
    }
    // Update is called once per frame

    void Update()
    {

    }

    public Vector2 holdingPosition;
    public Vector2 visiblePosition = Vector2.zero;

    public bool _isVisible = false;
    public bool isVisible
    {
        get { return _isVisible; }
        set {
            _isVisible = value;
            if(_isVisible)
                panel.anchoredPosition = visiblePosition;
            else
                panel.anchoredPosition = holdingPosition;
        }
    }




}
