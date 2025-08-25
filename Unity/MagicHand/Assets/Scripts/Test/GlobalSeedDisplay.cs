using UnityEngine;
using TMPro;

public class ShowRandomSeed : MonoBehaviour
{
    public TextMeshProUGUI text;  // 如果是UI文本
    // public TextMeshPro text;   // 如果是3D文本，取消注释这行，注释上面那行

    private void Update()
    {
        if (DataMgr.GetInstance() != null)
        {
            text.text = "Seed: " + DataMgr.GetInstance().GlobalRandomSeed;
        }
        else
        {
            text.text = "DataMgr not ready";
        }
    }
}