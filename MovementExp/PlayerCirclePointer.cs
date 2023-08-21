using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCirclePointer : MonoBehaviour {

    [SerializeField] Transform target;
    [SerializeField] float yOffset = 0.01f;

    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        if (target) {
            if (Physics.Raycast(target.position + (target.up * 0.2f), Vector3.down, out var hit)) {
                transform.position = hit.point + (hit.normal * yOffset);
                transform.rotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            }
        }
    }
}