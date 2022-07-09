using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUIProcess
{
	public bool showGUI = false;
	public float progress = 0;
	public string guiText = "";

	public void DrawGUIBar()
	{
		GUIStyle progessbar = new GUIStyle();
		progessbar.fontSize = 40;
		progessbar.normal.textColor = Color.cyan;

		if (showGUI)
		{
			GUI.BeginGroup(new Rect(Screen.width / 2 - 500, Screen.height / 2, 3000.0f, 50));
			GUI.Box(new Rect(0, 0, 2000.0f, 50.0f), guiText, progessbar);
			GUI.Box(new Rect(0, 0, progress * 100 * 10.0f, 50), "");
			GUI.EndGroup();
		}
	}
}
