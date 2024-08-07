
using System;

namespace Spine {

	/// <summary>Stores the setup pose and all of the stateless data for a skeleton.</summary>
	public class SkeletonData {
		internal string name;
		internal ExposedList<BoneData> bones = new ExposedList<BoneData>(); // Ordered parents first
		internal ExposedList<SlotData> slots = new ExposedList<SlotData>(); // Setup pose draw order.
		internal ExposedList<Skin> skins = new ExposedList<Skin>();
		internal Skin defaultSkin;
		internal ExposedList<EventData> events = new ExposedList<EventData>();
		internal ExposedList<Animation> animations = new ExposedList<Animation>();
		internal ExposedList<IkConstraintData> ikConstraints = new ExposedList<IkConstraintData>();
		internal ExposedList<TransformConstraintData> transformConstraints = new ExposedList<TransformConstraintData>();
		internal ExposedList<PathConstraintData> pathConstraints = new ExposedList<PathConstraintData>();
		internal float x , y, width, height;
		internal string version, hash;

		// Nonessential.
		internal float fps;
		internal string imagesPath, audioPath;

		public string Name { get { return name; } set { name = value; } }

		/// <summary>The skeleton's bones, sorted parent first. The root bone is always the first bone.</summary>
		public ExposedList<BoneData> Bones { get { return bones; } }

		public ExposedList<SlotData> Slots { get { return slots; } }

		/// <summary>All skins, including the default skin.</summary>
		public ExposedList<Skin> Skins { get { return skins; } set { skins = value; } }

		/// <summary>
		/// The skeleton's default skin.
		/// By default this skin contains all attachments that were not in a skin in Spine.
		/// </summary>
		/// <return>May be null.</return>
		public Skin DefaultSkin { get { return defaultSkin; } set { defaultSkin = value; } }

		public ExposedList<EventData> Events { get { return events; } set { events = value; } }
		public ExposedList<Animation> Animations { get { return animations; } set { animations = value; } }
		public ExposedList<IkConstraintData> IkConstraints { get { return ikConstraints; } set { ikConstraints = value; } }
		public ExposedList<TransformConstraintData> TransformConstraints { get { return transformConstraints; } set { transformConstraints = value; } }
		public ExposedList<PathConstraintData> PathConstraints { get { return pathConstraints; } set { pathConstraints = value; } }

		public float X { get { return x; } set { x = value; } }
		public float Y { get { return y; } set { y = value; } }
		public float Width { get { return width; } set { width = value; } }
		public float Height { get { return height; } set { height = value; } }
		/// <summary>The Spine version used to export this data, or null.</summary>
		public string Version { get { return version; } set { version = value; } }
		public string Hash { get { return hash; } set { hash = value; } }

		/// <summary>The path to the images directory as defined in Spine. Available only when nonessential data was exported. May be null</summary>
		public string ImagesPath { get { return imagesPath; } set { imagesPath = value; } }

		/// <summary>The path to the audio directory defined in Spine. Available only when nonessential data was exported. May be null.</summary>
		public string AudioPath { get { return audioPath; } set { audioPath = value; } }

		/// <summary>
		/// The dopesheet FPS in Spine. Available only when nonessential data was exported.</summary>
		public float Fps { get { return fps; } set { fps = value; } }

		// --- Bones.

		/// <summary>
		/// Finds a bone by comparing each bone's name.
		/// It is more efficient to cache the results of this method than to call it multiple times.</summary>
		/// <returns>May be null.</returns>
		public BoneData FindBone (string boneName) {
			if (boneName == null) throw new ArgumentNullException("boneName", "boneName cannot be null.");
			var bones = this.bones;
			var bonesItems = bones.Items;
			for (int i = 0, n = bones.Count; i < n; i++) {
				BoneData bone = bonesItems[i];
				if (bone.name == boneName) return bone;
			}
			return null;
		}

		/// <returns>-1 if the bone was not found.</returns>
		public int FindBoneIndex (string boneName) {
			if (boneName == null) throw new ArgumentNullException("boneName", "boneName cannot be null.");
			var bones = this.bones;
			var bonesItems = bones.Items;
			for (int i = 0, n = bones.Count; i < n; i++)
				if (bonesItems[i].name == boneName) return i;
			return -1;
		}

		// --- Slots.

		/// <returns>May be null.</returns>
		public SlotData FindSlot (string slotName) {
			if (slotName == null) throw new ArgumentNullException("slotName", "slotName cannot be null.");
			ExposedList<SlotData> slots = this.slots;
			for (int i = 0, n = slots.Count; i < n; i++) {
				SlotData slot = slots.Items[i];
				if (slot.name == slotName) return slot;
			}
			return null;
		}

		/// <returns>-1 if the slot was not found.</returns>
		public int FindSlotIndex (string slotName) {
			if (slotName == null) throw new ArgumentNullException("slotName", "slotName cannot be null.");
			ExposedList<SlotData> slots = this.slots;
			for (int i = 0, n = slots.Count; i < n; i++)
				if (slots.Items[i].name == slotName) return i;
			return -1;
		}

		// --- Skins.

		/// <returns>May be null.</returns>
		public Skin FindSkin (string skinName) {
			if (skinName == null) throw new ArgumentNullException("skinName", "skinName cannot be null.");
			foreach (Skin skin in skins)
				if (skin.name == skinName) return skin;
			return null;
		}

		// --- Events.

		/// <returns>May be null.</returns>
		public EventData FindEvent (string eventDataName) {
			if (eventDataName == null) throw new ArgumentNullException("eventDataName", "eventDataName cannot be null.");
			foreach (EventData eventData in events)
				if (eventData.name == eventDataName) return eventData;
			return null;
		}

		// --- Animations.

		/// <returns>May be null.</returns>
		public Animation FindAnimation (string animationName) {
			if (animationName == null) throw new ArgumentNullException("animationName", "animationName cannot be null.");
			ExposedList<Animation> animations = this.animations;
			for (int i = 0, n = animations.Count; i < n; i++) {
				Animation animation = animations.Items[i];
				if (animation.name == animationName) return animation;
			}
			return null;
		}

		// --- IK constraints.

		/// <returns>May be null.</returns>
		public IkConstraintData FindIkConstraint (string constraintName) {
			if (constraintName == null) throw new ArgumentNullException("constraintName", "constraintName cannot be null.");
			ExposedList<IkConstraintData> ikConstraints = this.ikConstraints;
			for (int i = 0, n = ikConstraints.Count; i < n; i++) {
				IkConstraintData ikConstraint = ikConstraints.Items[i];
				if (ikConstraint.name == constraintName) return ikConstraint;
			}
			return null;
		}

		// --- Transform constraints.

		/// <returns>May be null.</returns>
		public TransformConstraintData FindTransformConstraint (string constraintName) {
			if (constraintName == null) throw new ArgumentNullException("constraintName", "constraintName cannot be null.");
			ExposedList<TransformConstraintData> transformConstraints = this.transformConstraints;
			for (int i = 0, n = transformConstraints.Count; i < n; i++) {
				TransformConstraintData transformConstraint = transformConstraints.Items[i];
				if (transformConstraint.name == constraintName) return transformConstraint;
			}
			return null;
		}

		// --- Path constraints.

		/// <returns>May be null.</returns>
		public PathConstraintData FindPathConstraint (string constraintName) {
			if (constraintName == null) throw new ArgumentNullException("constraintName", "constraintName cannot be null.");
			ExposedList<PathConstraintData> pathConstraints = this.pathConstraints;
			for (int i = 0, n = pathConstraints.Count; i < n; i++) {
				PathConstraintData constraint = pathConstraints.Items[i];
				if (constraint.name.Equals(constraintName)) return constraint;
			}
			return null;
		}

		/// <returns>-1 if the path constraint was not found.</returns>
		public int FindPathConstraintIndex (string pathConstraintName) {
			if (pathConstraintName == null) throw new ArgumentNullException("pathConstraintName", "pathConstraintName cannot be null.");
			ExposedList<PathConstraintData> pathConstraints = this.pathConstraints;
			for (int i = 0, n = pathConstraints.Count; i < n; i++)
				if (pathConstraints.Items[i].name.Equals(pathConstraintName)) return i;
			return -1;
		}

		// ---

		override public string ToString () {
			return name ?? base.ToString();
		}
	}
}
