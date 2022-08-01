using TMPro;
using UnityEngine;

public class PlayerPositionInfo : MonoBehaviour
{
    const float fpsMeasurePeriod = 0.1f;
    private float fpsNextPeriod = 0;
    [SerializeField] private Transform playerTransform;
    const string display = "x: {0:0} y: {1:0} z: {2:0}";
    private TextMeshProUGUI tmp;
    
    private void Start()
    {
        fpsNextPeriod = Time.realtimeSinceStartup + fpsMeasurePeriod;
        tmp = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        // measure average frames per second
        if (Time.realtimeSinceStartup > fpsNextPeriod)
        {
            fpsNextPeriod += fpsMeasurePeriod;
            tmp.text = string.Format(display, playerTransform.position.x, playerTransform.position.y, playerTransform.position.z);
        }    
    }
}
