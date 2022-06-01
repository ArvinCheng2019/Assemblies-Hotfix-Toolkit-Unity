using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace zFramework.Hotfix.Toolkit
{
    [CustomPropertyDrawer(typeof(HotfixAssemblyInfo))]
    public class HotfixAssemblyInfoDrawer : PropertyDrawer
    {
        GUIStyle style;
        SimpleAssemblyInfo info = new SimpleAssemblyInfo();
        string message;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            position.height = EditorGUIUtility.singleLineHeight + 2;
            var indent = EditorGUI.indentLevel;
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUI.indentLevel = 0;

            #region Init Related  Propertys
            var assemblis = property.FindPropertyRelative("assembly");
            var bytesAsset = property.FindPropertyRelative("bytesAsset");
            var asm = assemblis.objectReferenceValue as AssemblyDefinitionAsset;
            var bts = bytesAsset.objectReferenceValue as TextAsset;
            #endregion

            property.serializedObject.Update();
            EditorGUI.BeginProperty(position, label, property);

            #region Draw title
            string name = asm ? asm.name : "����δָ��";
            var color = EditorStyles.foldout.normal.textColor;
            if (null == style)
            {
                style = new GUIStyle(EditorStyles.foldout);
            }
            style.normal.textColor = asm ? color : Color.red;
            if (!EditorGUIUtility.hierarchyMode)
            {
                EditorGUI.indentLevel--;
            }
            EditorGUIUtility.labelWidth = position.width;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, new GUIContent(name), style);
            EditorGUIUtility.labelWidth = labelWidth;
            if (!EditorGUIUtility.hierarchyMode)
            {
                EditorGUI.indentLevel++;
            }
            #endregion

            if (property.isExpanded)
            {
                #region ���� ���� �����ļ��ֶ�
                position.y += position.height + 6;
                var field_rect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);
                message = HandleDragAndDrop(field_rect, property);
                Debug.Log($"{nameof(HotfixAssemblyInfoDrawer)}: after HandleDragAndDrop {message} ");
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUI.PropertyField(position, assemblis);

                    if (check.changed)
                    {
                        property.serializedObject.ApplyModifiedProperties();

                        #region ת���ļ���Ч����֤��assembly ��Ϊ���������� ת���ļ�����ƥ��
                        asm = assemblis.objectReferenceValue as AssemblyDefinitionAsset;
                        bts = bytesAsset.objectReferenceValue as TextAsset;

                        Debug.Log($"{nameof(HotfixAssemblyInfoDrawer)}:  some changed to {asm?.name}");

                        if (bts && (!asm || !bts.name.Contains(asm.name)))
                        {
                            Undo.RecordObject(property.serializedObject.targetObject, "RemoveTypeMissmatchedBytesFile");
                            bytesAsset.objectReferenceValue = null;
                        }

                        #endregion

                    }
                }
                #endregion
                #region Draw Message
                Rect tip_rect = default;

                if (!string.IsNullOrEmpty(message))
                {
                    Debug.Log($"Assembly Hotfix Toolkit: {message}");
                    position.y += position.height + 4;
                    var height = EditorGUI.GetPropertyHeight(SerializedPropertyType.String, new GUIContent(message));
                    tip_rect = new Rect(position.x, position.y, position.width, height);
                    EditorGUI.HelpBox(tip_rect, message, MessageType.Warning);
                }
                #endregion


                #region ���� ת�� dll �ֶ�
                var enable = GUI.enabled;
                GUI.enabled = false;
                position.y += position.height + 4;
                EditorGUI.PropertyField(position, bytesAsset);
                GUI.enabled = enable;
                #endregion

            }
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var assemblis = property.FindPropertyRelative("assembly");
            var bytesAsset = property.FindPropertyRelative("bytesAsset");

            var height = EditorGUIUtility.singleLineHeight + 6;
            height += EditorGUI.GetPropertyHeight(assemblis) + 4;
            height += EditorGUI.GetPropertyHeight(bytesAsset) + 4;

            if (!string.IsNullOrEmpty(message))
            {
                Debug.Log($"{nameof(HotfixAssemblyInfoDrawer)}:  inside {message}");
                height += EditorGUI.GetPropertyHeight(SerializedPropertyType.String, new GUIContent(message)) + 8;
            }
            return property.isExpanded ? height : base.GetPropertyHeight(property, label);
        }

        private string HandleDragAndDrop(Rect field_rect, SerializedProperty property)
        {
            bool isDragging = Event.current.type == EventType.DragUpdated && field_rect.Contains(Event.current.mousePosition);
            bool isDropping = Event.current.type == EventType.DragPerform && field_rect.Contains(Event.current.mousePosition);
            var rejectedDrag = true;
            bool isEditorAssembly = false;
            bool isUsedByAssemblyCsharp = false;
            bool isDuplicated = false;
            Debug.Log($"{nameof(HotfixAssemblyInfoDrawer)}: {property.FindPropertyRelative("assembly").objectReferenceValue?.name}  -  {isDragging} - {message}");
            if (isDragging)
            {
                if (DragAndDrop.objectReferences[0] is AssemblyDefinitionAsset asmdef)
                {
                    #region ����
                    var ahm = property.serializedObject.targetObject as AssemblyHotfixManager;
                    isDuplicated = ahm.assemblies.Any(v => v.assembly && v.assembly.name == asmdef.name);
                    #endregion
                    if (!isDuplicated)
                    {
                        EditorJsonUtility.FromJsonOverwrite(asmdef.text, info);
                        isEditorAssembly = null != info.includePlatforms && info.includePlatforms.Length == 1 && info.includePlatforms[0] == "Editor";
                        isUsedByAssemblyCsharp = AssemblyHotfixManager.IsUsedByAssemblyCSharp(info.name);

                        if (isEditorAssembly)
                        {
                            message = $"�༭�����򼯲����ȸ��� ";
                        }
                        if (isUsedByAssemblyCsharp)
                        {
                            message = $"�� Assembly-CSharp ��س������ò����ȸ��� ";
                        }
                    }
                    else
                    {
                        message = $"�Ѵ��ڣ������ظ���ӣ� ";
                    }
                    rejectedDrag = isEditorAssembly || isUsedByAssemblyCsharp || isDuplicated;
                    DragAndDrop.visualMode = rejectedDrag ? DragAndDropVisualMode.Rejected : DragAndDropVisualMode.Generic;
                }
            }
            if (!rejectedDrag && isDropping)
            {
                property.objectReferenceValue = DragAndDrop.objectReferences[0];
                Debug.LogError($"{nameof(HotfixAssemblyInfoDrawer)}:  asign  data");
            }
            return message;
        }

        [Serializable]
        public class SimpleAssemblyInfo
        {
            public string name;
            public string[] includePlatforms;
        }
    }
}
