using System;
using Gameplay.Coop;
using Unity.Netcode;
using UnityEngine;
using UX.Options;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    private Vector2 input;
    private Vector2 lastMoveDir = Vector2.down;

    private AnimState lastSent;

    private struct AnimState : INetworkSerializable, IEquatable<AnimState>
    {
        public sbyte MoveX;
        public sbyte MoveY;
        public sbyte LastX;
        public sbyte LastY;
        public bool IsMoving;

        public AnimState(sbyte moveX, sbyte moveY, sbyte lastX, sbyte lastY, bool isMoving)
        {
            MoveX = moveX;
            MoveY = moveY;
            LastX = lastX;
            LastY = lastY;
            IsMoving = isMoving;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref MoveX);
            serializer.SerializeValue(ref MoveY);
            serializer.SerializeValue(ref LastX);
            serializer.SerializeValue(ref LastY);
            serializer.SerializeValue(ref IsMoving);
        }

        public bool Equals(AnimState other)
        {
            return MoveX == other.MoveX &&
                   MoveY == other.MoveY &&
                   LastX == other.LastX &&
                   LastY == other.LastY &&
                   IsMoving == other.IsMoving;
        }
    }

    private NetworkVariable<AnimState> animState = new NetworkVariable<AnimState>(
        new AnimState(0, -1, 0, -1, false),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            ApplyAnim(animState.Value);
        }

        animState.OnValueChanged += OnAnimStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        animState.OnValueChanged -= OnAnimStateChanged;

        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    private void OnAnimStateChanged(AnimState previous, AnimState next)
    {
        if (IsOwner) return;
        ApplyAnim(next);
    }

    private void Update()
    {
        if (IsOwner)
        {
            if (PauseManager.Instance != null && PauseManager.Instance.IsPaused)
            {
                input = Vector2.zero;
                ApplyLocalAnimFromInput();
                SendAnimStateIfChanged();
                return;
            }

            ReadFourDirInput();
            UpdateLastMoveDir();
            ApplyLocalAnimFromInput();
            SendAnimStateIfChanged();
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        if (rb == null) return;

        rb.linearVelocity = input * moveSpeed;
    }

    private void ReadFourDirInput()
    {
        float x = 0f;
        float y = 0f;

        bool left = InputSettings.Instance != null ? InputSettings.Instance.IsLeftPressed() : Input.GetKey(KeyCode.A);
        bool right = InputSettings.Instance != null ? InputSettings.Instance.IsRightPressed() : Input.GetKey(KeyCode.D);
        bool down = InputSettings.Instance != null ? InputSettings.Instance.IsBackPressed() : Input.GetKey(KeyCode.S);
        bool up = InputSettings.Instance != null ? InputSettings.Instance.IsForwardPressed() : Input.GetKey(KeyCode.W);

        if (left) x = -1f;
        else if (right) x = 1f;

        if (down) y = -1f;
        else if (up) y = 1f;

        if (x != 0f && y != 0f)
            y = 0f;

        input = new Vector2(x, y);
    }

    private void UpdateLastMoveDir()
    {
        if (input.sqrMagnitude > 0.001f)
            lastMoveDir = input;
    }

    private void ApplyLocalAnimFromInput()
    {
        if (animator == null) return;

        animator.SetFloat("MoveX", input.x);
        animator.SetFloat("MoveY", input.y);
        animator.SetFloat("LastX", lastMoveDir.x);
        animator.SetFloat("LastY", lastMoveDir.y);
        animator.SetBool("IsMoving", input.sqrMagnitude > 0.001f);
    }

    private void SendAnimStateIfChanged()
    {
        sbyte mx = (sbyte)Mathf.RoundToInt(Mathf.Clamp(input.x, -1f, 1f));
        sbyte my = (sbyte)Mathf.RoundToInt(Mathf.Clamp(input.y, -1f, 1f));
        sbyte lx = (sbyte)Mathf.RoundToInt(Mathf.Clamp(lastMoveDir.x, -1f, 1f));
        sbyte ly = (sbyte)Mathf.RoundToInt(Mathf.Clamp(lastMoveDir.y, -1f, 1f));
        bool moving = input.sqrMagnitude > 0.001f;

        AnimState next = new AnimState(mx, my, lx, ly, moving);

        if (next.Equals(lastSent))
            return;

        lastSent = next;
        animState.Value = next;
    }

    private void ApplyAnim(AnimState s)
    {
        if (animator == null) return;

        animator.SetFloat("MoveX", s.MoveX);
        animator.SetFloat("MoveY", s.MoveY);
        animator.SetFloat("LastX", s.LastX);
        animator.SetFloat("LastY", s.LastY);
        animator.SetBool("IsMoving", s.IsMoving);
    }
}
