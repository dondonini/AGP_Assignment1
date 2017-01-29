using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class CameraMotion : MonoBehaviour {

    public bool active = true;
    public float mouseSensitivity = 1.0f;

    private Vector3 lastPosition;

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

        if (active)
        {
            Vector3 delta = Input.mousePosition - lastPosition;
            transform.Translate(delta.x * mouseSensitivity, delta.y * mouseSensitivity, 0);
            lastPosition = Input.mousePosition;
        }
    }
}
