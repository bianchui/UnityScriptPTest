using UnityEngine;
using System.Collections;
using XLua;

public class VersionSwitch : MonoBehaviour {
    LuaEnv _env;
    private int tick = 0;

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        if (++tick % 50 == 0) {
            Debug.Log(">>>>>>>>Update in C#, tick = " + tick);
        }
    }

    private void CloseEnv() {
        if (_env != null) {
            _env.Dispose();
            _env = null;
        }
    }
    private void OnGUI() {
        if (GUI.Button(new Rect(10, 10, 300, 80), "Hotfix1")) {
            Debug.Log("Hotfix 1");
            CloseEnv();
            _env = new LuaEnv();

            _env.DoString(@"
                xlua.hotfix(CS.VersionSwitch, 'Update', function(self)
                    self.tick = self.tick + 1
                    if (self.tick % 50) == 0 then
                        print('<<<<<<<<Update in lua hotfix1, tick = ' .. self.tick)
                    end
                end)
            ");
        }
        if (GUI.Button(new Rect(10, 100, 300, 80), "Hotfix2")) {
            Debug.Log("Hotfix 2");
            CloseEnv();
            _env = new LuaEnv();

            _env.DoString(@"
                xlua.hotfix(CS.VersionSwitch, 'Update', function(self)
                    self.tick = self.tick + 1
                    if (self.tick % 50) == 0 then
                        print('<<<<<<<<Update in lua hotfix2, tick = ' .. self.tick)
                    end
                end)
            ");
        }
        if (GUI.Button(new Rect(10, 200, 300, 80), "Close")) {
            Debug.Log("Close");
            CloseEnv();
        }
    }
}
