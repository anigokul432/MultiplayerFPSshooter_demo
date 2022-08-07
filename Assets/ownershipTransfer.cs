using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ownershipTransfer : MonoBehaviourPun
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        int numOfChilds = transform.childCount;
        if (numOfChilds > 0)
        {
            ChangeOwner();
        }
    }

    void ChangeOwner()
    {
            base.photonView.RequestOwnership();
    }
}
