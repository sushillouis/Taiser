using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewAudioMgr : MonoBehaviour
{

    public static NewAudioMgr inst;
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

    public AudioSource source;
    public AudioClip maliciousUnfiltered;
    public AudioClip maliciousFiltered;
    public AudioClip MaliciousRuleChanged;
    public AudioClip BadFilterRule;
    public AudioClip GoodFilterRule;


    public void PlayOneShot(AudioClip clip)
    {
        source.PlayOneShot(clip);
    }
    


}
