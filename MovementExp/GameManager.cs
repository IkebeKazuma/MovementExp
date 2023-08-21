using UnityEngine;

public class GameManager : SingletonMonoBehaviour<GameManager> {

    void Start() {
        // FPS設定
        ApplyTargetFrameRate(60);
    }

    /// <summary>
    /// カーソル設定
    /// </summary>
    public void ApplyCursorState(CursorLockMode lockMode, bool visible) {
        Cursor.lockState = lockMode;
        Cursor.visible = visible;
    }

    /// <summary>
    /// FPS設定
    /// </summary>
    public void ApplyTargetFrameRate(int targetFrameRate, int vSyncCount = 0) {
        QualitySettings.vSyncCount = vSyncCount;
        Application.targetFrameRate = targetFrameRate;
    }

    void Update() {

    }

    /// <summary>
    /// ゲームプレイを終了する。
    /// </summary>
    public void Quit() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}