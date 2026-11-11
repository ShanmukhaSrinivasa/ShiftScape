using System.Collections;
using ShiftScape.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace ShiftScape.Game
{
    public sealed class GameController : MonoBehaviour
    {
        enum AppState { Title, LevelSelect, Playing }

        const float DragThreshold = 0.3f;
        // Past this distance the drag shows which line it will move.
        const float PreviewThreshold = 0.1f;
        // Padlock icons sit outside the board at the ends of locked lines and need the extra room.
        const float PaddingWithLocks = 0.95f;
        const float PaddingPlain = 0.3f;

        Camera cam;
        Background background;
        BoardView view;
        GameUi ui;
        GameAudio sound;
        PuzzleSession session;
        AppState state;
        int levelIndex;
        bool celebrating;
        Vector2Int lastScreenSize;

        bool dragging;
        Vector3 dragStartWorld;
        GridPos dragCell;

        // Lets the game run from any scene, including the template's SampleScene, with no setup.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindAnyObjectByType<GameController>() != null) return;
            new GameObject("ShiftScape").AddComponent<GameController>();
        }

        void Awake()
        {
            SetUpCamera();
            background = Background.Create(cam);
            sound = GameAudio.Create();

            view = new GameObject("Board").AddComponent<BoardView>();
            view.gameObject.SetActive(false);
            view.Stepped += sound.Step;
            view.CrystalCollected += sound.Crystal;

            UiFactory.AnyButtonClicked += sound.Click;
            ui = GameUi.Create();
            ui.SetMuted(sound.Muted);
            ui.MuteClicked += () =>
            {
                sound.ToggleMute();
                ui.SetMuted(sound.Muted);
            };
            ui.PlayClicked += () => LoadLevel(ProgressStore.FirstUnfinishedIndex());
            ui.LevelsClicked += ShowLevelSelect;
            ui.BackClicked += ShowTitle;
            ui.LevelChosen += index => { if (ProgressStore.IsUnlocked(index)) LoadLevel(index); };
            ui.UndoClicked += Undo;
            ui.RestartClicked += Restart;
            ui.ReplayClicked += Restart;
            ui.NextClicked += Next;
        }

        void Start() => ShowTitle();

        void OnDestroy()
        {
            if (sound != null) UiFactory.AnyButtonClicked -= sound.Click;
        }

        void SetUpCamera()
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
        }

        void ShowTitle()
        {
            LeaveLevel();
            state = AppState.Title;
            ui.ShowTitle();
        }

        void ShowLevelSelect()
        {
            LeaveLevel();
            state = AppState.LevelSelect;
            ui.ShowLevelSelect();
        }

        void LeaveLevel()
        {
            StopAllCoroutines();
            celebrating = false;
            dragging = false;
            view.HideHighlight();
            Unsubscribe();
            session = null;
            view.gameObject.SetActive(false);
        }

        void LoadLevel(int index)
        {
            LeaveLevel();
            levelIndex = index;
            session = new PuzzleSession(LevelLibrary.Levels[levelIndex]);
            session.MoveApplied += OnMoveApplied;
            session.Solved += OnSolved;
            session.Restarted += OnRestarted;

            state = AppState.Playing;
            view.gameObject.SetActive(true);
            view.Build(session.Board);
            ui.ShowHud();
            ui.SetHud(levelIndex, session);
            FitCamera();
        }

        void Unsubscribe()
        {
            if (session == null) return;
            session.MoveApplied -= OnMoveApplied;
            session.Solved -= OnSolved;
            session.Restarted -= OnRestarted;
        }

        void OnMoveApplied(ShiftMove move)
        {
            view.AnimateShift(move);
            sound.Slide();
            ui.SetHud(levelIndex, session);
        }

        void OnRestarted()
        {
            StopAllCoroutines();
            celebrating = false;
            view.Build(session.Board);
            ui.HideWin();
            ui.SetHud(levelIndex, session);
        }

        void OnSolved()
        {
            LevelData level = LevelLibrary.Levels[levelIndex];
            int previousBest = ProgressStore.BestMoves(level);
            ProgressStore.RecordWin(level, session.MoveCount);
            bool newBest = previousBest != 0 && session.MoveCount < previousBest;
            StartCoroutine(Celebrate(newBest));
        }

        IEnumerator Celebrate(bool newBest)
        {
            celebrating = true;
            while (view.IsBusy) yield return null;
            yield return view.WalkPath(PathFinder.FindPath(session.Board));
            sound.Win();
            view.Confetti(session.Board.Goal);
            // Long enough to enjoy the confetti before the win panel's dim overlay covers the board.
            yield return new WaitForSeconds(1f);
            celebrating = false;
            ui.ShowWin(StarRating.For(session.MoveCount, session.Par), session.MoveCount, session.Par, newBest,
                levelIndex == LevelLibrary.Levels.Count - 1);
        }

        void Update()
        {
            if (lastScreenSize.x != Screen.width || lastScreenSize.y != Screen.height) FitCamera();
            HandleKeyboard();
            if (state != AppState.Playing || session == null) return;
            ui.SetUndoEnabled(session.CanUndo && !view.IsBusy && !celebrating);
            HandlePointer();
        }

        void HandleKeyboard()
        {
            Keyboard k = Keyboard.current;
            if (k == null) return;
            if (k.escapeKey.wasPressedThisFrame)
            {
                if (state == AppState.Playing) ShowLevelSelect();
                else if (state == AppState.LevelSelect) ShowTitle();
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (k.f9Key.wasPressedThisFrame)
            {
                ProgressStore.UnlockAll = !ProgressStore.UnlockAll;
                if (state == AppState.LevelSelect) ui.ShowLevelSelect();
            }
#endif
            if (state != AppState.Playing) return;
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
            {
                dragging = !session.IsSolved && view.TryWorldToCell(world, out dragCell);
                dragStartWorld = world;
            }
            // A quick flick can press, move and release within a frame or two, so the release frame counts too.
            bool released = pointer.press.wasReleasedThisFrame;
            if (!dragging || (!pointer.press.isPressed && !released)) return;
            if (view.IsBusy)
            {
                if (released) EndDrag();
                return;
            }

            Vector3 delta = world - dragStartWorld;
            ShiftMove move = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? ShiftMove.Row(dragCell.Y, delta.x > 0f ? 1 : -1)
                : ShiftMove.Column(dragCell.X, delta.y < 0f ? 1 : -1);

            if (delta.magnitude >= PreviewThreshold) view.ShowHighlight(move.IsRow, move.Index);
            if (delta.magnitude < DragThreshold)
            {
                if (released) EndDrag();
                return;
            }

            if (!session.TryShift(move))
            {
                view.Reject(move);
                sound.Blocked();
            }
            EndDrag();
        }

        void EndDrag()
        {
            dragging = false;
            view.HideHighlight();
        }

        Vector3 ScreenToWorld(Vector2 screen)
        {
            Vector3 p = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            p.z = 0f;
            return p;
        }

        void Undo()
        {
            if (session != null && !view.IsBusy && !celebrating) session.Undo();
        }

        void Restart()
        {
            if (session != null && !celebrating) session.Restart();
        }

        void Next()
        {
            if (session == null || !session.IsSolved || celebrating) return;
            if (levelIndex + 1 < LevelLibrary.Levels.Count) LoadLevel(levelIndex + 1);
            else ShowLevelSelect();
        }

        // Fits the board into the band between the HUD's top and bottom bars.
        void FitCamera()
        {
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            if (session == null)
            {
                background.Fit();
                return;
            }

            float pixelsPerUnit = GameUi.PixelsPerUnit;
            float topPx = GameUi.TopBarHeight * pixelsPerUnit;
            float bottomPx = GameUi.BottomBarHeight * pixelsPerUnit;
            float availablePx = Mathf.Max(1f, Screen.height - topPx - bottomPx);

            float boardHeight = view.Height + (view.HasLockedColumn ? PaddingWithLocks : PaddingPlain) * 2f;
            float boardWidth = view.Width + (view.HasLockedRow ? PaddingWithLocks : PaddingPlain) * 2f;
            float sizeByHeight = boardHeight * Screen.height / (2f * availablePx);
            float sizeByWidth = boardWidth / (2f * cam.aspect * 0.94f);
            cam.orthographicSize = Mathf.Max(sizeByHeight, sizeByWidth);

            float worldPerPixel = 2f * cam.orthographicSize / Screen.height;
            cam.transform.position = new Vector3(0f, (topPx - bottomPx) * 0.5f * worldPerPixel, -10f);
            background.Fit();
        }
    }
}
