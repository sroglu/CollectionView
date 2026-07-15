using PFound.CollectionView.Config;
using UnityEditor;
using UnityEngine;

namespace PFound.CollectionView.EditorTools
{
    /// <summary>
    /// Custom inspector for <see cref="CollectionViewConfig"/>. Draws every serialized field but greys the
    /// bool-gated optional slots (content loader, fixed columns, empty-slot capacity) when their gate is off -
    /// the same visible wire-time contract the Odin <c>[EnableIf]</c> convention gives, implemented with plain
    /// <see cref="EditorGUI.DisabledScope"/> so the runtime module keeps zero third-party (Odin) dependency and
    /// stays liftable into any project.
    /// </summary>
    [CustomEditor(typeof(CollectionViewConfig))]
    public sealed class CollectionViewConfigInspector : UnityEditor.Editor
    {
        // Fields gated by a preceding bool; drawn inside a DisabledScope keyed on that bool's property name.
        static readonly (string Field, string Gate)[] Gated =
        {
            ("_fixedColumns", "_useFixedColumns"),
            ("_contentLoader", "_useContentLoader"),
            ("_emptySlotCapacity", "_useEmptySlotPadding")
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                    continue;
                }

                bool disabled = ShouldDisable(iterator.name);
                using (new EditorGUI.DisabledScope(disabled))
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        bool ShouldDisable(string fieldName)
        {
            for (int i = 0; i < Gated.Length; i++)
            {
                if (Gated[i].Field == fieldName)
                {
                    var gate = serializedObject.FindProperty(Gated[i].Gate);
                    return gate != null && !gate.boolValue;
                }
            }
            return false;
        }
    }
}
