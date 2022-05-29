using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace zFramework.Hotfix.Toolkit
{
    [CustomPropertyDrawer(typeof(HotfixAssemblyInfo))]
    public class HotfixAssemblyInfoDrawer : PropertyDrawer
    {
        private float height;
        GUIStyle style;
        bool isEditorAssembly = false;
        bool isUsedByAssemblyCsharp = false;
        bool isDuplicated = false;
        bool hasValidate = false;

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

            EditorGUI.BeginProperty(position, label, property);
            property.serializedObject.Update();

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

            var temp = position;
            if (property.isExpanded)
            {
                #region ���� ���� �����ļ��ֶ�
                position.y += position.height + 6;
                var field_rect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);
                bool isDragging = Event.current.type == EventType.DragUpdated && field_rect.Contains(Event.current.mousePosition);
                bool isDropping = Event.current.type == EventType.DragPerform && field_rect.Contains(Event.current.mousePosition);

                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUI.PropertyField(position, assemblis);
                    HandleDragAndDrop(property, isDragging, isDropping);

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








                #region ���� ת�� dll �ֶ�
                var enable = GUI.enabled;
                GUI.enabled = false;
                position.y += position.height + 4;
                EditorGUI.PropertyField(position, bytesAsset);
                GUI.enabled = enable;
                #endregion

                height = position.y - temp.y + position.height;
            }
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {

            return property.isExpanded ? this.height : base.GetPropertyHeight(property, label);
        }

        private void HandleDragAndDrop(SerializedProperty property, bool isDragging, bool isDropping)
        {
            var rejectedDrag = true;
            if (isDragging)
            {
                if (DragAndDrop.objectReferences[0] is AssemblyDefinitionAsset asmdef)
                {
                    if (!hasValidate)
                    {
                        hasValidate = true;
                        Debug.Log($"{nameof(HotfixAssemblyInfoDrawer)}: inside validate");

                        #region ����
                        var ahm = property.serializedObject.targetObject as AssemblyHotfixManager;
                        isDuplicated = ahm.assemblies.Any(v => v.assembly && v.assembly.name == asmdef.name);
                        #endregion
                        if (!isDuplicated)
                        {
                            var info = new SimpleAssemblyInfo();
                            EditorJsonUtility.FromJsonOverwrite(asmdef.text, info);
                            isEditorAssembly = null != info.includePlatforms && info.includePlatforms.Length == 1 && info.includePlatforms[0] == "Editor";
                            isUsedByAssemblyCsharp = AppDomain.CurrentDomain.GetAssemblies()
                                                                                   .Where(v => v.FullName.Contains("Assembly-CSharp"))
                                                                                   .SelectMany(v => v.GetReferencedAssemblies())
                                                                                   .Any(v => v.Name.Equals(info.name));

                            if (isEditorAssembly)
                            {
                                Debug.LogError($"AssemblyHotfixToolkit��{info.name} �Ǳ༭�����򼯣������ȸ��� ");
                            }
                            if (isUsedByAssemblyCsharp)
                            {
                                Debug.LogError($"AssemblyHotfixToolkit��{info.name} �� Assembly-CSharp ��س������ã������ȸ��� ");
                            }
                        }
                        else
                        {
                            Debug.LogError($"AssemblyHotfixToolkit��{asmdef.name} �Ѵ��ڣ������ظ���ӣ� ");
                        }
                    }
                    rejectedDrag = isEditorAssembly || isUsedByAssemblyCsharp || isDuplicated;
                }
                DragAndDrop.visualMode = rejectedDrag ? DragAndDropVisualMode.Rejected : DragAndDropVisualMode.Generic;
            }

            if (Event.current.type== EventType.DragPerform|| Event.current.type == EventType.DragExited||Event.current.type!= EventType.DragUpdated)
            {
                isEditorAssembly = isUsedByAssemblyCsharp = isDuplicated = hasValidate = false;
                Debug.Log($"{nameof(HotfixAssemblyInfoDrawer)}: reset drag drop");
            }

            if (!rejectedDrag && isDropping)
            {
                property.objectReferenceValue = DragAndDrop.objectReferences[0];
                Event.current.Use();
                Debug.Log($"{nameof(HotfixAssemblyInfoDrawer)}:  asign  data");
            }
        }

        [Serializable]
        public class SimpleAssemblyInfo
        {
            public string name;
            public string[] includePlatforms;
        }
    }
}
