using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class FragmentData
{
	static public Fragment[] fragments;
	static private Dictionary<string, Fragment> fragDict = new Dictionary<string, Fragment>();
	static private Dictionary<string, int> fragDictID = new Dictionary<string, int>();

	static public void SetFragments(Fragment[] fragments)
    {
		FragmentData.fragments = fragments;
        for (int i = 0; i < fragments.Length; i++)
        {
			fragDict.Add(fragments[i].parent.name, fragments[i]);
			fragDictID.Add(fragments[i].parent.name, i);
		}
			
	}

	static public Fragment GetFragmentByName(string name)
	{
		try
		{
			return fragDict[name];
		}
		catch (KeyNotFoundException)
		{
			return null;
		}
	}

	static public int GetFragmentIndexByName(string name)
	{
		try
		{
			return fragDictID[name];
		}
		catch (KeyNotFoundException)
		{
			return -1;
		}
	}

	static public float GetPairCurveMatchScore(Fragment f1, int facei, int curvei,
		Fragment f2, int facej, int curvej)
	{
        if (f1 != null && f2 != null)
        {
			List<CurveMatch> matchList = f1.SimilarCurve[facei][curvei];
			foreach (var match in matchList)
			{
				if (match.fragmentID == f2.parent.name && match.face == facej && match.curve == curvej)
					return match.matchScore;
			}

			matchList = f2.SimilarCurve[facej][curvej];
			foreach (var match in matchList)
			{
				if (match.fragmentID == f1.parent.name && match.face == facei && match.curve == curvei)
					return match.matchScore;
			}
		}

		return 0.0f;
	}

	/// <summary>
	/// get pair points' position & normals in world position
	/// </summary>
	/// <param name="pairs"></param>
	/// <param name="fPoints"></param>
	/// <param name="fNormals"></param>
	static public void GetPairPoints(List<KeyValuePair<CurveIndex, CurveIndex>> pairs,
		out Vector3[][] fPoints, out Vector3[][] fNormals)
	{
		List<Vector3>[] fPointsList = new List<Vector3>[] { new List<Vector3>(), new List<Vector3>() };
		List<Vector3>[] fNormalsList = new List<Vector3>[] { new List<Vector3>(), new List<Vector3>() };
		for (int pairi = 0; pairi < pairs.Count; pairi++)
		{
			var sourceInfo = pairs[pairi].Key;
			var targetInfo = pairs[pairi].Value;
			Fragment[] fr = new Fragment[] {
				GetFragmentByName(sourceInfo.fragmentID),
				GetFragmentByName(targetInfo.fragmentID)
			};
			int[] faceir = new int[] { sourceInfo.face, targetInfo.face };

			for (int i = 0; i < 2; i++)
			{
				fr[i].GetFaceWorldVN(faceir[i], out Vector3[] fp, out Vector3[] fn);
				fPointsList[i].AddRange(fp);
				fNormalsList[i].AddRange(fn);
			}
		}
		fPoints = new Vector3[][] { fPointsList[0].ToArray(), fPointsList[1].ToArray() };
		fNormals = new Vector3[][] { fNormalsList[0].ToArray(), fNormalsList[1].ToArray() };
	}


	static public List<KeyValuePair<CurveIndex, CurveIndex>> GetAllOpenLinks(Transform[] group)
    {
		var openLinks = new List<KeyValuePair<CurveIndex, CurveIndex>>();
		for (int i = 0; i < group.Length; i++)
		{
			Fragment frags = GetFragmentByName(group[i].parent.name);
			openLinks.AddRange(frags.skeletonLink.FindAll(s => s.Value.face == -1));
		}

		return openLinks;
	}

	/// <summary>
	/// input group transform
	/// </summary>
	/// <param name="group"></param>
	/// <returns></returns>
	static public List<KeyValuePair<CurveIndex, CurveIndex>> GetAllOpenLinks(Transform group)
	{
		List<Transform> fragments = new List<Transform>();
		foreach (Transform transform in group)
            if (transform.childCount > 0)
				fragments.Add(transform.GetChild(0));

		return GetAllOpenLinks(fragments.ToArray());
	}


	static public List<KeyValuePair<CurveIndex, CurveIndex>> GetAllInternalLinks(Transform[] fragTransform)
    {
		Fragment[] frags = new Fragment[fragTransform.Length];
		string[] fragNames = new string[fragTransform.Length];
        for (int fi = 0; fi < frags.Length; fi++)
        {
			frags[fi] = GetFragmentByName(fragTransform[fi].name);
			fragNames[fi] = fragTransform[fi].name;
		}

		var internalLinks = new List<KeyValuePair<CurveIndex, CurveIndex>>();
		for (int fi = 0; fi < frags.Length; fi++)
            for (int li = 0; li < frags[fi].skeletonLink.Count; li++)
            {
				int rltidx = Array.FindIndex(fragNames, id => id == frags[fi].skeletonLink[li].Value.fragmentID);
				if (rltidx >= 0)
					internalLinks.Add(frags[fi].skeletonLink[li]);
			}

		return internalLinks;
    }

	static public List<KeyValuePair<CurveIndex, CurveIndex>> GetAllExternalLinks(
		Transform groupTransform, out List<Vector2Int> skeletonidx)
    {
		Transform[] fragTransforms = GroupManager.GetAllFragTransform(groupTransform);
		Fragment[] frags = new Fragment[fragTransforms.Length];
		string[] fragNames = new string[fragTransforms.Length];
		for (int fi = 0; fi < frags.Length; fi++)
		{
			frags[fi] = GetFragmentByName(fragTransforms[fi].parent.name);
			fragNames[fi] = fragTransforms[fi].parent.name;
		}

		var externalLinks = new List<KeyValuePair<CurveIndex, CurveIndex>>();
		skeletonidx = new List<Vector2Int>();
		for (int fi = 0; fi < frags.Length; fi++)
			for (int li = 0; li < frags[fi].skeletonLink.Count; li++)
            {
				int rltidx = Array.FindIndex(fragNames, id => id == frags[fi].skeletonLink[li].Value.fragmentID);
				if (rltidx < 0 && !frags[fi].skeletonLink[li].Value.IsEmpty())
                {
					externalLinks.Add(frags[fi].skeletonLink[li]);
					int fragRefi = Array.FindIndex(fragments, f => f.GetIDName() == frags[fi].GetIDName());
					skeletonidx.Add(new Vector2Int(fragRefi, li));
				}
			}

		return externalLinks;
	}

	/// <summary>
	/// get all fragment that can be traversed by groupMatch
	/// return frag idx
	/// </summary>
	/// <param name="groupMatch"></param>
	/// <param name="groupManager"></param>
	/// <returns></returns>
	static public int[] GetCurrentSkeletonFragment(GroupMatch groupMatch, GroupManager groupManager)
    {
		var targetLinks = groupMatch.targetLinks;
		var otherLinks = groupMatch.otherLinks;
		List<string> fragName = new List<string>();

		List<Transform> targetFragTransform = new List<Transform>();
		foreach (var li in targetLinks)
			targetFragTransform.Add(GetFragmentByName(li.Key.fragmentID).objMesh.transform);
		groupManager.GetSecondaryParent(targetFragTransform, out List<Transform> targetGroupTransform);

		List<Transform> otherFragTransform = new List<Transform>();
		foreach (var li in otherLinks)
			otherFragTransform.Add(GetFragmentByName(li.Key.fragmentID).objMesh.transform);
		groupManager.GetSecondaryParent(otherFragTransform, out List<Transform> otherGroupTransform);

		foreach (var tt in targetGroupTransform)
		{
			Transform[] targetTransforms = GroupManager.GetAllFragTransform(tt);
			foreach (var t in targetTransforms)
				fragName.Add(t.parent.name);
		}
		foreach (var ot in otherGroupTransform)
		{
			Transform[] otherTransforms = GroupManager.GetAllFragTransform(ot);
			foreach (var t in otherTransforms)
				fragName.Add(t.parent.name);
		}
		fragName = fragName.Distinct().ToList();
		List<int> fragsIdx = new List<int>();
		foreach (var name in fragName)
			fragsIdx.Add(GetFragmentIndexByName(name));

		return fragsIdx.ToArray();
	}

	
	static public void GetCandidatePointCloud(
		GroupMatch groupMatch,
		GroupManager groupManager,
		out KeyValuePair<string, KeyValuePair<Vector3, Quaternion>>[] fragCenters,
		out List<Vector3> SkeletonNodeFacePosition,
		out List<KeyValuePair<int, string>> SkeletonNodeFaceSign)
    {
		var targetLinks = groupMatch.targetLinks;
		var otherLinks = groupMatch.otherLinks;

		List<Transform> targetFragTransform = new List<Transform>();
		foreach (var li in targetLinks)
			targetFragTransform.Add(GetFragmentByName(li.Key.fragmentID).objMesh.transform);
		groupManager.GetSecondaryParent(targetFragTransform, out List<Transform> targetGroupTransform);

		List<Fragment> frags = new List<Fragment>();
		List<string> targetName = new List<string>();
		List<string> otherName = new List<string>();
		foreach (var tt in targetGroupTransform)
		{
			Transform[] targetTransforms = GroupManager.GetAllFragTransform(tt);
			targetName.AddRange(targetTransforms.Select(t => t.parent.name));
			foreach (var t in targetTransforms)
				frags.Add(GetFragmentByName(t.parent.name));
		}
		foreach (var li in otherLinks)
		{
			otherName.Add(li.Key.fragmentID);
			frags.Add(GetFragmentByName(li.Key.fragmentID));
		}
		frags = frags.Distinct().ToList();


		frags = frags.OrderBy(f => f.parent.name).ToList();
		string[] fragName = FragmentData.fragments.Select(f => f.parent.name).OrderBy(name => name).ToArray();


		int fragsIdx = 0;
		SkeletonNodeFacePosition = new List<Vector3>();
		SkeletonNodeFaceSign = new List<KeyValuePair<int, string>>();
		fragCenters = new KeyValuePair<string, KeyValuePair<Vector3, Quaternion>>[frags.Count];
		for (int fi = 0; fi < fragName.Length; fi++)
		{
			// candidate frags
			if (fragsIdx < frags.Count && fragName[fi] == frags[fragsIdx].parent.name)
			{
				Transform fragTransform = frags[fragsIdx].objMesh.transform.Find(FragmentTypes.Default);
				SkeletonNodeFacePosition.Add(fragTransform.TransformPoint(
					fragTransform.GetComponent<MeshFilter>().mesh.bounds.center));
				int fragSign;
				if (targetName.FindIndex(n => n == fragName[fi]) >= 0)
					fragSign = 0;
				else
					fragSign = -1;
				SkeletonNodeFaceSign.Add(new KeyValuePair<int, string>(fragSign, fragName[fi]));

				var allFace = frags[fragsIdx].skeletonLink
					.Select(s => s.Key.face)
					.OrderBy(face => face)
					.ToList();

				var linkFace = new List<int>();
				var matchingFace = new List<int>();
				if (fragSign == 0)
                {
					linkFace = frags[fragsIdx].skeletonLink
						.Where(s => !s.Value.IsEmpty())
						.Select(s => s.Key.face).ToList();

					matchingFace = targetLinks
						.Where(l => l.Key.fragmentID == frags[fragsIdx].parent.name)
						.Select(l => l.Key.face).ToList();
				}
                else
                {
					matchingFace.AddRange(otherLinks
						.Where(l => l.Key.fragmentID == frags[fragsIdx].parent.name)
						.Select(l => l.Key.face));
				}

				var candidateFace = new List<int>(linkFace);
				candidateFace.AddRange(matchingFace);
				candidateFace = candidateFace.OrderBy(face => face).ToList();

				int linkFaceIdx = 0;
				for (int facei = 0; facei < allFace.Count; facei++)
				{
					if (linkFaceIdx < candidateFace.Count && candidateFace[linkFaceIdx] == allFace[facei])
					{
						int faceSign;
						if (linkFace.FindIndex(l => l == candidateFace[linkFaceIdx]) >= 0)
							faceSign = 1;
						else
							faceSign = 3;

                        if (fragSign == 0 || (fragSign == -1 && faceSign == 3))
						{
							SkeletonNodeFacePosition.Add(fragTransform.TransformPoint(
								frags[fragsIdx].faceCenter[candidateFace[linkFaceIdx]]));
							SkeletonNodeFaceSign.Add(
								new KeyValuePair<int, string>(faceSign, candidateFace[linkFaceIdx].ToString()));
						}
                        else
                        {
							SkeletonNodeFacePosition.Add(Vector3.one * 100);
							SkeletonNodeFaceSign.Add(
								new KeyValuePair<int, string>(2, candidateFace[linkFaceIdx].ToString()));
						}

						linkFaceIdx += 1;
					}
                    else
                    {
						SkeletonNodeFacePosition.Add(Vector3.one * 100);
						SkeletonNodeFaceSign.Add(new KeyValuePair<int, string>(2, allFace[facei].ToString()));
					}
						
				}

				fragCenters[fragsIdx] = new KeyValuePair<string, KeyValuePair<Vector3, Quaternion>>(
					frags[fragsIdx].parent.name,
					new KeyValuePair<Vector3, Quaternion>(fragTransform.parent.position, fragTransform.parent.rotation));

				fragsIdx += 1;
			}
			else
			{
				Fragment otherfrag = FragmentData.GetFragmentByName(fragName[fi]);
				
				int faceNum = otherfrag.skeletonLink.Count();
				SkeletonNodeFacePosition.AddRange(Enumerable.Repeat(Vector3.one * 100, faceNum + 1));
				SkeletonNodeFaceSign.Add(new KeyValuePair<int, string>(-2, fragName[fi]));
				SkeletonNodeFaceSign.AddRange(Enumerable.Repeat(
					new KeyValuePair<int, string>(4, "-1"), faceNum));
			}
		}
	}


	static public void GetGroupSkeleton(GameObject group, out Vector3[] np, out Vector4[] nc, out Vector3[][] lp, out Vector4[] lc)
    {
		List<Vector3> nodePos = new List<Vector3>();
		List<Vector4> nodeColor = new List<Vector4>();
		List<Vector3[]> linkPos = new List<Vector3[]>();
		List<Vector4> linkColor = new List<Vector4>();

		
		Transform[] groupFragTrans = GroupManager.GetAllFragTransform(group.transform);
		// nodes
		int[] nodePosIndex = new int[fragments.Length];
		for (int i = 0; i < groupFragTrans.Length; i++)
        {
			Transform fragDefTrans = groupFragTrans[i].Find(FragmentTypes.Default);
			Vector3 fc = fragDefTrans.TransformPoint(fragDefTrans.GetComponent<MeshFilter>().mesh.bounds.center);
			nodePos.Add(fc);
			nodeColor.Add(Color.cyan);

			int fi = GetFragmentIndexByName(groupFragTrans[i].parent.name);
			nodePosIndex[fi] = i;
		}

		HashSet<Vector4> pairSet = new HashSet<Vector4>();
		for (int i = 0; i < groupFragTrans.Length; i++)
		{
			int fi = GetFragmentIndexByName(groupFragTrans[i].parent.name);
			Fragment frag = fragments[fi];
			for (int li = 0; li < frag.skeletonLink.Count; li++)
			{
				var partner = frag.skeletonLink[li];
				int fj = GetFragmentIndexByName(partner.Value.fragmentID);
				if (pairSet.Contains(new Vector4(fi, partner.Key.face, fj, partner.Value.face)))
					continue;

				if (fj >= 0)
                {
					linkPos.Add(new Vector3[] { nodePos[nodePosIndex[fi]], nodePos[nodePosIndex[fj]] });
					linkColor.Add(nodeColor[nodePosIndex[fi]]);
					linkColor.Add(nodeColor[nodePosIndex[fj]]);
				}
				else
				{
					Transform t = fragments[fi].objMesh.transform.Find(FragmentTypes.Default);
					linkPos.Add(new Vector3[] { nodePos[nodePosIndex[fi]], t.TransformPoint(fragments[fi].faceCenter[partner.Key.face]) });
					linkColor.Add(nodeColor[nodePosIndex[fi]]);
					linkColor.Add(nodeColor[nodePosIndex[fi]]);
				}

				pairSet.Add(new Vector4(fi, partner.Key.face, fj, partner.Value.face));
				pairSet.Add(new Vector4(fj, partner.Value.face, fi, partner.Key.face));
			}
		}

		np = nodePos.ToArray();
		nc = nodeColor.ToArray();
		lp = linkPos.ToArray();
		lc = linkColor.ToArray();
	}


	static public int[] GetRefFragmentofGroupMatch(GroupMatch groupMatch, GroupManager groupManager)
    {
		var targetLinks = groupMatch.targetLinks;
		var otherLinks = groupMatch.otherLinks;

		List<Transform> targetFragTransform = new List<Transform>();
		foreach (var li in targetLinks)
			targetFragTransform.Add(GetFragmentByName(li.Key.fragmentID).objMesh.transform);
		groupManager.GetSecondaryParent(targetFragTransform, out List<Transform> targetGroupTransform);

		List<int> frags = new List<int>();
		List<string> targetName = new List<string>();
		List<string> otherName = new List<string>();
		foreach (var tt in targetGroupTransform)
		{
			Transform[] targetTransforms = GroupManager.GetAllFragTransform(tt);
			targetName.AddRange(targetTransforms.Select(t => t.parent.name));
			foreach (var t in targetTransforms)
				frags.Add(GetFragmentIndexByName(t.parent.name));
		}
		foreach (var li in otherLinks)
		{
			otherName.Add(li.Key.fragmentID);
			frags.Add(GetFragmentIndexByName(li.Key.fragmentID));
		}

		return frags.Distinct().ToArray();
	}
}
