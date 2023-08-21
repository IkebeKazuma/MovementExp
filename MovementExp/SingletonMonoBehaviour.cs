using UnityEngine;
using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;   // AssetDatabaseを使うために必要
#endif

public abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour {

    private static T _Instance;
    public static T Instance {
        get {
            if (_Instance == null) {
                Type t = typeof(T);
                _Instance = (T)FindObjectOfType(t);

                // 見つからなかった場合は非アクティブのものも検索する
                if (_Instance == null) {
                    _Instance = FindObjectInHierarchy(t);
                }

                if (_Instance == null) {
                    Debug.LogError(t + " をアタッチしているGameObjectはありません。");
                } else {
                    _Instance.gameObject.SetActive(true);
                }
            }

            return _Instance;
        }
    }

    static T FindObjectInHierarchy(Type target) {
        return Resources.FindObjectsOfTypeAll<T>()
#if UNITY_EDITOR
            .Where(go => AssetDatabase.GetAssetOrScenePath(go).Contains(".unity"))
#endif
            .First(go => go.GetType() == target);
    }

    /// <summary>
    /// 初期化
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    void Initialize() {
        // インスタンスチェック
        CheckInstance();
    }

    protected bool CheckInstance() {
        if (_Instance == null) {
            _Instance = this as T;
            return true;
        } else if (Instance == this) {
            return true;
        }
        Destroy(this);
        return false;
    }
}