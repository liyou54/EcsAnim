using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorSelect : MonoBehaviour
{
    #if UNITY_EDITOR
    public int index;
    public Image _savedColor;

    public void SetColorSave()
    {
        SoonsoonData.Instance._spumManager._nowSelectColorNum = index;
        if(_savedColor.gameObject.activeInHierarchy)
        {
            SoonsoonData.Instance._spumManager._nowColor = _savedColor.color;
            SoonsoonData.Instance._spumManager._nowColorShow.color = _savedColor.color;
            SoonsoonData.Instance._spumManager._hexColorText.text = SoonsoonData.Instance._spumManager.ColorToStr(_savedColor.color);
            SoonsoonData.Instance._spumManager.SetObjColor();

            SoonsoonData.Instance._spumManager.ToastOn("Loaded Color");
            SoonsoonData.Instance._spumManager._nowSelectColor.SetActive(true);
            SoonsoonData.Instance._spumManager._nowSelectColor.transform.position = transform.position;
            //set the color
        }
        else
        {
            //saved the color
            _savedColor.gameObject.SetActive(true);
            _savedColor.color = SoonsoonData.Instance._spumManager._nowColor;
            SoonsoonData.Instance._spumManager.ToastOn("Saved Color");

            string tSTR = SoonsoonData.Instance._spumManager.ColorToStr(_savedColor.color);
            SoonsoonData.Instance._soonData2._savedColorList[index] = tSTR;
            SoonsoonData.Instance.SaveData();
        }
    }
    #endif
}
