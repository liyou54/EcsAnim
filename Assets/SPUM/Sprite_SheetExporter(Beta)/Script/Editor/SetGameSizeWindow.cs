using UnityEngine;
using UnityEditor;
 
public class SetGameSizeWindow: EditorWindow {
 
private Vector2 _size = new Vector2(1920, 800);
private Vector2 _pos = new Vector2(7, 200);
	
        [MenuItem("Window/My Window")]
    static void Init()
    {
        // 생성되어있는 윈도우를 가져온다. 없으면 새로 생성한다. 싱글턴 구조인듯하다.
        SetGameSizeWindow window = (SetGameSizeWindow)EditorWindow.GetWindow(typeof(SetGameSizeWindow));
        window.Show();
    }
	
    void OnGUI () {



    } // OnGUI()
 
}