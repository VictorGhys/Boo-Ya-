using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomBehavior : MonoBehaviour
{
    [SerializeField]
    private int _id = -1;

    private const string _npcTag = "NPC";
    private const string _interactTag = "Interact";


    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log(_id);

        if (other.tag == _npcTag)
        {
            NPCBehavior behavior = 
                other.gameObject.GetComponent<NPCBehavior>();
            behavior.RoomId = _id;
        }

        if (other.tag == _interactTag) /*!! Add interact tag to all interactables !!*/
        {
            BaseInteractable baseInteractable = 
                other.gameObject.GetComponent<BaseInteractable>();
            if (baseInteractable)
                baseInteractable.RoomId = _id;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.tag == _npcTag)
        {
            NPCBehavior behavior =
                other.gameObject.GetComponent<NPCBehavior>();
            behavior.RoomId = _id;
        }

        if (other.tag == _interactTag) /*!! Add interact tag to all interactables !!*/
        {
            BaseInteractable baseInteractable =
                other.gameObject.GetComponent<BaseInteractable>();
            if (baseInteractable)
                baseInteractable.RoomId = _id;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == _npcTag)
        {
            NPCBehavior behavior =
                other.gameObject.GetComponent<NPCBehavior>();
            behavior.RoomId = -1;
        }

        if (other.tag == _interactTag) /*!! Add interact tag to all interactables !!*/
        {
            BaseInteractable baseInteractable =
                other.gameObject.GetComponent<BaseInteractable>();
            if (baseInteractable)
                baseInteractable.RoomId = -1;
        }
    }
}
