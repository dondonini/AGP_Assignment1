using UnityEngine;
using System.Collections;

public class ButtonHandler : MonoBehaviour {

    public Vector2 pos;
    public GameMaster gm;

	public void OnButtonPress()
    {
        gm.OnMapButtonPress(pos);
    }
}
