using UnityEngine;

public class DirectionalAnimator : MonoBehaviour
{
    public enum Direction { Down, Up, Left, Right }

    [Header("行走帧")]
    [SerializeField] private Sprite[] walkUp;
    [SerializeField] private Sprite[] walkDown;
    [SerializeField] private Sprite[] walkLeft;
    [SerializeField] private Sprite[] walkRight;

    [Header("待机帧")]
    [SerializeField] private Sprite[] idleUp;
    [SerializeField] private Sprite[] idleDown;
    [SerializeField] private Sprite[] idleLeft;
    [SerializeField] private Sprite[] idleRight;

    [Header("参数")]
    [SerializeField] private float fps = 6f;

    private SpriteRenderer sr;
    private Direction currentDirection = Direction.Down;
    private bool isWalking;
    private Sprite[] activeFrames;
    private int currentFrame;
    private float timer;
    private bool flipX;

    public Direction CurrentDirection => currentDirection;
    public bool IsWalking => isWalking;
    public float FPS { get => fps; set => fps = value; }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (sr == null || activeFrames == null || activeFrames.Length == 0) return;
        if (activeFrames.Length == 1)
        {
            ApplyFrame(0);
            return;
        }
        if (fps <= 0f) return;

        timer += Time.deltaTime;
        float interval = 1f / fps;
        if (timer >= interval)
        {
            timer -= interval;
            currentFrame++;
            if (currentFrame >= activeFrames.Length)
                currentFrame = 0;
            ApplyFrame(currentFrame);
        }
    }

    public void SetDirection(Direction dir)
    {
        if (dir == currentDirection) return;
        currentDirection = dir;
        RefreshActiveFrames();
    }

    public void SetWalking(bool walking)
    {
        if (walking == isWalking) return;
        isWalking = walking;
        RefreshActiveFrames();
    }

    public void SetFrames(Direction dir, bool walk, Sprite[] frames)
    {
        switch (dir)
        {
            case Direction.Up:    if (walk) walkUp = frames; else idleUp = frames; break;
            case Direction.Down:  if (walk) walkDown = frames; else idleDown = frames; break;
            case Direction.Left:  if (walk) walkLeft = frames; else idleLeft = frames; break;
            case Direction.Right: if (walk) walkRight = frames; else idleRight = frames; break;
        }
        RefreshActiveFrames();
    }

    public void SetFPS(float newFps)
    {
        fps = newFps;
    }

    public bool HasAnyFrames()
    {
        return HasFrames(walkUp) || HasFrames(walkDown) || HasFrames(walkLeft) || HasFrames(walkRight)
            || HasFrames(idleUp) || HasFrames(idleDown) || HasFrames(idleLeft) || HasFrames(idleRight);
    }

    private void RefreshActiveFrames()
    {
        flipX = false;
        Sprite[] frames = GetFrameSet(currentDirection, isWalking);

        if (!HasFrames(frames))
            frames = ApplyFallback();

        if (activeFrames != frames)
        {
            activeFrames = frames;
            currentFrame = 0;
            timer = 0f;
            if (activeFrames != null && activeFrames.Length > 0)
                ApplyFrame(0);
        }

        sr.flipX = flipX;
    }

    private Sprite[] ApplyFallback()
    {
        flipX = false;

        if (isWalking)
        {
            Sprite[] direct = GetWalkSet(currentDirection);
            if (HasFrames(direct)) return direct;

            if (currentDirection == Direction.Left && HasFrames(walkRight))
            {
                flipX = true;
                return walkRight;
            }
            if (currentDirection == Direction.Right && HasFrames(walkLeft))
            {
                flipX = true;
                return walkLeft;
            }

            if (HasFrames(walkDown)) return walkDown;
            if (HasFrames(walkUp)) return walkUp;
            if (HasFrames(walkLeft)) return walkLeft;
            if (HasFrames(walkRight)) return walkRight;

            return GetIdleFallback();
        }
        else
        {
            Sprite[] direct = GetIdleSet(currentDirection);
            if (HasFrames(direct)) return direct;

            if (currentDirection == Direction.Left && HasFrames(idleRight))
            {
                flipX = true;
                return idleRight;
            }
            if (currentDirection == Direction.Right && HasFrames(idleLeft))
            {
                flipX = true;
                return idleLeft;
            }

            if (HasFrames(idleDown)) return idleDown;
            if (HasFrames(idleUp)) return idleUp;
            if (HasFrames(idleLeft)) return idleLeft;
            if (HasFrames(idleRight)) return idleRight;

            Sprite[] walkFallback = GetWalkSet(currentDirection);
            if (HasFrames(walkFallback)) return new[] { walkFallback[0] };

            if (HasFrames(walkDown)) return new[] { walkDown[0] };
            if (HasFrames(walkUp)) return new[] { walkUp[0] };
            if (HasFrames(walkLeft)) return new[] { walkLeft[0] };
            if (HasFrames(walkRight)) return new[] { walkRight[0] };

            return null;
        }
    }

    private Sprite[] GetIdleFallback()
    {
        Sprite[] idle = GetIdleSet(currentDirection);
        if (HasFrames(idle)) return idle;
        if (HasFrames(idleDown)) return idleDown;
        if (HasFrames(idleUp)) return idleUp;
        if (HasFrames(idleLeft)) return idleLeft;
        if (HasFrames(idleRight)) return idleRight;
        return null;
    }

    private Sprite[] GetFrameSet(Direction dir, bool walk)
    {
        return walk ? GetWalkSet(dir) : GetIdleSet(dir);
    }

    private Sprite[] GetWalkSet(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return walkUp;
            case Direction.Down: return walkDown;
            case Direction.Left: return walkLeft;
            case Direction.Right: return walkRight;
            default: return walkDown;
        }
    }

    private Sprite[] GetIdleSet(Direction dir)
    {
        switch (dir)
        {
            case Direction.Up: return idleUp;
            case Direction.Down: return idleDown;
            case Direction.Left: return idleLeft;
            case Direction.Right: return idleRight;
            default: return idleDown;
        }
    }

    private void ApplyFrame(int index)
    {
        if (sr == null || activeFrames == null || index >= activeFrames.Length) return;
        if (activeFrames[index] != null)
            sr.sprite = activeFrames[index];
    }

    private static bool HasFrames(Sprite[] arr)
    {
        return arr != null && arr.Length > 0 && arr[0] != null;
    }
}
