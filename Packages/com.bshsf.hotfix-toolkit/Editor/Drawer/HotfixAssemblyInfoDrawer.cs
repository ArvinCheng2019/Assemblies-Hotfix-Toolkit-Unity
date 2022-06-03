using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace zFramework.Hotfix.Toolkit
{
    using static AssemblyHotfixManager;
    [CustomPropertyDrawer(typeof(HotfixAssemblyInfo))]
    public class HotfixAssemblyInfoDrawer : PropertyDrawer
    {
        static Dictionary<string, DrawerState> drawerState = new Dictionary<string, DrawerState>();
        internal class DrawerState
        {
            public GUIStyle style;
            public float height;
            public string message;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {

            property.serializedObject.Update();
            position.height = EditorGUIUtility.singleLineHeight + 2;
            var indent = EditorGUI.indentLevel;
            var labelWidth = EditorGUIUtility.labelWidth;
            TextAsset bts = default;
            EditorGUI.indentLevel = 0;

            #region Initial Drawer State
            var path = property.FindPropertyRelative("assembly").propertyPath;
            var data = drawerState[path];
            #endregion

            EditorGUI.BeginProperty(position, label, property);

            #region Draw title
            var asm = property.FindPropertyRelative("assembly").objectReferenceValue as AssemblyDefinitionAsset;
            string name = !string.IsNullOrEmpty(data.message) || !asm ? "���������쳣" : asm.name;
            var color = EditorStyles.foldout.normal.textColor;
            data.style.normal.textColor = asm ? color : Color.red;
            if (!EditorGUIUtility.hierarchyMode)
            {
                EditorGUI.indentLevel--;
            }
            EditorGUIUtility.labelWidth = position.width;
            property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, new GUIContent(name), data.style);
            EditorGUIUtility.labelWidth = labelWidth;
            if (!EditorGUIUtility.hierarchyMode)
            {
                EditorGUI.indentLevel++;
            }
            #endregion
            var orign = position;
            if (property.isExpanded)
            {

                #region Assembly ��Ч��У��
                asm = property.FindPropertyRelative("assembly").objectReferenceValue as AssemblyDefinitionAsset;
                if (asm)
                {
                    if (IsEditorAssembly(asm)) // ���Ƿ�Ϊ�༭���ű�
                    {
                        data.message = $"�༭�����򼯲����ȸ��� ";
                    }
                    else if (IsUsedByAssemblyCSharp(asm)) // ���Ƿ�Unity������������
                    {
                        data.message = $"�� Assembly-CSharp ��س������ò����ȸ��� ";
                    }

                }
                else
                {
                    data.message = $"��ָ����Ҫ�ȸ��ĳ��򼯣� ";
                }
                #endregion


                #region ���� ���� �����ļ��ֶ�
                position.y += position.height + 6;
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    EditorGUI.PropertyField(position, property.FindPropertyRelative("assembly"));
                    if (check.changed)
                    {
                        property.serializedObject.ApplyModifiedProperties();

                        if (IsAssemblyDuplicated(asm))// ����
                        {
                            data.message = $"�����ظ���ӵĳ��򼯣� ";
                        }

                        #region ת���ļ���Ч����֤��assembly ��Ϊ���������� ת���ļ�����ƥ��
                        asm = property.FindPropertyRelative("assembly").objectReferenceValue as AssemblyDefinitionAsset;
                        bts = property.FindPropertyRelative("bytesAsset").objectReferenceValue as TextAsset;

                        if (bts && (!asm || !bts.name.Equals(GetAssemblyName(asm))))
                        {
                            Undo.RecordObject(property.serializedObject.targetObject, "RemoveTypeMissmatchedBytesFile");
                            property.FindPropertyRelative("bytesAsset").objectReferenceValue = null;
                            property.serializedObject.ApplyModifiedProperties();
                        }
                        Editor editor = default;
                        Editor.CreateCachedEditor(property.serializedObject.targetObject, typeof(AssemblyHotfixManagerEditor), ref editor);
                        editor.Repaint();
                        #endregion
                    }
                }

                #endregion
                #region Draw Message
                Rect tip_rect = default;
                if (!string.IsNullOrEmpty(data.message))
                {
                    position.y += position.height + 4;
                    var height = EditorGUI.GetPropertyHeight(SerializedPropertyType.String, new GUIContent(data.message));
                    tip_rect = new Rect(position.x, position.y, position.width, height);
                    EditorGUI.HelpBox(tip_rect, data.message, MessageType.Error);
                    data.message = string.Empty;
                }
                #endregion


                #region ���� ת�� dll �ֶ�
                var enable = GUI.enabled;
                GUI.enabled = false;
                position.y += position.height + 4;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("bytesAsset"));
                GUI.enabled = enable;
                #endregion
            }
            else
            {

            }
            drawerState[path].height = position.y - orign.y + EditorGUIUtility.singleLineHeight + 2;
            EditorGUI.indentLevel = indent;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var assemblis = property.FindPropertyRelative("assembly");
            var path = assemblis.propertyPath;
            if (!drawerState.TryGetValue(path, out var data))
            {
                data = new DrawerState();
                data.style = new GUIStyle(EditorStyles.foldout);
                drawerState[path] = data;
                data.height = base.GetPropertyHeight(property, label);
            }
            return data.height;
        }


    }
}
