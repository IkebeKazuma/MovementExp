using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace PMP.SimpleGroundedChecker {
    [CustomEditor(typeof(SimpleGroundedChecker))]
    public class SimpleGroundedCheckerEditor : Editor {
        [MenuItem("Tools/PM Presents/Add [SimpleGroundedChecker] to the active object")]
        static void AddSGC () {
            var _go = Selection.activeGameObject;
            if (!_go.GetComponent<SimpleGroundedChecker>()) _go.AddComponent<SimpleGroundedChecker>();
        }

        GUIStyle desc;

        public override void OnInspectorGUI () {
            // base.OnInspectorGUI();

            SimpleGroundedChecker data = target as SimpleGroundedChecker;

            desc = new GUIStyle();
            desc.normal.textColor = Color.gray;

            LayoutUtility.TitleAndCredit("Simple接地判定", "© 2023 ピノまっちゃ");

            EditorGUILayout.Space(5);

            if (EditorApplication.isPlaying) {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    LayoutUtility.HeaderField("Rayの状態", Color.gray, Color.white);

                    EditorGUILayout.Space(5);

                    EditorGUILayout.LabelField($"接地しているか？ : {(data.isGrounded ? "している" : "していない")}");
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(10);
            }

            // 更新チェック
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                LayoutUtility.HeaderField("Rayプロパティ設定", Color.gray, Color.white);

                EditorGUILayout.Space(5);

                if (data.layerMask == 0) {
                    EditorGUILayout.HelpBox("LayerMaskがNothingに設定されています。", MessageType.Warning);
                }

                SerializedProperty layerMaskProp = serializedObject.FindProperty("layerMask");
                EditorGUILayout.PropertyField(layerMaskProp, new GUIContent("レイヤーマスク"));

                EditorGUILayout.Space();

                EditorGUI.indentLevel++;
                SerializedProperty tagMaskProp = serializedObject.FindProperty("tagMaskList");
                EditorGUILayout.PropertyField(tagMaskProp, new GUIContent("タグマスク"));
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("↑タグマスクに設定したタグの付くオブジェクトは地面とみなしません。", desc);
                EditorGUI.indentLevel--;
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();

                data.rayOffset = EditorGUILayout.FloatField("RayのY軸オフセット", data.rayOffset);
                data.rayRange = EditorGUILayout.FloatField("Rayを飛ばす距離", data.rayRange);

                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical(GUI.skin.box);
                {
                    var _text = new GUIStyle();

                    EditorGUILayout.BeginHorizontal();
                    {
                        _text.normal.textColor = Color.red;
                        EditorGUILayout.LabelField("赤色の球: Rayの始点", _text);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    {
                        _text.normal.textColor = Color.blue;
                        EditorGUILayout.LabelField("青色の球: Rayの終点", _text);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;

                EditorGUILayout.Space();

                data.radius = EditorGUILayout.FloatField("半径", data.radius);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                LayoutUtility.HeaderField("接地判定プロパティ設定", Color.gray, Color.white);

                EditorGUILayout.Space(5);

                data.maxSlopeAngle = EditorGUILayout.FloatField("許容角度", data.maxSlopeAngle);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"{data.maxSlopeAngle}°までを地面とみなします。", desc);
                EditorGUI.indentLevel--;

                if (EditorApplication.isPlaying) {
                    EditorGUILayout.Space(5);

                    EditorGUILayout.LabelField($"地面の角度: {data.groundAngle}");
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(GUI.skin.box);
            {
                LayoutUtility.HeaderField("ギズモ設定", Color.gray, Color.white);

                EditorGUILayout.Space(5);

                if (GUILayout.Button($"ギズモを表示{(data.useGizmo ? "しない" : "する")}", GUILayout.Height(30))) {
                    data.useGizmo = !data.useGizmo;
                }
                if (data.useGizmo) data.sphereGizmoResolution = EditorGUILayout.IntSlider("ギズモ球の解像度", data.sphereGizmoResolution, 6, 180);
            }
            EditorGUILayout.EndVertical();

            if (EditorGUI.EndChangeCheck()) {
                var scene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorApplication.QueuePlayerLoopUpdate();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI () {

            SimpleGroundedChecker data = target as SimpleGroundedChecker;

            if (!data.useGizmo) return;

            // ノーマル描画
            if (data.isGrounded) {
                Color color = new Color(1.00f, 0.20f, 0.20f, 0.50f);
                Handles.color = color;

                Vector3 origin = data.hitPoint;
                Handles.DrawLine(origin, origin + (data.groundNormal * 0.2f));
                Handles.DrawWireDisc(origin, data.groundNormal, data.radius * 0.5f);
                Handles.DrawSolidDisc(origin, data.groundNormal, data.radius * 0.4f);

                var normalHandleLabelStyle = new GUIStyle();
                color.a = 1f;
                normalHandleLabelStyle.normal.textColor = color;
                Handles.Label(origin, "ノーマル", normalHandleLabelStyle);
            }

            // ラベル描画
            float xOffset = data.transform.lossyScale.x * 0.02f + 0.005f;
            var handleLabelStyle = new GUIStyle();
            handleLabelStyle.normal.textColor = Color.red;
            Handles.Label(data.rayOrigin + new Vector3(-xOffset, 0, 0), "Ray 始点", handleLabelStyle);
            handleLabelStyle.normal.textColor = Color.blue;
            Handles.Label(data.rayEndPos + new Vector3(-xOffset, 0, 0), "Ray 終点", handleLabelStyle);
            if (data.isGrounded) {
                handleLabelStyle.normal.textColor = Color.green;
                Handles.Label(data.endPos + new Vector3(-xOffset, 0, 0), "SphereCast 中心点", handleLabelStyle);
            }
        }
    }
}