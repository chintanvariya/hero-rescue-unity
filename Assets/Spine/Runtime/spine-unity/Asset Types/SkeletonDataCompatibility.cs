
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Globalization;

namespace Spine.Unity {

	public static class SkeletonDataCompatibility {

		static readonly int[][] compatibleBinaryVersions = { new[] { 3, 8, 0 } };
		static readonly int[][] compatibleJsonVersions = { new[] { 3, 8, 0 } };

		static bool wasVersionDialogShown = false;

		public enum SourceType {
			Json,
			Binary
		}

		[System.Serializable]
		public class VersionInfo {
			public string rawVersion = null;
			public int[] version = null;
			public SourceType sourceType;
		}

		[System.Serializable]
		public class CompatibilityProblemInfo {
			public VersionInfo actualVersion;
			public int[][] compatibleVersions;

			public string DescriptionString () {
				string compatibleVersionString = "";
				string optionalOr = null;
				foreach (int[] version in compatibleVersions) {
					compatibleVersionString += string.Format("{0}{1}.{2}", optionalOr, version[0], version[1]);
					optionalOr = " or ";
				}
				return string.Format("Skeleton data could not be loaded. Data version: {0}. Required version: {1}.\nPlease re-export skeleton data with Spine {1} or change runtime to version {2}.{3}.",
					actualVersion.rawVersion, compatibleVersionString, actualVersion.version[0], actualVersion.version[1]);
			}
		}

	#if UNITY_EDITOR
		public static VersionInfo GetVersionInfo (TextAsset asset) {
			if (asset == null)
				return null;

			VersionInfo fileVersion = new VersionInfo();
			fileVersion.sourceType = asset.name.Contains(".skel") ? SourceType.Binary : SourceType.Json;

			if (fileVersion.sourceType == SourceType.Binary) {
				try {
					using (var memStream = new MemoryStream(asset.bytes)) {
						fileVersion.rawVersion = SkeletonBinary.GetVersionString(memStream);
					}
				}
				catch (System.Exception e) {
					Debug.LogErrorFormat("Failed to read '{0}'. It is likely not a binary Spine SkeletonData file.\n{1}", asset.name, e);
					return null;
				}
			}
			else {
				object obj = Json.Deserialize(new StringReader(asset.text));
				if (obj == null) {
					Debug.LogErrorFormat("'{0}' is not valid JSON.", asset.name);
					return null;
				}

				var root = obj as Dictionary<string, object>;
				if (root == null) {
					Debug.LogErrorFormat("'{0}' is not compatible JSON. Parser returned an incorrect type while parsing version info.", asset.name);
					return null;
				}

				if (root.ContainsKey("skeleton")) {
					var skeletonInfo = (Dictionary<string, object>)root["skeleton"];
					object jv;
					skeletonInfo.TryGetValue("spine", out jv);
					fileVersion.rawVersion = jv as string;
				}
			}

			string primaryRuntimeVersionDebugString = compatibleBinaryVersions[0][0] + "." + compatibleBinaryVersions[0][1];
			if (string.IsNullOrEmpty(fileVersion.rawVersion)) {
				// very likely not a Spine skeleton json file at all.
				return null;
			}

			var versionSplit = fileVersion.rawVersion.Split('.');
			try {
				fileVersion.version = new[]{ int.Parse(versionSplit[0], CultureInfo.InvariantCulture),
									int.Parse(versionSplit[1], CultureInfo.InvariantCulture) };
			}
			catch (System.Exception e) {
				Debug.LogErrorFormat("Failed to read version info at skeleton '{0}'. It is likely not a valid Spine SkeletonData file.\n{1}", asset.name, e);
				return null;
			}
			return fileVersion;
		}

		public static CompatibilityProblemInfo GetCompatibilityProblemInfo (VersionInfo fileVersion) {
			if (fileVersion == null)
				return null;

			CompatibilityProblemInfo info = new CompatibilityProblemInfo();
			info.actualVersion = fileVersion;
			info.compatibleVersions = (fileVersion.sourceType == SourceType.Binary) ? compatibleBinaryVersions
				: compatibleJsonVersions;
			
			foreach (var compatibleVersion in info.compatibleVersions) {
				bool majorMatch = fileVersion.version[0] == compatibleVersion[0];
				bool minorMatch = fileVersion.version[1] == compatibleVersion[1];
				if (majorMatch && minorMatch) {
					return null; // is compatible, thus no problem info returned
				}
			}
			return info;
		}

		public static void DisplayCompatibilityProblem (string descriptionString, TextAsset spineJson) {
			if (!wasVersionDialogShown) {
				wasVersionDialogShown = true;
				UnityEditor.EditorUtility.DisplayDialog("Version mismatch!", descriptionString, "OK");
			}
			Debug.LogError(string.Format("Error importing skeleton '{0}': {1}",
				spineJson.name, descriptionString), spineJson);
		}
	#endif // UNITY_EDITOR
	}
}
