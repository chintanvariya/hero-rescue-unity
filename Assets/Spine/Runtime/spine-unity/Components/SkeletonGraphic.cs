
#if UNITY_2018_3 || UNITY_2019 || UNITY_2018_3_OR_NEWER
#define NEW_PREFAB_SYSTEM
#endif

using UnityEngine;
using UnityEngine.UI;

namespace Spine.Unity {
	#if NEW_PREFAB_SYSTEM
	[ExecuteAlways]
	#else
	[ExecuteInEditMode]
	#endif
	[RequireComponent(typeof(CanvasRenderer), typeof(RectTransform)), DisallowMultipleComponent]
	[AddComponentMenu("Spine/SkeletonGraphic (Unity UI Canvas)")]
	public class SkeletonGraphic : MaskableGraphic, ISkeletonComponent, IAnimationStateComponent, ISkeletonAnimation, IHasSkeletonDataAsset {

		#region Inspector
		public SkeletonDataAsset skeletonDataAsset;
		public SkeletonDataAsset SkeletonDataAsset { get { return skeletonDataAsset; } }

		[SpineSkin(dataField:"skeletonDataAsset", defaultAsEmptyString:true)]
		public string initialSkinName;
		public bool initialFlipX, initialFlipY;

		[SpineAnimation(dataField:"skeletonDataAsset")]
		public string startingAnimation;
		public bool startingLoop;
		public float timeScale = 1f;
		public bool freeze;
		public bool unscaledTime;

		private Texture baseTexture = null;

		#if UNITY_EDITOR
		protected override void OnValidate () {
			// This handles Scene View preview.
			base.OnValidate ();
			if (this.IsValid) {
				if (skeletonDataAsset == null) {
					Clear();
				} else if (skeletonDataAsset.skeletonJSON == null) {
					Clear();
				} else if (skeletonDataAsset.GetSkeletonData(true) != skeleton.data) {
					Clear();
					Initialize(true);
					if (skeletonDataAsset.atlasAssets.Length > 1 || skeletonDataAsset.atlasAssets[0].MaterialCount > 1)
						Debug.LogError("Unity UI does not support multiple textures per Renderer. Your skeleton will not be rendered correctly. Recommend using SkeletonAnimation instead. This requires the use of a Screen space camera canvas.");
				} else {
					if (freeze) return;

					if (!string.IsNullOrEmpty(initialSkinName)) {
						var skin = skeleton.data.FindSkin(initialSkinName);
						if (skin != null) {
							if (skin == skeleton.data.defaultSkin)
								skeleton.SetSkin((Skin)null);
							else
								skeleton.SetSkin(skin);
						}

					}

					// Only provide visual feedback to inspector changes in Unity Editor Edit mode.
					if (!Application.isPlaying) {
						skeleton.ScaleX = this.initialFlipX ? -1 : 1;
						skeleton.ScaleY = this.initialFlipY ? -1 : 1;

						state.ClearTrack(0);
						skeleton.SetToSetupPose();
						if (!string.IsNullOrEmpty(startingAnimation)) {
							state.SetAnimation(0, startingAnimation, startingLoop);
							Update(0f);
						}
					}
				}
			} else {
				// Under some circumstances (e.g. sometimes on the first import) OnValidate is called
				// before SpineEditorUtilities.ImportSpineContent, causing an unnecessary exception.
				// The (skeletonDataAsset.skeletonJSON != null) condition serves to prevent this exception.
				if (skeletonDataAsset != null && skeletonDataAsset.skeletonJSON != null)
					Initialize(true);
			}
		}

		protected override void Reset () {

			base.Reset();
			if (material == null || material.shader != Shader.Find("Spine/SkeletonGraphic"))
				Debug.LogWarning("SkeletonGraphic works best with the SkeletonGraphic material.");
		}
		#endif
		#endregion

		#region Runtime Instantiation
		/// <summary>Create a new GameObject with a SkeletonGraphic component.</summary>
		/// <param name="material">Material for the canvas renderer to use. Usually, the default SkeletonGraphic material will work.</param>
		public static SkeletonGraphic NewSkeletonGraphicGameObject (SkeletonDataAsset skeletonDataAsset, Transform parent, Material material) {
			var sg = SkeletonGraphic.AddSkeletonGraphicComponent(new GameObject("New Spine GameObject"), skeletonDataAsset, material);
			if (parent != null) sg.transform.SetParent(parent, false);
			return sg;
		}

		/// <summary>Add a SkeletonGraphic component to a GameObject.</summary>
		/// <param name="material">Material for the canvas renderer to use. Usually, the default SkeletonGraphic material will work.</param>
		public static SkeletonGraphic AddSkeletonGraphicComponent (GameObject gameObject, SkeletonDataAsset skeletonDataAsset, Material material) {
			var c = gameObject.AddComponent<SkeletonGraphic>();
			if (skeletonDataAsset != null) {
				c.material = material;
				c.skeletonDataAsset = skeletonDataAsset;
				c.Initialize(false);
			}
			return c;
		}
		#endregion

		#region Internals
		// This is used by the UI system to determine what to put in the MaterialPropertyBlock.
		Texture overrideTexture;
		public Texture OverrideTexture {
			get { return overrideTexture; }
			set {
				overrideTexture = value;
				canvasRenderer.SetTexture(this.mainTexture); // Refresh canvasRenderer's texture. Make sure it handles null.
			}
		}
		public override Texture mainTexture {
			get {
				if (overrideTexture != null) return overrideTexture;
				return baseTexture;
			}
		}

		protected override void Awake () {

			base.Awake ();
			if (!this.IsValid) {
#if UNITY_EDITOR
				// workaround for special import case of open scene where OnValidate and Awake are
				// called in wrong order, before setup of Spine assets.
				if (!Application.isPlaying) {
					if (this.skeletonDataAsset != null && this.skeletonDataAsset.skeletonJSON == null)
						return;
				}
#endif
				Initialize(false);
				Rebuild(CanvasUpdate.PreRender);
			}
		}

		public override void Rebuild (CanvasUpdate update) {
			base.Rebuild(update);
			if (canvasRenderer.cull) return;
			if (update == CanvasUpdate.PreRender) UpdateMesh();
		}

		public virtual void Update () {
			#if UNITY_EDITOR
			if (!Application.isPlaying) {
				Update(0f);
				return;
			}
			#endif

			if (freeze) return;
			Update(unscaledTime ? Time.unscaledDeltaTime : Time.deltaTime);
		}

		public virtual void Update (float deltaTime) {
			if (!this.IsValid) return;

			deltaTime *= timeScale;
			skeleton.Update(deltaTime);
			state.Update(deltaTime);
			state.Apply(skeleton);

			if (UpdateLocal != null) UpdateLocal(this);

			skeleton.UpdateWorldTransform();

			if (UpdateWorld != null) {
				UpdateWorld(this);
				skeleton.UpdateWorldTransform();
			}

			if (UpdateComplete != null) UpdateComplete(this);
		}

		public void LateUpdate () {
			if (freeze) return;
			//this.SetVerticesDirty(); // Which is better?
			UpdateMesh();
		}
		#endregion

		#region API
		protected Skeleton skeleton;
		public Skeleton Skeleton { get { return skeleton; } set { skeleton = value; } }
		public SkeletonData SkeletonData { get { return skeleton == null ? null : skeleton.data; } }
		public bool IsValid { get { return skeleton != null; } }

		protected Spine.AnimationState state;
		public Spine.AnimationState AnimationState { get { return state; } }

		[SerializeField] protected Spine.Unity.MeshGenerator meshGenerator = new MeshGenerator();
		public Spine.Unity.MeshGenerator MeshGenerator { get { return this.meshGenerator; } }
		DoubleBuffered<Spine.Unity.MeshRendererBuffers.SmartMesh> meshBuffers;
		SkeletonRendererInstruction currentInstructions = new SkeletonRendererInstruction();

		public Mesh GetLastMesh () {
			return meshBuffers.GetCurrent().mesh;
		}

		public event UpdateBonesDelegate UpdateLocal;
		public event UpdateBonesDelegate UpdateWorld;
		public event UpdateBonesDelegate UpdateComplete;

		/// <summary> Occurs after the vertex data populated every frame, before the vertices are pushed into the mesh.</summary>
		public event Spine.Unity.MeshGeneratorDelegate OnPostProcessVertices;

		public void Clear () {
			skeleton = null;
			canvasRenderer.Clear();
		}

		public void Initialize (bool overwrite) {
			if (this.IsValid && !overwrite) return;

			// Make sure none of the stuff is null
			if (this.skeletonDataAsset == null) return;
			var skeletonData = this.skeletonDataAsset.GetSkeletonData(false);
			if (skeletonData == null) return;

			if (skeletonDataAsset.atlasAssets.Length <= 0 || skeletonDataAsset.atlasAssets[0].MaterialCount <= 0) return;

			this.state = new Spine.AnimationState(skeletonDataAsset.GetAnimationStateData());
			if (state == null) {
				Clear();
				return;
			}

			this.skeleton = new Skeleton(skeletonData) {
				ScaleX = this.initialFlipX ? -1 : 1,
				ScaleY = this.initialFlipY ? -1 : 1
			};

			meshBuffers = new DoubleBuffered<MeshRendererBuffers.SmartMesh>();
			baseTexture = skeletonDataAsset.atlasAssets[0].PrimaryMaterial.mainTexture;
			canvasRenderer.SetTexture(this.mainTexture); // Needed for overwriting initializations.

			// Set the initial Skin and Animation
			if (!string.IsNullOrEmpty(initialSkinName))
				skeleton.SetSkin(initialSkinName);

			if (!string.IsNullOrEmpty(startingAnimation)) {
				var animationObject = skeletonDataAsset.GetSkeletonData(false).FindAnimation(startingAnimation);
				if (animationObject != null) {
					state.SetAnimation(0, animationObject, startingLoop);
					#if UNITY_EDITOR
					if (!Application.isPlaying)
						Update(0f);
					#endif
					if (freeze)
						Update(0f);
				}
			}
		}

		public void UpdateMesh () {
			if (!this.IsValid) return;

			skeleton.SetColor(this.color);
			var smartMesh = meshBuffers.GetNext();
			var currentInstructions = this.currentInstructions;
			MeshGenerator.GenerateSingleSubmeshInstruction(currentInstructions, skeleton, null);
			bool updateTriangles = SkeletonRendererInstruction.GeometryNotEqual(currentInstructions, smartMesh.instructionUsed);

			meshGenerator.Begin();
			if (currentInstructions.hasActiveClipping) {
				meshGenerator.AddSubmesh(currentInstructions.submeshInstructions.Items[0], updateTriangles);
			} else {
				meshGenerator.BuildMeshWithArrays(currentInstructions, updateTriangles);
			}

			if (canvas != null) meshGenerator.ScaleVertexData(canvas.referencePixelsPerUnit);
			if (OnPostProcessVertices != null) OnPostProcessVertices.Invoke(this.meshGenerator.Buffers);

			var mesh = smartMesh.mesh;
			meshGenerator.FillVertexData(mesh);
			if (updateTriangles) meshGenerator.FillTriangles(mesh);
			meshGenerator.FillLateVertexData(mesh);

			canvasRenderer.SetMesh(mesh);
			smartMesh.instructionUsed.Set(currentInstructions);

			if (currentInstructions.submeshInstructions.Count > 0) {
				var material = currentInstructions.submeshInstructions.Items[0].material;
				if (material != null && baseTexture != material.mainTexture) {
					baseTexture = material.mainTexture;
					if (overrideTexture == null)
						canvasRenderer.SetTexture(this.mainTexture);
				}
			}

			//this.UpdateMaterial(); // TODO: This allocates memory.
		}
		#endregion
	}
}
