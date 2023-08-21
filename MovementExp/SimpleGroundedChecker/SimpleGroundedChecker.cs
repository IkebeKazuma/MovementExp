using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;


namespace PMP.SimpleGroundedChecker {
    public class SimpleGroundedChecker : MonoBehaviour {

        // 接地判定かどうか
        public bool isGrounded { get; private set; } = false;
        public bool overrideGroundedState { set { isGrounded = value; } }

        // SphereCastが衝突しているか
        public bool isCollided { get; private set; }
        // 衝突位置
        public Vector3 hitPoint { get; private set; }
        // 衝突位置のノーマル
        public Vector3 groundNormal { get; private set; }

        // レイヤーマスク
        public LayerMask layerMask = ~0;
        public List<string> tagMaskList = new List<string>();

        public float rayOffset = 0.5f;
        public float rayRange = 1.05f;
        public Vector3 rayEndPos => rayOrigin + (rayDirection * rayRange);

        public Vector3 rayOrigin { get; private set; }
        public Vector3 endPos { get; private set; }

        public float radius = 0.5f;
        Vector3 rayDirection => -transform.up;
        public float maxSlopeAngle = 60;
        public float groundAngle = -1;

        // コールバック
        public UnityAction onLand = null;
        public UnityAction onLeave = null;

        public float distanceFromGroundDirectlyUnder { get; private set; }

        public bool useGizmo = true;



        private bool prevGroundedState = false;

        private void Update() {
            CheckGrounded();
        }

        private bool CheckGrounded() {
            // 始点計算
            rayOrigin = transform.position + (-rayDirection * rayOffset);

            isCollided = Physics.SphereCast(rayOrigin, radius, rayDirection, out RaycastHit hitInfo, rayRange, layerMask);

            bool result = isCollided;

            if (isCollided) {
                // 地面の法線
                groundNormal = hitInfo.normal.normalized;

                // 角度計算
                groundAngle = Vector3.Angle(Vector3.up, groundNormal);

                // 衝突位置
                hitPoint = hitInfo.point;

                // タグマスク
                bool invalidTagDetected = true;
                if (tagMaskList != null && tagMaskList.Count > 0) {
                    foreach (string mask in tagMaskList) {
                        if (hitInfo.collider.CompareTag(mask)) continue;
                        invalidTagDetected = false;
                    }
                } else invalidTagDetected = false;

                // タグマスクの検出に引っかかったらfalse
                if (invalidTagDetected) {
                    result = false;
                } else {
                    // スロープにいる場合は角度判定
                    if (OnSlope()) {
                        if (groundAngle > maxSlopeAngle) {
                            result = false;
                        }
                    }
                }
            } else {
                groundNormal = Vector3.up;
            }

            // 地面からの距離を計算
            distanceFromGroundDirectlyUnder = CalcDistanceFromGroundDirectlyUnder();

            // 終点計算
            endPos = result ? rayOrigin + (rayDirection * hitInfo.distance) : rayEndPos;

            // 接地ステート切り替わり
            if (result != prevGroundedState) {
                if (result == true) {
                    onLand?.Invoke();
                } else {
                    onLeave?.Invoke();
                }
                prevGroundedState = result;
            }

            return isGrounded = result;
        }

        /// <summary>
        /// 直下地面からの距離を返す。
        /// Rayが当たらなければ-1を返す。
        /// </summary>
        private float CalcDistanceFromGroundDirectlyUnder() {
            if (Physics.Raycast(rayOrigin, rayDirection, out var hitInfo, layerMask)) {
                return Vector3.Distance(transform.position, hitInfo.point);
            } else {
                return -1f;
            }
        }

        /// <summary>
        /// 斜面にいるか
        /// </summary>
        public bool OnSlope() => groundAngle > 0;

        public int sphereGizmoResolution = 12;
        private int prevSphereGizmoResolution = 12;
        Mesh sphereMeshData;

        private void OnDrawGizmosSelected() {
            if (!useGizmo) return;

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying) { CheckGrounded(); }
#endif

            if (CheckSphereMeshPropChanged())
                sphereMeshData = GetSphereMesh(radius, sphereGizmoResolution, sphereGizmoResolution);

            Color tRed = new Color(1.00f, 0.50f, 0.50f, 0.45f);
            Color tGreen = new Color(0.50f, 1.00f, 0.50f, 0.45f);

            float smallSphereRadius = transform.lossyScale.x * 0.02f;

            Color sphereColor = isGrounded ? tGreen : tRed;
            Gizmos.color = sphereColor;
            Gizmos.DrawLine(rayOrigin, endPos);
            Gizmos.DrawMesh(sphereMeshData, endPos);
            Gizmos.DrawWireSphere(endPos, radius);

            sphereColor.a = 1f;
            Gizmos.color = new Color(0.20f, 1.00f, 0.20f, 1.00f);
            Gizmos.DrawSphere(endPos, smallSphereRadius);

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(rayEndPos, radius);

            // 始点
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(rayOrigin, smallSphereRadius);

            // 終点
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(rayEndPos, smallSphereRadius);
        }

        bool CheckSphereMeshPropChanged() {
            if(sphereMeshData == null) { return true; }
            if (prevSphereGizmoResolution != sphereGizmoResolution) { prevSphereGizmoResolution = sphereGizmoResolution; return true; }
            return false;
        }

        /// <summary>
        /// 球メッシュ
        /// https://3dcg-school.pro/sphere-mesh-generator/
        /// </summary>
        public Mesh GetSphereMesh(float radius, int dividedInVertical = 6, int dividedInHorizontal = 6) {
            dividedInVertical = Mathf.Clamp(dividedInVertical, 6, 180);
            dividedInHorizontal = Mathf.Clamp(dividedInHorizontal, 6, 180);

            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();

            float x;
            float y;
            float z;

            #region 頂点を格納
            // 球のてっぺんは例外処理で追加
            vertices.Add(new Vector3(0, radius, 0));

            for (int p = 1; p < dividedInVertical; p++) {
                y = Mathf.Cos(Mathf.Deg2Rad * p * 180f / dividedInVertical) * radius;
                var t = Mathf.Sin(Mathf.Deg2Rad * p * 180f / dividedInVertical) * radius;

                for (int q = 0; q < dividedInHorizontal; q++) {
                    x = Mathf.Cos(Mathf.Deg2Rad * q * 360f / dividedInHorizontal) * t;
                    z = Mathf.Sin(Mathf.Deg2Rad * q * 360f / dividedInHorizontal) * t;
                    vertices.Add(new Vector3(x, y, z));
                }
            }
            // 球の底は例外処理で追加
            vertices.Add(new Vector3(0, -radius, 0));

            #endregion

            #region 頂点順序を格納
            // てっぺんを含む三角形のみ例外。円環上にポリゴンを敷き詰めていく
            for (int i = 0; i < dividedInHorizontal; i++) {
                ///円環の最後のポリゴンのみ最初にもどるので例外
                if (i == dividedInHorizontal - 1) {
                    triangles.Add(0);
                    triangles.Add(1);
                    triangles.Add(i + 1);
                    break;
                }

                triangles.Add(0);
                triangles.Add(i + 2);
                triangles.Add(i + 1);
            }

            for (int p = 0; p < dividedInVertical - 2; p++) {
                var firstIndexInLayer = p * dividedInHorizontal + 1;

                for (int q = 0; q < dividedInHorizontal; q++) {
                    // 円環の最後のみ例外
                    if (q == dividedInHorizontal - 1) {
                        triangles.Add(firstIndexInLayer + q);
                        triangles.Add(firstIndexInLayer);
                        triangles.Add(firstIndexInLayer + dividedInHorizontal);

                        triangles.Add(firstIndexInLayer + q);
                        triangles.Add(firstIndexInLayer + dividedInHorizontal);
                        triangles.Add(firstIndexInLayer + q + dividedInHorizontal);

                        break;
                    }

                    triangles.Add(firstIndexInLayer + q);
                    triangles.Add(firstIndexInLayer + q + 1);
                    triangles.Add(firstIndexInLayer + q + 1 + dividedInHorizontal);

                    triangles.Add(firstIndexInLayer + q);
                    triangles.Add(firstIndexInLayer + q + dividedInHorizontal + 1);
                    triangles.Add(firstIndexInLayer + q + dividedInHorizontal);
                }
            }

            // 底を含む三角形のみ例外処理
            for (int i = 0; i < dividedInHorizontal; i++) {
                // 円環の最後のポリゴンのみ最初にもどるので例外
                if (i == dividedInHorizontal - 1) {
                    triangles.Add(vertices.Count - 1);
                    triangles.Add(vertices.Count - 1 - dividedInHorizontal + i);
                    triangles.Add(vertices.Count - 1 - dividedInHorizontal);
                    break;
                }

                triangles.Add(vertices.Count - 1);
                triangles.Add(vertices.Count - 1 - dividedInHorizontal + i);
                triangles.Add(vertices.Count - dividedInHorizontal + i);
            }
            #endregion

            Mesh mesh = new Mesh();   // メッシュを作成
            mesh.Clear();   // メッシュ初期化
            mesh.SetVertices(vertices);   // メッシュに頂点を登録する
            mesh.SetTriangles(triangles, 0);   // メッシュにインデックスリストを登録する
            mesh.SetIndices(triangles, MeshTopology.Triangles, 0);   //MeshTopologyを変更すればラインや点群といった表示もできる
            mesh.RecalculateNormals();   // 法線の再計算

            return mesh;
        }
    }
}