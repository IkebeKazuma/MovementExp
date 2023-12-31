using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSuccessiveAttackController : MonoBehaviour {

    [System.Serializable]
    public class AttackData {
        [Range(0, 1)] public float goNextTolerance;   // 攻撃後、次の攻撃に移行するまでの許容値
        [SerializeField] string nextAttackName;
    }

    public AttackData[] attackData;

    public bool GetAllowGoNext(int phase, float normalizedTime) {
        int index = phase - 1;

        if (normalizedTime >= attackData[index].goNextTolerance) return true;

        return false;
    }
}