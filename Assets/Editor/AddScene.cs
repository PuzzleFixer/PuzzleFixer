using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEditor.SceneManagement;

public class AddScene
{
	[MenuItem("File/Combine Scenes")]
	static void Combine()
	{
		Object[] objects = Selection.objects;

		EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
		EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

		foreach (Object item in objects)
			EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(item), OpenSceneMode.Additive);
	}


	[MenuItem("File/Combine Scenes", true)]
	static bool CanCombine()
	{
		if (Selection.objects.Length < 2)
			return false;

		foreach (Object item in Selection.objects)
			if (!Path.GetExtension(AssetDatabase.GetAssetPath(item)).ToLower().Equals(".unity"))
				return false;

		return true;
	}
}
