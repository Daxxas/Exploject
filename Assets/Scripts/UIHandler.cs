using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHandler : MonoBehaviour
{
    [SerializeField] private TMP_InputField equationField;
    [SerializeField] private TMP_InputField seedField;
    [SerializeField] private TMP_InputField widthField;
    [SerializeField] private TMP_InputField lengthField;
    [SerializeField] private Slider waterLevelSlider;
    [SerializeField] private TMP_InputField waterLevelField;

    [SerializeField] private MapDataGenerator mapDataGenerator;

    public void UpdateEquation()
    {
        // mapGenerator.generationEquation = equationField.text;
    }

    public void UpdateSeed()
    {
        mapDataGenerator.seed = int.Parse(seedField.text);
    }
}
