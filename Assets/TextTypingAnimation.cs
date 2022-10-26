using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class TextTypingAnimation : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        BriefingText = AnimatableText.text;
        briefingTextLength = BriefingText.Length;
        ParentTaiserPanel = transform.gameObject.GetComponent<TaiserPanel>();
        Debug.Log(BriefingText);
        AnimatableText.text = "";
        textAnimator = TextTypeSlowly(0.1f);
    }

    /*
     * "The line between nation-state and criminal actors is increasingly blurry as nation-states turn to criminal proxies as a tool of state power, then turn a blind eye to the cyber crime perpetrated by the same malicious actors. "
Mieke Eoyang, Deputy assistant secretary of defense 

UNR has Funds from the National Science Foundation and the Department of Defense

Objective: Develop training programs for next generation cyber defense

You! can help us evaluate and assess next gen cyber defense tools
Work with a human or AI teammate to detect, filter, and eliminate malicious packets sent by nation state and cyber criminal actors

Hospitals, utilities, and defense infrastructure may be vulnerable


Work fast, work together, work accurately
Lives are at stake
     *
     *
     * */


    public int briefingTextLength = 0;
    public TaiserPanel ParentTaiserPanel;
    public Text AnimatableText;
    public string BriefingText;

    private IEnumerator textAnimator;

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool _animate;
    public bool Animate
    {
        get { return _animate; }
        set
        {
            _animate = value;
            if(_animate) {
                StartCoroutine(textAnimator);
            } else {
                StopCoroutine(textAnimator);
            }
        }
    }

    IEnumerator TextTypeSlowly(float timeToWait)
    {
        StringBuilder sb = new StringBuilder();

        for(int i = 0; i < briefingTextLength; i++) {
            sb.Append(BriefingText.Substring(i, 1));
            AnimatableText.text = sb.ToString();
            if(BriefingText.Substring(i,1) == "\n")
                yield return new WaitForSeconds(timeToWait * 10);
            else
                yield return new WaitForSeconds(timeToWait);
        }
    }

}
