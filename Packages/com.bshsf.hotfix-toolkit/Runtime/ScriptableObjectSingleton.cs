using System;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace zFramework.Hotfix.Toolkit
{
    public class ScriptableObjectSingleton<T> : ScriptableObject where T : ScriptableObject
    {
        static T instance;
        static object _lock = new object();
        public static T Instance => GetInstance();
        private static T GetInstance()
        {
#if UNITY_EDITOR
            lock (_lock)
            {
                if (!instance)
                {
                    Type t = typeof(T);
                    var attr = t.GetCustomAttributes(typeof(SingletonParamAttribute), false);
                    if (attr.Length > 0)
                    {
                        var abPath = (attr[0] as SingletonParamAttribute).path;
                        var path = Path.Combine(Application.dataPath, abPath);
                        var dir = new DirectoryInfo(path);
                        if (!dir.Exists)
                        {
                            dir.Create();
                        }
                        var file = $"Assets/{abPath}/{ObjectNames.NicifyVariableName(t.Name)}.asset";
                        instance = AssetDatabase.LoadAssetAtPath<T>(file);
                        if (!instance)
                        {
                            instance = CreateInstance(t) as T;
                            AssetDatabase.CreateAsset(instance, file);
                            AssetDatabase.Refresh();
                            //todo : �������Ϸ��������Ϊ��Ѱַ���󣬰���������� ���򼯣����ֻ��Ҫ�û��Լ����� host ����
                            //https://forum.unity.com/threads/set-asset-as-addressable-through-script.718751/
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"ScriptableObject �ĵ������ʹ�� {nameof(SingletonParamAttribute)} ָ�� asset �洢·��!");
                    }
                }
            }
#endif
            return instance;
        }
        public virtual void OnAssetCreated() { }
    }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class SingletonParamAttribute : Attribute
    {
        public string path;
        public bool addressable;
        public SingletonParamAttribute(string path, bool addressable = false)
        {
            this.addressable = addressable;
            if (!string.IsNullOrEmpty(path))
            {
                this.path = path;
            }
            else
            {
                throw new InvalidOperationException("ScriptableObject �ĵ��� asset �洢·������Ϊ��!");
            }
        }
    }
}
