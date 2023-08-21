using KanKikuchi.AudioManager;
using PMP.SimpleGroundedChecker;
using PMP.UnityLib;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    [Header("References")]
    [SerializeField] Rigidbody rb;
    [SerializeField] GameObject cam;
    [SerializeField] Animator anim;
    [SerializeField] SimpleGroundedChecker groundedChecker;
    [SerializeField] WallDetectionUtilities wallDetection;
    [SerializeField] PlayerBehaviourStateHandler bvStateHandler;
    [SerializeField] PlayerEffectController effCtrl;
    [SerializeField] PlayerSuccessiveAttackController sucAtkCtrl;

    Vector3 camForward;
    Vector3 moveForward;   // 移動方向
    Vector3 moveVector;   // 移動ベクトル
    float gravityScale;
    float verticalVelocity;

    [Header("Settings")]
    [SerializeField] float moveSpeed = 5;
    [SerializeField] float rotateSpeed = 20;

    [SerializeField] float normalGravityScale = 35.0f;
    [SerializeField] float jumpGravityScale = 5.0f;
    [SerializeField] float gravityChangeRate = 5;

    [Header("Jump Settings")]
    [SerializeField] float jumpTimeout = 0.1f;
    [SerializeField] float jumpPower = 1.2f;
    [SerializeField] float jumpMaxTime = 1f;
    float jumpTimeoutDelta;
    bool isJumping = false;
    float jumpElapsedTime = 0.0f;

    [SerializeField] float fallTimeout = 0.15f;
    float fallTimeoutDelta;

    [Header("Avoid Settings")]
    [SerializeField] float avoidTimeout = 0.5f;
    float avoidTimeoutDelta;
    Vector3 avoidDirection;
    [SerializeField] float avoidSpeed = 15f;
    [SerializeField] AnimationCurve avoidSpeedCurve;

    // Start is called before the first frame update
    void Start() {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (rb) {   // Rigidbodyの設定
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (!cam) cam = Camera.main.gameObject;

        bvStateHandler.ChangeState(PlayerBehaviourStateHandler.BehaviourState.Normal);

        groundedChecker.onLeave += OnLeave;
        groundedChecker.onLand += OnLand;
    }

    // Update is called once per frame
    void Update() {
        if (rb && cam) {
            CalcForward(PlayerInputReceiver.Instance.move);

            // 回転処理
            Rotate();

            if (bvStateHandler.Equals(PlayerBehaviourStateHandler.BehaviourState.Avoid)) {
                if (avoidTimeoutDelta >= 0.0f) {
                    avoidTimeoutDelta -= Time.deltaTime;
                } else {
                    //isAvoiding= false;
                    bvStateHandler.ChangeState(PlayerBehaviourStateHandler.BehaviourState.Normal);
                }
            }

            if (bvStateHandler.Equals(PlayerBehaviourStateHandler.BehaviourState.Attack) && !anim.GetCurrentAnimatorStateInfo(0).IsTag("Attack")) {
                bvStateHandler.ChangeState(PlayerBehaviourStateHandler.BehaviourState.Normal);
            }

            if (PlayerInputReceiver.Instance.attack) {
                PlayerInputReceiver.Instance.AttackInput(false);
                Attack();
            }

            if (PlayerInputReceiver.Instance.avoid) {
                PlayerInputReceiver.Instance.AvoidInput(false);
                Avoid();
            }

            JumpAndGravity();
        }

        if (anim) ApplyAnimationParam();
    }

    private void FixedUpdate() {
        // 進行方向
        Vector3 mfv = moveForward;
        // 速度
        float speed = moveSpeed * PlayerInputReceiver.Instance.move.magnitude;

        if (bvStateHandler.Equals(PlayerBehaviourStateHandler.BehaviourState.Avoid)) {
            mfv = avoidDirection;
            speed = avoidSpeed * avoidSpeedCurve.Evaluate(1 - (avoidTimeoutDelta / avoidTimeout));
        }

        // 壁衝突計算
        wallDetection.UpdateCastState(mfv);
        if (wallDetection.collided) {
            float dis = wallDetection.GetDistanceFromWall();
            if (Vector3.Angle(Vector3.up, wallDetection.GetHitInfo().normal) >= groundedChecker.maxSlopeAngle && wallDetection.CalcDistanceError(dis) < wallDetection.distanceErrorTolerance) {
                mfv = wallDetection.wallSlideVector;
            }
        }
        // 移動ベクトルの計算
        moveVector = mfv * speed;

        Move(moveVector + new Vector3(0.0f, verticalVelocity, 0.0f) * Time.fixedDeltaTime);
    }

    /// <summary>
    /// 正面計算
    /// </summary>
    /// <param name="input"></param>
    private void CalcForward(Vector2 input) {
        camForward = Vector3.Scale(cam.transform.forward, new Vector3(1, 0, 1)).normalized;

        Vector3 mfv = camForward * input.y + cam.transform.right * input.x;
        if (groundedChecker.isGrounded) {
            mfv = Vector3.ProjectOnPlane(mfv, groundedChecker.groundNormal);
        }

        moveForward = mfv;
    }

    /// <summary>
    /// 加速度を上書きする
    /// </summary>
    private void Move(Vector3 moveVector) {
        rb.velocity = moveVector;
    }

    /// <summary>
    /// 向きを変える
    /// </summary>
    private void Rotate() {
        if (bvStateHandler.Equals(PlayerBehaviourStateHandler.BehaviourState.Avoid)) return;

        if (moveForward != Vector3.zero && PlayerInputReceiver.Instance.HasMoveInput()) {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(moveForward), Time.deltaTime * rotateSpeed);
        }
    }

    /// <summary>
    /// ジャンプ入力監視
    /// </summary>
    bool CheckJumpInput() => PlayerInputReceiver.Instance.jump;

    void JumpAndGravity() {
        if (groundedChecker.isGrounded) {
            // reset the fall timeout timer
            fallTimeoutDelta = fallTimeout;

            anim.SetBool("Jump", false);

            // stop our velocity dropping infinitely when grounded
            if (verticalVelocity < 0.0f) {
                verticalVelocity = -2f;
            }

            // Jump
            if (CheckJumpInput()) {   // ジャンプ入力監視
                // ジャンプ処理
                if (!isJumping && jumpTimeoutDelta <= 0.0f) {
                    StartJump();
                }
            }

            // jump timeout
            if (jumpTimeoutDelta >= 0.0f) {
                jumpTimeoutDelta -= Time.deltaTime;
            }
        } else {
            // reset the jump timeout timer
            jumpTimeoutDelta = jumpTimeout;

            if (isJumping) {
                jumpElapsedTime += Time.deltaTime;

                // 最大時間終了
                if (jumpElapsedTime >= jumpMaxTime || CheckJumpInput() == false) {
                    EndJump();
                }
                gravityScale = jumpGravityScale;
            } else {
                gravityScale = normalGravityScale;

                // fall timeout
                if (fallTimeoutDelta >= 0.0f) {
                    fallTimeoutDelta -= Time.deltaTime;
                } else {
                    var stateInfo = anim.GetCurrentAnimatorStateInfo(0);
                    if (!stateInfo.IsName("Fall") && !stateInfo.IsTag("Jump-up") && !bvStateHandler.Equals(PlayerBehaviourStateHandler.BehaviourState.Avoid)) anim.CrossFadeInFixedTime("Fall", 0.2f);
                }

                // apply gravity
                verticalVelocity += rb.mass * Physics.gravity.y * gravityScale * Time.deltaTime;
            }
        }
    }

    void StartJump() {
        if (bvStateHandler.Equals(PlayerBehaviourStateHandler.BehaviourState.Avoid)) return;

        groundedChecker.overrideGroundedState = false;
        isJumping = true;
        if (anim) {
            anim.SetBool("Jump", true);
            anim.CrossFadeInFixedTime("WGS_Jump_StandToUp", 0.0f);
        }

        {
            var path = RandomUtils.Flag ? SEPath.UNIV0001 : SEPath.UNIV0002;
            SEManager.Instance.Play(path, pitch: 1 + (RandomUtils.Value * 0.05f));
        }

        PerformJump();
    }

    void PerformJump() {
        // the square root of H * -2 * G = how much velocity needed to reach desired height
        verticalVelocity = Mathf.Sqrt(jumpPower * -2f * (rb.mass * Physics.gravity.y * gravityScale));

        jumpElapsedTime = 0.0f;
    }

    void EndJump() {
        isJumping = false;
        anim.SetBool("Jump", false);
    }

    void ResetJumpStates() {
        isJumping = false;
        jumpElapsedTime = 0.0f;
        // reset the jump timeout timer
        jumpTimeoutDelta = jumpTimeout;
    }

    void Avoid() {
        if (bvStateHandler.Equals(PlayerBehaviourStateHandler.BehaviourState.Avoid)) return;
        if (!groundedChecker.isGrounded) return;
        if (isJumping) return;

        avoidTimeoutDelta = avoidTimeout;
        avoidDirection = moveForward;

        transform.rotation = Quaternion.LookRotation(avoidDirection);

        //isAvoiding = true;
        bvStateHandler.ChangeState(PlayerBehaviourStateHandler.BehaviourState.Avoid);

        SEManager.Instance.Play(SEPath.UNIV1101, pitch: 1 + (RandomUtils.Value * 0.05f));

        anim.CrossFadeInFixedTime("Avoid", 0.0f);
        effCtrl.PlayAvoidEff();
    }

    #region 攻撃

    void Attack() {
        if (bvStateHandler.Equals(PlayerBehaviourStateHandler.BehaviourState.Avoid)) return;

        bool isAttackState = bvStateHandler.Equals(PlayerBehaviourStateHandler.BehaviourState.Attack);

        if (!isAttackState) {
            bvStateHandler.ChangeState(PlayerBehaviourStateHandler.BehaviourState.Attack);
            anim.CrossFadeInFixedTime("A1", 0.0f);
        } else if (isAttackState) {
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            int atkPhaseIndex = GetAttackPhase(stateInfo);
            string phaseStr = $"A{atkPhaseIndex}";
            if (atkPhaseIndex == -1) return;

            bool allowGoNext = sucAtkCtrl.GetAllowGoNext(atkPhaseIndex, stateInfo.normalizedTime);
            if (!allowGoNext) return;

            switch (atkPhaseIndex) {
                case 1:
                    anim.CrossFadeInFixedTime("A2", 0.2f);
                    break;
                case 2:
                    anim.CrossFadeInFixedTime("A3", 0.2f);
                    break;
                case 3:
                    //anim.CrossFadeInFixedTime("A4", 0.0f);
                    break;
                case 4:
                    anim.CrossFadeInFixedTime("A5", 0.2f);
                    break;
                case 5:
                    break;
            }
        }
    }

    public int GetAttackPhase(AnimatorStateInfo stateInfo) {
        if (stateInfo.IsName("A1")) return 1;
        if (stateInfo.IsName("A2")) return 2;
        if (stateInfo.IsName("A3")) return 3;
        if (stateInfo.IsName("A4")) return 4;
        if (stateInfo.IsName("A5")) return 5;
        return -1;
    }

    #endregion

    void OnLeave() {

    }

    void OnLand() {
        if (bvStateHandler.Equals(PlayerBehaviourStateHandler.BehaviourState.Avoid)) return;

        if (!anim.GetCurrentAnimatorStateInfo(0).IsName("Land")) anim.Play("Land");
        effCtrl.PlayLandEff();

        ResetJumpStates();
    }

    void ApplyAnimationParam() {
        anim.SetFloat("Speed", Mathf.Clamp01(PlayerInputReceiver.Instance.move.magnitude));
        anim.SetBool("IsGrounded", groundedChecker.isGrounded);
        anim.SetFloat("VerticalVelocity", verticalVelocity);
    }
}