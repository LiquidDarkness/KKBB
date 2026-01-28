using System;
using UnityEngine;

[CreateAssetMenu]
public class TypeDistinguisher : ScriptableObject
{
    public string PrefsKey => name;
    public PlayerPrefType prefType;
    public bool purgable = true;

    public event Action OnValueChanged;

    private void OnEnable()
    {
        //OnValueChanged += SaveManager.Save;
        //OnValueChanged += PlayerPrefs.Save;
    }

    private void OnDisable()
    {
        //OnValueChanged -= SaveManager.Save;
        //OnValueChanged -= PlayerPrefs.Save;
    }

    public override string ToString()
    {
        return $"{PrefsKey}/{prefType}/{GetExportValue()}";
    }

    [ContextMenu(nameof(LogValue))]
    public  void LogValue()
    {
        Debug.Log(this.ToString());
    }

    public void GetValue(out int value)
    {
        value = PlayerPrefs.GetInt(PrefsKey);
    }
    public void GetValue(out float value)
    {
        value = PlayerPrefs.GetFloat(PrefsKey);
    }    

    public void GetValue(out string value)
    {
        value = PlayerPrefs.GetString(PrefsKey);
    }

    public int IntValue => PlayerPrefs.GetInt(PrefsKey);
    public float FloatValue => PlayerPrefs.GetFloat(PrefsKey);
    public string StringValue => PlayerPrefs.GetString(PrefsKey);

    public bool BoolValue => IntValue > 0;


    private string GetExportValue()
    {
        switch (prefType)
        {
            case PlayerPrefType.INT:
                return PlayerPrefs.GetInt(PrefsKey).ToString();            
            case PlayerPrefType.BOOL:
                return PlayerPrefs.GetInt(PrefsKey).ToString();
            case PlayerPrefType.FLOAT:
                return PlayerPrefs.GetFloat(PrefsKey).ToString();
            case PlayerPrefType.STRING:
                return PlayerPrefs.GetString(PrefsKey);
            default:
                return string.Empty;
        }
    }

    public enum PlayerPrefType
    {
        INT,
        FLOAT,
        STRING,
        BOOL,
    }

    public void SetBoolValue(bool value) => SetValue(value); 
    public void SetFloatValue(float value) => SetValue(value); 
    public void SetIntValue(int value) => SetValue(value); 
    public void SetStringValue(string value) => SetValue(value); 

    internal static void FromString(string item)
    {
        string[] elements = item.Split('/');
        Debug.Assert(elements.Length == 3, $"Unable to parse: {item}");
        switch (elements[1])
        {
            case nameof(PlayerPrefType.INT):
                if (!int.TryParse(elements[2], out int iValue))
                {
                    Debug.LogError($"Wrong int value: {elements[2]}, for key: {elements[0]}");
                }
                PlayerPrefs.SetInt(elements[0], iValue);
                break;
            case nameof(PlayerPrefType.BOOL):
                if (!int.TryParse(elements[2], out int bValue))
                {
                    Debug.LogError($"Wrong int value: {elements[2]}, for key: {elements[0]}");
                }
                PlayerPrefs.SetInt(elements[0], bValue);
                break;
            case nameof(PlayerPrefType.FLOAT):
                if (!float.TryParse(elements[2], out float fValue))
                {
                    Debug.LogError($"Wrong float value: {elements[2]}, for key: {elements[0]}");
                }
                PlayerPrefs.SetFloat(elements[0], fValue);
                break;
            case nameof(PlayerPrefType.STRING):
                PlayerPrefs.SetString(elements[0], elements[2]);
                break;
        }
        if (elements[0] == "chosenScenario")
        {
            Debug.Log(item);
        }
    }

    public void SetValue(object valueToSet)
    {
        switch (this.prefType)
        {
            case PlayerPrefType.INT:
                PlayerPrefs.SetInt(PrefsKey, (int)valueToSet);
                break;
            case PlayerPrefType.BOOL:
                PlayerPrefs.SetInt(PrefsKey, (bool)valueToSet ? 1 : 0);
                break;
            case PlayerPrefType.FLOAT:
                PlayerPrefs.SetFloat(PrefsKey, (float)valueToSet);
                break;
            case PlayerPrefType.STRING:
                PlayerPrefs.SetString(PrefsKey, valueToSet.ToString());
                break;
            default:
                break;
        }
        if (name == "chosenScenario")
        {
            Debug.Log(valueToSet);
        }
        OnValueChanged?.Invoke();
    }
}
