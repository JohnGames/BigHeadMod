using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityModManagerNet;
using Harmony;
using System.Reflection;
using static UnityModManagerNet.UnityModManager;
using System.IO;
using System.Collections;

namespace BigHeadMod
{
	static class Main
	{
		public static ModEntry mod;

		// Send a response to the mod manager about the launch status, success or not.
		static bool Load(ModEntry modEntry)
		{
			var harmony = HarmonyInstance.Create(modEntry.Info.Id);
			harmony.PatchAll(Assembly.GetExecutingAssembly());

			mod = modEntry;

			return true; // If false the mod will show an error.
		}

	}

	[HarmonyPatch(typeof(Worker))]
	[HarmonyPatch("UpdateHeadScale")]
	static class UpdateHeadScale
	{

		static bool Prefix(Worker __instance)
		{
			Traverse traverse = Traverse.Create(__instance);
			GameObject m_HeadModel = traverse.Field<GameObject>("m_HeadModel").Value;
			if (m_HeadModel != null)
			{
				float num = 2f;
				if (__instance.m_ExtraMemorySize > 0 || __instance.m_ExtraSearchRange > 0)
				{
					num *= 1.35f;
				}
				m_HeadModel.transform.localScale = new Vector3(num, num, num);
			}
			return false;
		}
	}

	[HarmonyPatch(typeof(FarmerClothes))]
	[HarmonyPatch("RefreshParents")]
	static class FindTransforms
	{
		static List<FarmerClothes> updated = new List<FarmerClothes>();
		internal static List<FarmerClothes> danger = new List<FarmerClothes>();
		static AssetBundle bundle;

		static bool Prefix(FarmerClothes __instance)
		{
			if (updated.Contains(__instance))
			{
				return true;
			}
			Traverse traverse = Traverse.Create(__instance);
			Farmer __m_Farmer = traverse.Field<Farmer>("m_Farmer").Value;
			if (__m_Farmer.m_ModelRoot == null)
			{
				return true;
			}

			if (!__m_Farmer.name.Contains("FarmerPlayer"))
			{
				return true;
			}

			//Destroy the old hatpoint.
			object[] myObjArray = { __m_Farmer.m_ModelRoot.transform, "HatPoint" };
			Type[] typeArray = { typeof(Transform), typeof(string) };
			GameObject originalHatPoint = traverse.Method("FindChildByName", typeArray, myObjArray).GetValue<GameObject>();
			if (originalHatPoint == null)
			{
				Debug.Log("Failed to find HatPoint for hat.");
				return true;
			}

			//Load in our new 
			if (bundle == null)
			{
				bundle = AssetBundle.LoadFromFile($"{Path.GetDirectoryName(Application.dataPath)}/Mods/BigHeadMod/head");
			}
			if (bundle == null)
			{
				Debug.Log("Failed to load AssetBundle!");
				return true;
			}

			GameObject head = bundle.LoadAsset<GameObject>("Head");
			if (head == null)
			{
				Debug.Log("Fail to load head!");
				return true;
			}
			GameObject.Instantiate(head, __m_Farmer.m_ModelRoot.transform);
			GameObject.Destroy(originalHatPoint);
			updated.Add(__instance);
			danger.Add(__instance);


			//StreamWriter sr = new StreamWriter("C:/Users/JohnC/OneDrive/Desktop/log.txt", true);

			//if (__m_Farmer.m_ModelRoot == null)
			//{
			//	sr.Close();
			//	return true; ;
			//}
			//sr.WriteLine($"Finding {__m_Farmer.name} object tree");
			//sr.WriteLine("{");
			////Traverse the game object tree.
			//foreach (var item in __m_Farmer.m_ModelRoot.GetComponentsInChildren<Transform>(true))
			//{
			//	sr.WriteLine(item.name);
			//	sr.WriteLine("{");
			//	Component[] list = item.GetComponents<Component>();
			//	foreach (var item2 in list)
			//	{
			//		sr.WriteLine(item2.ToString());
			//		if(item2.GetType() == typeof(MeshFilter))
			//		{
			//			MeshFilter mr = (MeshFilter)item2;

			//		}
			//	}
			//	sr.WriteLine("}");

			//}
			//sr.WriteLine("}");

			//sr.Close();
			return true;
		}

	}

	[HarmonyPatch(typeof(FarmerClothes))]
	[HarmonyPatch("Add")]
	static class RefreshPlayerModel
	{
		static bool Prefix(FarmerClothes __instance)
		{
			if (FindTransforms.danger.Contains(__instance))
			{
				__instance.RefreshParents();
				FindTransforms.danger.Remove(__instance);
			}

			return true;
		}
	}


}

