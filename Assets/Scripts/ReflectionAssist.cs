using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ReflectionAssist : MonoBehaviour
{
    public void Awake()
    {
        ServiceProvider.Instance.DevConsole.RegisterCommand("DebugTargetingSystem", DebugTargetingSystem);
    }

    public void DebugType(Type t)
    {
        Debug.Log(string.Format("ReflectionAssist of {0}:", t.FullName));

        FieldInfo[] fieldInfo = t.GetFields();
        string fieldDebug = string.Format("> {0} fields in {1}", fieldInfo.Length, t.Name);
        foreach(FieldInfo f in fieldInfo)
        {
            string fAccess = f.IsPublic ? "public" : (f.IsPrivate ? "private" : "----");
            string fType = f.FieldType.Name;
            string fName = f.Name;
            fieldDebug += string.Format("\n    > {0} {1} {2}", fAccess, fType, fName);
        }
        Debug.Log(fieldDebug);

        PropertyInfo[] propertyInfo = t.GetProperties();
        string propertyDebug = string.Format("> {0} properties in {1}", propertyInfo.Length, t.Name);
        foreach (PropertyInfo p in propertyInfo)
        {
            string pAccessRead = p.CanRead ? "R" : "";
            string pAccessWrite = p.CanWrite ? "W" : "";
            string pType = p.PropertyType.Name;
            string pName = p.Name;
            fieldDebug += string.Format("\n    > {0}{1} {2} {3}", pAccessRead, pAccessWrite, pType, pName);
        }
        Debug.Log(propertyDebug);
    }

    public void DebugTargetingSystem()
    {
        Component[] components = GetComponents<Component>();
        Type AircraftScriptType = null;
        Type TargetingSystemType = null;

        foreach (Component c in components)
        {
            if (c.GetType().Name == "AircraftScript")
            {
                AircraftScriptType = c.GetType();
                break;
            }
        }

        MemberInfo[] members = AircraftScriptType.GetMembers();
        foreach (MemberInfo m in members)
        {
            if (m.GetType().Name == "TargetingSystem")
            {
                TargetingSystemType = m.GetType();
                DebugType(TargetingSystemType);
                break;
            }
        }
    }
}
