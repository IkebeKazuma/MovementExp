using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviourStateHandler : MonoBehaviour {

    public enum BehaviourState {
        None = -1,
        Normal,
        Attack,
        Avoid
    }
    public BehaviourState behavState { get; private set; } = BehaviourState.None;

    public BehaviourState prevBehavState { get; private set; } = BehaviourState.None;

    public void ChangeState(BehaviourState newState) {
        prevBehavState = behavState;
        behavState = newState;
    }

    public bool Equals(BehaviourState state) {
        return behavState == state;
    }

}