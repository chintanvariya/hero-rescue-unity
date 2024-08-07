
#pragma warning disable 0219

#define SPINE_SKELETONMECANIM

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Globalization;

namespace Spine.Unity.Editor {
	using EventType = UnityEngine.EventType;

	public partial class SpineEditorUtilities {
		public static class DragAndDropInstantiation {
			public struct SpawnMenuData {
				public Vector3 spawnPoint;
				public SkeletonDataAsset skeletonDataAsset;
				public EditorInstantiation.InstantiateDelegate instantiateDelegate;
				public bool isUI;
			}

			public static void SceneViewDragAndDrop (SceneView sceneview) {
				var current = UnityEngine.Event.current;
				var references = DragAndDrop.objectReferences;
				if (current.type == EventType.Layout)
					return;

				// Allow drag and drop of one SkeletonDataAsset.
				if (references.Length == 1) {
					var skeletonDataAsset = references[0] as SkeletonDataAsset;
					if (skeletonDataAsset != null) {
						var mousePos = current.mousePosition;

						bool invalidSkeletonData = skeletonDataAsset.GetSkeletonData(true) == null;
						if (invalidSkeletonData) {
							DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
							Handles.BeginGUI();
							GUI.Label(new Rect(mousePos + new Vector2(20f, 20f), new Vector2(400f, 40f)), new GUIContent(string.Format("{0} is invalid.\nCannot create new Spine GameObject.", skeletonDataAsset.name), SpineEditorUtilities.Icons.warning));
							Handles.EndGUI();
							return;
						} else {
							DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
							Handles.BeginGUI();
							GUI.Label(new Rect(mousePos + new Vector2(20f, 20f), new Vector2(400f, 20f)), new GUIContent(string.Format("Create Spine GameObject ({0})", skeletonDataAsset.skeletonJSON.name), SpineEditorUtilities.Icons.skeletonDataAssetIcon));
							Handles.EndGUI();

							if (current.type == EventType.DragPerform) {
								RectTransform rectTransform = (Selection.activeGameObject == null) ? null : Selection.activeGameObject.GetComponent<RectTransform>();
								Plane plane = (rectTransform == null) ? new Plane(Vector3.back, Vector3.zero) : new Plane(-rectTransform.forward, rectTransform.position);
								Vector3 spawnPoint = MousePointToWorldPoint2D(mousePos, sceneview.camera, plane);
								ShowInstantiateContextMenu(skeletonDataAsset, spawnPoint);
								DragAndDrop.AcceptDrag();
								current.Use();
							}
						}
					}
				}
			}

			public static void ShowInstantiateContextMenu (SkeletonDataAsset skeletonDataAsset, Vector3 spawnPoint) {
				var menu = new GenericMenu();

				// SkeletonAnimation
				menu.AddItem(new GUIContent("SkeletonAnimation"), false, HandleSkeletonComponentDrop, new SpawnMenuData {
					skeletonDataAsset = skeletonDataAsset,
					spawnPoint = spawnPoint,
					instantiateDelegate = (data) => EditorInstantiation.InstantiateSkeletonAnimation(data),
					isUI = false
				});

				// SkeletonGraphic
				var skeletonGraphicInspectorType = System.Type.GetType("Spine.Unity.Editor.SkeletonGraphicInspector");
				if (skeletonGraphicInspectorType != null) {
					var graphicInstantiateDelegate = skeletonGraphicInspectorType.GetMethod("SpawnSkeletonGraphicFromDrop", BindingFlags.Static | BindingFlags.Public);
					if (graphicInstantiateDelegate != null)
						menu.AddItem(new GUIContent("SkeletonGraphic (UI)"), false, HandleSkeletonComponentDrop, new SpawnMenuData {
							skeletonDataAsset = skeletonDataAsset,
							spawnPoint = spawnPoint,
							instantiateDelegate = System.Delegate.CreateDelegate(typeof(EditorInstantiation.InstantiateDelegate), graphicInstantiateDelegate) as EditorInstantiation.InstantiateDelegate,
							isUI = true
						});
				}

#if SPINE_SKELETONMECANIM
				menu.AddSeparator("");
				// SkeletonMecanim
				menu.AddItem(new GUIContent("SkeletonMecanim"), false, HandleSkeletonComponentDrop, new SpawnMenuData {
					skeletonDataAsset = skeletonDataAsset,
					spawnPoint = spawnPoint,
					instantiateDelegate = (data) => EditorInstantiation.InstantiateSkeletonMecanim(data)
				});
#endif

				menu.ShowAsContext();
			}

			public static void HandleSkeletonComponentDrop (object spawnMenuData) {
				var data = (SpawnMenuData)spawnMenuData;

				if (data.skeletonDataAsset.GetSkeletonData(true) == null) {
					EditorUtility.DisplayDialog("Invalid SkeletonDataAsset", "Unable to create Spine GameObject.\n\nPlease check your SkeletonDataAsset.", "Ok");
					return;
				}

				bool isUI = data.isUI;

				Component newSkeletonComponent = data.instantiateDelegate.Invoke(data.skeletonDataAsset);
				GameObject newGameObject = newSkeletonComponent.gameObject;
				Transform newTransform = newGameObject.transform;

				var activeGameObject = Selection.activeGameObject;
				if (isUI && activeGameObject != null)
					newTransform.SetParent(activeGameObject.transform, false);

				newTransform.position = isUI ? data.spawnPoint : RoundVector(data.spawnPoint, 2);

				if (isUI && (activeGameObject == null || activeGameObject.GetComponent<RectTransform>() == null))
					Debug.Log("Created a UI Skeleton GameObject not under a RectTransform. It may not be visible until you parent it to a canvas.");

				if (!isUI && activeGameObject != null && activeGameObject.transform.localScale != Vector3.one)
					Debug.Log("New Spine GameObject was parented to a scaled Transform. It may not be the intended size.");

				Selection.activeGameObject = newGameObject;
				//EditorGUIUtility.PingObject(newGameObject); // Doesn't work when setting activeGameObject.
				Undo.RegisterCreatedObjectUndo(newGameObject, "Create Spine GameObject");
			}

			/// <summary>
			/// Rounds off vector components to a number of decimal digits.
			/// </summary>
			public static Vector3 RoundVector (Vector3 vector, int digits) {
				vector.x = (float)System.Math.Round(vector.x, digits);
				vector.y = (float)System.Math.Round(vector.y, digits);
				vector.z = (float)System.Math.Round(vector.z, digits);
				return vector;
			}

			/// <summary>
			/// Converts a mouse point to a world point on a plane.
			/// </summary>
			static Vector3 MousePointToWorldPoint2D (Vector2 mousePosition, Camera camera, Plane plane) {
				var screenPos = new Vector3(mousePosition.x, camera.pixelHeight - mousePosition.y, 0f);
				var ray = camera.ScreenPointToRay(screenPos);
				float distance;
				bool hit = plane.Raycast(ray, out distance);
				return ray.GetPoint(distance);
			}
		}
	}
}
