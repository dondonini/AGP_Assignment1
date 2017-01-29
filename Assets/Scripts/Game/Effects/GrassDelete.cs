using UnityEngine;
using System.Collections;

public class GrassDelete : MonoBehaviour {

    [ReadOnly]
    public bool delayActive = false;
    public float delayDuration = 3.0f;
    public Animator delayAnimation;

    private float startTime = 0;

    /// <summary>
    /// Delay destories grass
    /// </summary>
    public void DelayDestoryGrass()
    {
        if (!delayActive)
        {
            startTime = Time.time;
            delayActive = true;
        }
    }

    /// <summary>
    /// Destory grass
    /// </summary>
	public void DestoryGrass()
    {
        Destroy(gameObject);
    }

    private void Update()
    {
        if (delayActive)
        {
            if (Time.time >= startTime + delayDuration)
            {
                delayActive = false;
                delayAnimation.SetTrigger("Fade Out");
            }
        }
    }
}
