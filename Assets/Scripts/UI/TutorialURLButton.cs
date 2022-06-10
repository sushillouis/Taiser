using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialURLButton : MonoBehaviour
{
    [SerializeField] private string _TutorialURL;

    public void OpenTutorial()
    {
        Application.OpenURL(_TutorialURL);
    }

}
