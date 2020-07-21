using UnityEngine;

[System.Serializable]
public struct IntRange
{
    public int min;
    public int max;

    public int RandomValueInRange
    {
        get { return Random.Range(min, max + 1); }
    }
}
