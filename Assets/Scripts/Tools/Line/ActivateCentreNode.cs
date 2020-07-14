using Sibz.Lines;
using UnityEngine;

public class ActivateCentreNode : MonoBehaviour
{
    //public GameObject CentreNode;

    private LineBehaviour parentLine;

    private void OnEnable()
    {
        parentLine = transform.parent.GetComponent<LineBehaviour>();
    }

    private void OnTriggerEnter(Collider other)
    {
        //CentreNode.SetActive(true);
        //Debug.Log("Activated Centre Node");
        parentLine.CentreNodeEnabled = true;
    }

    private void OnTriggerExit(Collider other)
    {
        //CentreNode.SetActive(false);
        //Debug.Log("Deactivated Centre Node");
        parentLine.CentreNodeEnabled = false;
    }
}