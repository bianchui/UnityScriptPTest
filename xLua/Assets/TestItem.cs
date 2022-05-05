using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;

public abstract class TestItem
{

    protected PTest m_ptest;
    protected int m_index;

    public TestItem(PTest ptest, int index)
    {
        m_ptest = ptest;
        m_index = index;
    }

    public virtual IEnumerator Test() {
        m_ptest.logText += "Test " + m_index + " Begin:\n";
        double totalMS = 0;

        int count = m_ptest.runCount;
        for (int i = 1; i <= count; ++i) {
            m_ptest.GC();
            yield return m_ptest.ws;
            long ts = System.DateTime.Now.Ticks;
            DoTest();
            double t = (double)((System.DateTime.Now.Ticks - ts) / 10000.0);
            totalMS += t;
            m_ptest.logText += string.Format("{0}: ms: {1}\n", i, t);
        }

        m_ptest.logText += string.Format("Test {0} complete average ms: {1}\n", m_index, totalMS / count);
    }

    protected abstract void DoTest();
}

public class TestLua : TestItem
{
    protected string m_luaFuncName;
    Transform m_trans;
    Func<Transform, double> m_f;

    public TestLua(PTest ptest, int index, Transform trans)
        : base(ptest, index)
    {
        m_luaFuncName = "Test" + index;
        m_trans = trans;
    }

    public override IEnumerator Test() {
        m_ptest.scriptEnv.Get(m_luaFuncName, out m_f);
        return base.Test();
    }

    protected override void DoTest() {
        m_f(m_trans);
    }

}

public class TestFFI : TestLua {
    public TestFFI(PTest ptest, int index, Transform trans)
        : base(ptest, index, trans)
    {
        m_luaFuncName = "ffi_test" + index;
    }
}

public class TestFFI2 : TestItem {
    [CSharpCallLua]
    delegate int TestFunction(IntPtr a);

    TestFunction m_f;

    public TestFFI2(PTest ptest, int index) : base(ptest, index) {
        m_f = m_ptest.scriptEnv.Get<TestFunction>("ffi_test3");
    }

    List<FFIClass> _objs = new List<FFIClass>();

    protected override unsafe void DoTest() {
        FFIClass cls;
        //_objs.Add(cls);
        using (cls = new FFIClass()) {
            int count = 200000;
            Vector3* vector3 = cls.getArray(count);
            m_f(cls.handle());
            for (int i = 0; i < count; ++i) {
                if (vector3[i].x != i + 1) {
                    throw new Exception();
                }
                if (vector3[i].y != i + 2) {
                    throw new Exception();
                }
                if (vector3[i].z != i + 3) {
                    throw new Exception();
                }
            }
        }
    }
}

public class TestFFI3 : TestItem {
    delegate int TestFunction(IntPtr a);

    TestFunction m_f;

    public TestFFI3(PTest ptest, int index) : base(ptest, index) {
        m_f = m_ptest.scriptEnv.Get<TestFunction>("ffi_test4");
    }

    protected override unsafe void DoTest() {
        IntPtr ptr = FFIClass.test_ffi_ptr();
        Debug.Log(ptr);
        m_f(ptr);
    }
}
public class TestEmptyFunc : TestItem
{
    Action m_f;

    public TestEmptyFunc(PTest ptest, int index)
        : base(ptest, index)
    {
    }

    public override IEnumerator Test() {
        m_ptest.scriptEnv.Get("EmptyFunc", out m_f);
        return base.Test();
    }

    protected override void DoTest() {
        for (int j = 0; j < 200000; ++j) {
            m_f();
        }
    }
}


public class TestGetLuaValue : TestItem
{
    string m_valueName;
    object m_value;

    public TestGetLuaValue(PTest ptest, int index, string valueName)
        : base(ptest, index)
    {
        m_valueName = valueName;
    }

    protected override void DoTest() {
        for (int j = 0; j < 200000; ++j) {
            m_value = m_ptest.scriptEnv.GetInPath<object>(m_valueName);
        }
    }
}
