using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(InfitiScrollView))]
public class InfitiScrollViewEditor : Editor
{
	public override void OnInspectorGUI()
	{
		InfitiScrollView script = target as InfitiScrollView;

		if (!Application.isPlaying)
		{
			if (GUILayout.Button("JumpTo"))
			{
				script.JumpTo();
			}
		}
	}
}
