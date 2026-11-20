using System;
using UnityEditor;
using UnityEngine;

namespace TargetRange.EditorTools
{
    /// <summary>
    /// Sets [SerializeField] private fields from editor code, exactly like dragging a reference into
    /// the Inspector. A missing field name throws, so a typo can never silently leave a slot empty.
    /// </summary>
    internal static class Wire
    {
        private static SerializedProperty Prop(SerializedObject so, string field)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) throw new Exception($"Field '{field}' not found on {so.targetObject.GetType().Name}");
            return p;
        }

        public static void Ref(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target);
            Prop(so, field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Refs(UnityEngine.Object target, string field, UnityEngine.Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty array = Prop(so, field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Float(UnityEngine.Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            Prop(so, field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Int(UnityEngine.Object target, string field, int value)
        {
            var so = new SerializedObject(target);
            Prop(so, field).intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Enum(UnityEngine.Object target, string field, int index)
        {
            var so = new SerializedObject(target);
            Prop(so, field).enumValueIndex = index;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Vec3(UnityEngine.Object target, string field, Vector3 value)
        {
            var so = new SerializedObject(target);
            Prop(so, field).vector3Value = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Bool(UnityEngine.Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            Prop(so, field).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
