using UnityEngine;
using UnityEngine.EventSystems;

public class OnSelectCube : MonoBehaviour, IPointerClickHandler
{
    // Start is called before the first frame update
    void Start()
    {
        destination = transform.parent.GetComponentInChildren<TDestination>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public TDestination destination;
    public void OnPointerClick(PointerEventData eventData)
    {
        RuleSpecButtonMgr.inst.OnAttackableDestinationClicked(destination);
    }
   
}
