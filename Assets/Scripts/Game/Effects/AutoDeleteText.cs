using UnityEngine;
using System.Collections;

public class AutoDeleteText : MonoBehaviour {

	public void DestroyText()
    {
        Destroy(gameObject);
    }
}
