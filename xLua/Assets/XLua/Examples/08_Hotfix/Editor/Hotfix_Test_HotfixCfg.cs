using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using XLua;

//如果涉及到Assembly-CSharp.dll之外的其它dll，如下代码需要放到Editor目录
namespace XLuaTest {

    public static class HotfixCfg {
        [Hotfix]
        public static List<Type> by_field = new List<Type>() {
            typeof(HotfixTest),
            typeof(HotfixCalc),
            typeof(GenericClass<>),
            typeof(InnerTypeTest),
            typeof(BaseTest),
            typeof(StructTest),
            typeof(GenericStruct<>),
            typeof(StatefullTest),
        };

        [Hotfix]
        public static List<Type> by_property {
            get {
                return (from type in Assembly.Load("Assembly-CSharp").GetTypes()
                        where type.Namespace == "XXXX"
                        select type).ToList();
            }
        }
    }
}
