using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReceiver : SingletonMonoBehaviour<PlayerInputReceiver> {

	[Header("Input Settings")]
	public float inputAcceleration = 20;
	public float inputDeceleration = 20;

	// 実際の入力値
	public Vector2 actualMove { get; private set; }
	Vector2 prevActualMove;

	[HideInInspector] public Vector2 move => GetLerpedInputValue();
	Vector2 learpedMove;

	/// <summary>
	/// 入力値があるか
	/// </summary>
	public bool HasMoveInput() => actualMove.magnitude > 0.001f;

	public bool jump { get; private set; }
	public bool attack { get; private set; }
    public bool avoid { get; private set; }

    public void OnMove(InputAction.CallbackContext context) {
		MoveInput(context.ReadValue<Vector2>());
	}

	public void OnJump(InputAction.CallbackContext context) {
		JumpInput(context.performed);
    }

    public void OnAttack(InputAction.CallbackContext context) {
        AttackInput(context.performed);
    }

    public void OnAvoid(InputAction.CallbackContext context) {
        AvoidInput(context.performed);
    }

    public void MoveInput(Vector2 newMoveDirection) {
		actualMove = ClampedRawInput(newMoveDirection);
	}

	public void JumpInput(bool newJumpState) {
		jump = newJumpState;
    }

    public void AttackInput(bool newAttackState) {
        attack = newAttackState;
    }

    public void AvoidInput(bool newAvoidState) {
        avoid = newAvoidState;
    }

    public void ResetMoveInput() {
		actualMove = learpedMove = Vector2.zero;
	}

	private Vector2 GetLerpedInputValue() {
		float correction;
		float diffTolerance = 0.02f;

		if (HasMoveInput()) {
			float magDiff = actualMove.magnitude - learpedMove.magnitude;
			if (Mathf.Abs(magDiff) >= diffTolerance) {
                if (magDiff > 0) {
                    correction = inputAcceleration;
                } else {
                    correction = inputDeceleration;
                }
            } else {
				correction = 1f;
			}
		} else {
			correction = inputDeceleration;
		}

		if(prevActualMove != actualMove) { prevActualMove = actualMove; }

		learpedMove = Vector2.Lerp(learpedMove, actualMove, correction * Time.deltaTime);
		return learpedMove;
	}

	Vector2 ClampedRawInput(Vector2 input) {
		input.x = Mathf.Clamp(input.x, -1, 1);
		input.y = Mathf.Clamp(input.y, -1, 1);
		return input;
	}
}