using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSuccessiveAttackController : MonoBehaviour {

    [System.Serializable]
    public class AttackData {
        [Range(0, 1)] public float goNextTolerance;   // UŒ‚ŒãAŽŸ‚ÌUŒ‚‚ÉˆÚs‚·‚é‚Ü‚Å‚Ì‹–—e’l
        [SerializeField] string nextAttackName;
    }

    public AttackData[] attackData;

    public bool GetAllowGoNext(int phase, float normalizedTime) {
        int index = phase - 1;

        if (normalizedTime >= attackData[index].goNextTolerance) return true;

        return false;
    }
}