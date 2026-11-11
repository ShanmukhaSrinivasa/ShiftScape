using System.Collections;
using ShiftScape.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace ShiftScape.Game
{
    public sealed class GameController : MonoBehaviour
    {
        const string ProgressKey = "shiftscape.level";
        const float DragThreshold = 0.3f;
        const float HorizontalMargin = 0.9f;
        const float VerticalMargin = 1.5f;

        Camera cam;
        BoardView view;
        PuzzleSession session;
        int levelIndex;
        bool celebrating;
        Vector2Int lastScreenSize;

        bool dragging;
        Vector3 dragStartWorld;
        GridPos dragCell;

        GUIStyle titleStyle, infoStyle, buttonStyle;
        int styledHeight;

        // Lets the game run from any scene, including the template's SampleScene, with no setup.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindAnyObjectByType<GameController>() != null) return;
            new GameObject("ShiftScape").AddComponent<GameController>();
        }

        void Awake()
        {
            cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.56f, 0.76f, 0.86f);
            cam.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            var urpCamera = cam.GetComponent<UniversalAdditionalCameraData>();
            if (urpCamera != null) urpCamera.renderPostProcessing = false;

            view = new GameObject("Board").AddComponent<BoardView>();
            levelIndex = Mathf.Clamp(PlayerPrefs.GetInt(ProgressKey, 0), 0, LevelLibrary.Levels.Count - 1);
        }

        void Start() => LoadLevel(levelIndex);

        void LoadLevel(int index)
        {
            if (session != null)
            {
                session.MoveApplied -= view.AnimateShift;
                session.Solved -= OnSolved;
                session.Restarted -= OnRestarted;
            }
            StopAllCoroutines();
            celebrating = false;
            dragging = false;

            levelIndex = index;
            PlayerPrefs.SetInt(ProgressKey, levelIndex);
            PlayerPrefs.Save();

            session = new PuzzleSession(LevelLibrary.Levels[levelIndex]);
            session.MoveApplied += view.AnimateShift;
            session.Solved += OnSolved;
            session.Restarted += OnRestarted;
            view.Build(session.Board);
            FitCamera();
        }

        void OnRestarted()
        {
            StopAllCoroutines();
            celebrating = false;
            view.Build(session.Board);
        }

        void OnSolved() => StartCoroutine(Celebrate());

        IEnumerator Celebrate()
        {
            celebrating = true;
            while (view.IsBusy) yield return null;
            yield return view.WalkPath(PathFinder.FindPath(session.Board));
            celebrating = false;
        }

        void Update()
        {
            if (session == null) return;
            if (lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height) FitCamera();
            HandleKeyboard();
            HandlePointer();
        }

        void HandleKeyboard()
        {
            Keyboard k = Keyboard.current;
            if (k == null) return;
            if (k.rKey.wasPressedThisFrame) Restart();
            if (k.zKey.wasPressedThisFrame || k.uKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame) Undo();
            if (k.nKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame) Next();
        }

        void HandlePointer()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null) return;
            Vector3 world = ScreenToWorld(pointer.position.ReadValue());

            if (pointer.press.wasPressedThisFrame)
                dragging = !session.IsSolved && view.TryWorldToCell(world, out dragCell);
            if (!pointer.press.isPressed)
            {
                dragging = false;
                return;
            }
            if (pointer.press.wasPressedThisFrame) dragStartWorld = world;
            if (!dragging || view.IsBusy) return;

            Vector3 delta = world - dragStartWorld;
            if (delta.magnitude < DragThreshold) return;

            ShiftMove move = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? ShiftMove.Row(dragCell.Y, delta.x > 0f ? 1 : -1)
                : ShiftMove.Column(dragCell.X, delta.y < 0f ? 1 : -1);
            session.TryShift(move);
            dragging = false;
        }

        Vector3 ScreenToWorld(Vector2 screen)
        {
            Vector3 p = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            p.z = 0f;
            return p;
        }

        void Undo()
        {
            if (!view.IsBusy && !celebrating) session.Undo();
        }

        void Restart()
        {
            if (!celebrating) session.Restart();
        }

        void Next()
        {
            if (!session.IsSolved || celebrating) return;
            LoadLevel((levelIndex + 1) % LevelLibrary.Levels.Count);
        }

        void FitCamera()
        {
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            float halfHeight = view.Height * 0.5f + VerticalMargin;
            float halfWidth = view.Width * 0.5f + HorizontalMargin;
            cam.orthographicSize = Mathf.Max(halfHeight, halfWidth / cam.aspect);
        }

        // Temporary IMGUI HUD until the real UI pass.
        void OnGUI()
        {
            if (session == null) return;
            EnsureStyles();
            float s = Screen.height / 720f;
            float w = Screen.width;

            GUI.Label(new Rect(0, 14 * s, w, 44 * s), $"Level {levelIndex + 1}  ·  {session.Name}", titleStyle);
            string info = session.IsSolved
                ? $"Solved in {session.MoveCount} move{(session.MoveCount == 1 ? "" : "s")}{(session.MoveCount <= session.Par ? "  ·  Perfect!" : $"  ·  Par {session.Par}")}"
                : $"Moves {session.MoveCount}  ·  Par {session.Par}";
            GUI.Label(new Rect(0, 58 * s, w, 30 * s), info, infoStyle);

            float bw = 150 * s, bh = 52 * s, gap = 16 * s;
            float y = Screen.height - bh - 18 * s;
            float x = (w - (bw * 3 + gap * 2)) * 0.5f;

            GUI.enabled = session.CanUndo && !view.IsBusy && !celebrating;
            if (GUI.Button(new Rect(x, y, bw, bh), "Undo", buttonStyle)) Undo();
            GUI.enabled = !celebrating;
            if (GUI.Button(new Rect(x + bw + gap, y, bw, bh), "Restart", buttonStyle)) Restart();
            GUI.enabled = session.IsSolved && !celebrating;
            bool last = levelIndex == LevelLibrary.Levels.Count - 1;
            if (GUI.Button(new Rect(x + (bw + gap) * 2, y, bw, bh), last ? "Play Again" : "Next", buttonStyle)) Next();
            GUI.enabled = true;
        }

        void EnsureStyles()
        {
            if (titleStyle != null && styledHeight == Screen.height) return;
            styledHeight = Screen.height;
            float s = Screen.height / 720f;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(32 * s),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.14f, 0.2f, 0.26f) },
            };
            infoStyle = new GUIStyle(titleStyle) { fontSize = Mathf.RoundToInt(22 * s), fontStyle = FontStyle.Normal };
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(22 * s) };
        }
    }
}
