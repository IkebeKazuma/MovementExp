using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class WallDetectionUtilities : MonoBehaviour {

    [Header("References")]
    [SerializeField] CapsuleCollider capsuleCollider;

    [Header("Settings")]
    public float range = 1.5f;
    public float capsuleRadiusMargin = 0.01f;
    public float capsuleHeightMargin = 0.01f;
    public float distanceErrorTolerance = 0.05f;
    public float minWallLimit = 60;

    [Header("Cast Settings")]
    [SerializeField] LayerMask layerMask = ~0;
    [SerializeField] QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore;

    public Vector3 wallSlideVector { get; private set; } = Vector3.zero;
    Vector3 castDirection;
    RaycastHit hitInfo;
    public RaycastHit GetHitInfo() => hitInfo;

    float capsuleRadius => capsuleCollider.radius - capsuleRadiusMargin;
    float capsuleHeight => capsuleCollider.height - capsuleHeightMargin;
    Vector3 capsuleCenter => transform.position + capsuleCollider.center;

    Vector3 p1 => capsuleCenter + (transform.up * (capsuleHeight / 2 - capsuleRadius));
    Vector3 p2 => capsuleCenter - transform.up * (capsuleHeight / 2 - capsuleRadius);

    public bool collided { get; private set; }

    public void UpdateCastState(Vector3 f) {
        castDirection = f.normalized;
        Physics.CapsuleCast(
            point1: p1,
            point2: p2,
            radius: capsuleRadius,
            direction: castDirection,
            maxDistance: range,
            hitInfo: out hitInfo,
            layerMask: layerMask,
            queryTriggerInteraction: queryTriggerInteraction
        );
        collided = hitInfo.collider != null;

        Vector3 tmpWsv = Vector3.zero;
        if (collided) {
            float wallAngle = Vector3.Angle(Vector3.up, hitInfo.normal);
            if (wallAngle > minWallLimit) {
                // ぶつかった壁の角度が限界地を超えていたらYをつぶす
                //tmpWsv = Vector3.Scale(tmpWsv, new Vector3(1, 0, 1));
                // ぶつかった壁の角度が限界地を超えていたら90度の壁とみなす
                tmpWsv = Vector3.ProjectOnPlane(f, Vector3.Scale(hitInfo.normal, new Vector3(1, 0, 1)));
            } else {
                tmpWsv = Vector3.ProjectOnPlane(f, hitInfo.normal);
            }
        }

        wallSlideVector = collided ? tmpWsv : Vector3.zero;
    }

    /// <summary>
    /// 壁を平面とした時の平面との距離を返します。
    /// 衝突していない場合は-1を返します。
    /// </summary>
    public float GetDistanceFromWall() {
        if (collided) {
            // 平面を定義
            var plane = new Plane(hitInfo.normal, hitInfo.point);
            // 平面と点との距離を求める
            var distance = plane.GetDistanceToPoint(capsuleCenter);
            distance = Mathf.Max(0, distance);
            return distance;
        } else
            return -1;
    }

    /// <summary>
    /// 距離誤差を計算
    /// </summary>
    public float CalcDistanceError(float distance) {
        if (collided) {
            return Mathf.Abs(distance - capsuleCollider.radius);
        } else
            return -1;
    }

    private void OnDrawGizmos() {
#if UNITY_EDITOR

        var defaultCol = Gizmos.color;

        //Gizmos.color = Color.white;
        //Gizmos.DrawMesh(CapsuleMeshDrawer.GetMesh(capsuleRadius, capsuleHeight), capsuleCenter);

        float alpha = 0.25f;

        {
            Gizmos.color = new Color(1, 1, 1, alpha);

            Vector3 from = capsuleCenter;
            Vector3 to = capsuleCenter + (castDirection * range);
            Gizmos.DrawLine(from, to);
            Vector3 targetPos = from + (castDirection * range);
            Gizmos.DrawMesh(CapsuleMeshDrawer.GetMesh(capsuleRadius, capsuleHeight), targetPos);
            Utils.GizmosExtensions.DrawWireCapsule(targetPos, capsuleRadius, capsuleHeight);
        }


        if (collided) {
            Gizmos.color = new Color(1f, 0.92f, 0.016f, alpha);
            // 衝突地点のカプセル
            var hitPointCapCenter = capsuleCenter + (castDirection * hitInfo.distance);
            Gizmos.DrawMesh(CapsuleMeshDrawer.GetMesh(capsuleRadius, capsuleHeight), hitPointCapCenter);
            Utils.GizmosExtensions.DrawWireCapsule(hitPointCapCenter, capsuleRadius, capsuleHeight);

            Gizmos.color = new Color(0, 0, 1, alpha);
            // 壁ずりベクトル地点のカプセル
            var slidedVecCenter = hitPointCapCenter + wallSlideVector;
            Gizmos.DrawMesh(CapsuleMeshDrawer.GetMesh(capsuleRadius, capsuleHeight), slidedVecCenter);
            Utils.GizmosExtensions.DrawWireCapsule(slidedVecCenter, capsuleRadius, capsuleHeight);
        }

        if (collided) {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + wallSlideVector);
            Gizmos.DrawSphere(hitInfo.point, 0.1f);
        }

        Gizmos.color = defaultCol;
#endif
    }

    public static class CapsuleMeshDrawer {

        private static  Vector2Int _divide = new Vector2Int(10, 10);

        public static Mesh GetMesh(float radius, float height) {
            int divideH = _divide.x;
            int divideV = _divide.y;

            height -= radius*2f;

            var data = CreateCapsule(divideH, divideV, height, radius);
            var mesh = new Mesh();
            mesh.SetVertices(data.vertices);
            mesh.SetIndices(data.indices, MeshTopology.Triangles, 0);
            mesh.RecalculateNormals();

            return mesh;
        }

        struct MeshData {
            public Vector3[] vertices;
            public int[] indices;
        }

        /// <summary>  
        /// カプセルメッシュデータを作成  
        /// </summary>  
        static MeshData CreateCapsule(int divideH, int divideV, float height, float radius) {
            divideH = divideH < 4 ? 4 : divideH;
            divideV = divideV < 4 ? 4 : divideV;
            radius = radius <= 0 ? 0.001f : radius;

            // 偶数のみ有効  
            if (divideV % 2 != 0) {
                divideV++;
            }

            int cnt = 0;

            // =============================  
            // 頂点座標作成  
            // =============================  

            int vertCount = divideH * divideV + 2;
            var vertices = new Vector3[vertCount];

            // 中心角  
            float centerEulerRadianH = 2f * Mathf.PI / (float)divideH;
            float centerEulerRadianV = 2f * Mathf.PI / (float)divideV;

            float offsetHeight = height * 0.5f;

            // 天面  
            vertices[cnt++] = new Vector3(0, radius + offsetHeight, 0);

            // カプセル上部  
            for (int vv = 0; vv < divideV / 2; vv++) {
                var vRadian = (float)(vv + 1) * centerEulerRadianV / 2f;

                // 1辺の長さ  
                var tmpLen = Mathf.Abs(Mathf.Sin(vRadian) * radius);

                var y = Mathf.Cos(vRadian) * radius;
                for (int vh = 0; vh < divideH; vh++) {
                    var pos = new Vector3(
                        tmpLen * Mathf.Sin((float)vh * centerEulerRadianH),
                        y + offsetHeight,
                        tmpLen * Mathf.Cos((float)vh * centerEulerRadianH)
                    );
                    // サイズ反映  
                    vertices[cnt++] = pos;
                }
            }

            // カプセル下部  
            int offset = divideV / 2;
            for (int vv = 0; vv < divideV / 2; vv++) {
                var yRadian = (float)(vv + offset) * centerEulerRadianV / 2f;

                // 1辺の長さ  
                var tmpLen = Mathf.Abs(Mathf.Sin(yRadian) * radius);

                var y = Mathf.Cos(yRadian) * radius;
                for (int vh = 0; vh < divideH; vh++) {
                    var pos = new Vector3(
                        tmpLen * Mathf.Sin((float)vh * centerEulerRadianH),
                        y - offsetHeight,
                        tmpLen * Mathf.Cos((float)vh * centerEulerRadianH)
                    );
                    // サイズ反映  
                    vertices[cnt++] = pos;
                }
            }

            // 底面  
            vertices[cnt] = new Vector3(0, -radius - offsetHeight, 0);

            // =============================  
            // インデックス配列作成  
            // =============================  

            int topAndBottomTriCount = divideH * 2;
            // 側面三角形の数  
            int aspectTriCount = divideH * (divideV - 2 + 1) * 2;

            int[] indices = new int[(topAndBottomTriCount + aspectTriCount) * 3];

            //天面  
            int offsetIndex = 0;
            cnt = 0;
            for (int i = 0; i < divideH * 3; i++) {
                if (i % 3 == 0) {
                    indices[cnt++] = 0;
                } else if (i % 3 == 1) {
                    indices[cnt++] = 1 + offsetIndex;
                } else if (i % 3 == 2) {
                    var index = 2 + offsetIndex++;
                    // 蓋をする  
                    index = index > divideH ? indices[1] : index;
                    indices[cnt++] = index;
                }
            }

            // 側面Index  

            /* 頂点を繋ぐイメージ  
             * 1 - 2  
             * |   |  
             * 0 - 3  
             *  
             * 0, 1, 2  
             * 0, 2, 3  
             *  
             * 注意 : 1周した時にClampするのを忘れないように。  
             */

            // 開始Index番号  
            int startIndex = indices[1];

            // 天面、底面を除いたカプセルIndex要素数  
            int sideIndexLen = aspectTriCount * 3;

            int lap1stIndex = 0;

            int lap2ndIndex = 0;

            // 一周したときのindex数  
            int lapDiv = divideH * 2 * 3;

            int createSquareFaceCount = 0;

            for (int i = 0; i < sideIndexLen; i++) {
                // 一周の頂点数を超えたら更新(初回も含む)  
                if (i % lapDiv == 0) {
                    lap1stIndex = startIndex;
                    lap2ndIndex = startIndex + divideH;
                    createSquareFaceCount++;
                }

                if (i % 6 == 0 || i % 6 == 3) {
                    indices[cnt++] = startIndex;
                } else if (i % 6 == 1) {
                    indices[cnt++] = startIndex + divideH;
                } else if (i % 6 == 2 || i % 6 == 4) {
                    if (i > 0 &&
                        (i % (lapDiv * createSquareFaceCount - 2) == 0 ||
                         i % (lapDiv * createSquareFaceCount - 4) == 0)
                    ) {
                        // 1周したときのClamp処理  
                        // 周回ポリゴンの最後から2番目のIndex  
                        indices[cnt++] = lap2ndIndex;
                    } else {
                        indices[cnt++] = startIndex + divideH + 1;
                    }
                } else if (i % 6 == 5) {
                    if (i > 0 && i % (lapDiv * createSquareFaceCount - 1) == 0) {
                        // 1周したときのClamp処理  
                        // 周回ポリゴンの最後のIndex  
                        indices[cnt++] = lap1stIndex;
                    } else {
                        indices[cnt++] = startIndex + 1;
                    }

                    // 開始Indexの更新  
                    startIndex++;
                } else {
                    Debug.LogError("Invalid : " + i);
                }
            }


            // 底面Index  
            offsetIndex = vertices.Length - 1 - divideH;
            lap1stIndex = offsetIndex;
            var finalIndex = vertices.Length - 1;
            int len = divideH * 3;

            for (int i = len - 1; i >= 0; i--) {
                if (i % 3 == 0) {
                    // 底面の先頂点  
                    indices[cnt++] = finalIndex;
                    offsetIndex++;
                } else if (i % 3 == 1) {
                    indices[cnt++] = offsetIndex;
                } else if (i % 3 == 2) {
                    var value = 1 + offsetIndex;
                    if (value >= vertices.Length - 1) {
                        value = lap1stIndex;
                    }

                    indices[cnt++] = value;
                }
            }


            return new MeshData() {
                indices = indices,
                vertices = vertices
            };
        }
    }
}