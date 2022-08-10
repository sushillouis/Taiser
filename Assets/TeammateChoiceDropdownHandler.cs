using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeammateChoiceDropdownHandler : MonoBehaviour
{
    public Dropdown dropdown;
    public string playerName; //has to be set before OnValueChanged is called
    public PlayerSpecies species;

    private void Awake()
    {
        dropdown = GetComponent<Dropdown>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnValueChanged(int index)
    {

        if(shouldTrigger) {
            switch(dropdown.options[index].text.Trim()) {
                case "Human":
                    species = PlayerSpecies.Human;
                    break;
                case "AI":
                    species = PlayerSpecies.AI;
                    break;
                case "Unknown":
                    species = PlayerSpecies.Unknown;
                    break;
                default:
                    species = PlayerSpecies.Unknown;
                    break;
            }

            NewLobbyMgr.inst.OnValueChangedInTeammateSpeciesChoiceDropdown(playerName, species, dropdown, index);
        }


    }

    public bool shouldTrigger = true;
    public void SetValueWithoutTrigger(int val)
    {
        shouldTrigger = false;
        dropdown.value = val;
        dropdown.RefreshShownValue();
        shouldTrigger = true;
    }



}
