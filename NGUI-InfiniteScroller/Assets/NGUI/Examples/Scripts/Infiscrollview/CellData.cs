using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellData : MonoBehaviour
{
    public UILabel Title;
    public int DataIndex;
    public bool IsSee => GetComponent<UIWidget>().isVisible;
    public void SetData(string title)
    {
        Title.text = title;
    }
}