using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public static class MyTestUpdate_HotfixCfg {
    [XLua.Hotfix]
    public static List<Type> by_field = new List<Type>() {
        typeof(MyTestUpdate),
        typeof(VersionSwitch),
    };

    [XLua.Hotfix]
    public static List<Type> by_property {
        get {
            return (from type in Assembly.Load("Assembly-CSharp").GetTypes()
                    where type.Namespace == "XXXX"
                    select type).ToList();
        }
    }
}
