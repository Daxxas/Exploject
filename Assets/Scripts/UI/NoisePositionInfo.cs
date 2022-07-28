using TMPro;
using UnityEngine;

public class NoisePositionInfo : MonoBehaviour
{
    const float fpsMeasurePeriod = 0.1f;
    private float fpsNextPeriod = 0;
    [SerializeField] private GenerationConfiguration generationConfiguration;
    [SerializeField] private Transform player;
    const string display = "Y continentalness: {0:0.000} Squash continentalness: {1:0.000}";
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
            tmp.text = string.Format(display, generationConfiguration.yContinentalness.GetNoise(GenerationInfo.seed, player.position.x, player.position.z), generationConfiguration.squashContinentalness.GetNoise(GenerationInfo.seed, player.position.x, player.position.y, player.position.z));
        }    
    }
}