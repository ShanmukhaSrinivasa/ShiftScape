using System;
using ShiftScape.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ShiftScape.Game
{
    public sealed class GameUi : MonoBehaviour
    {
        // The canvas is laid out on a 1080x1080 reference that expands to fit both landscape and portrait.
        public const float ReferenceSize = 1080f;
        public const float TopBarHeight = 190f;
        public const float BottomBarHeight = 250f;

        const int GridColumns = 6;
        static readonly Vector2 GridCell = new Vector2(140f, 165f);
        const float GridSpacing = 18f;

        public event Action PlayClicked, LevelsClicked, BackClicked;
        public event Action UndoClicked, RestartClicked, NextClicked, ReplayClicked;
        public event Action<int> LevelChosen;

        static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        static readonly Vector2 Top = new Vector2(0.5f, 1f);
        static readonly Vector2 Bottom = new Vector2(0.5f, 0f);

        Transform root;
        GameObject title, levelSelect, hud, win;
        Text titleStars, levelSelectStars, hudLevel, hudMoves, hudHint, winInfo, nextLabel;
        RectTransform levelGrid;
        Image[] winStars;
        Button undoButton;

        public static float PixelsPerUnit => Mathf.Min(Screen.width, Screen.height) / ReferenceSize;

        public static GameUi Create() => new GameObject("UI").AddComponent<GameUi>();

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceSize, ReferenceSize);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            gameObject.AddComponent<GraphicRaycaster>();
            root = transform;
            EnsureEventSystem();

            BuildTitle();
            BuildLevelSelect();
            BuildHud();
            BuildWin();
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        public void ShowTitle()
        {
            ShowOnly(title);
            titleStars.text = $"{ProgressStore.TotalStars()} / {LevelLibrary.Levels.Count * StarRating.MaxStars}";
        }

        public void ShowLevelSelect()
        {
            ShowOnly(levelSelect);
            levelSelectStars.text = $"{ProgressStore.TotalStars()} / {LevelLibrary.Levels.Count * StarRating.MaxStars}";
            RebuildLevelGrid();
        }

        public void ShowHud()
        {
            ShowOnly(hud);
            win.SetActive(false);
        }

        public void SetHud(int levelIndex, PuzzleSession session)
        {
            hudLevel.text = $"{levelIndex + 1}. {session.Name}";
            hudMoves.text = $"Moves {session.MoveCount}     Par {session.Par}";
            hudHint.text = session.Hint;
        }

        public void SetUndoEnabled(bool enabled)
        {
            if (undoButton.interactable != enabled) undoButton.interactable = enabled;
        }

        public void ShowWin(int stars, int moves, int par, bool newBest, bool isLastLevel)
        {
            win.SetActive(true);
            UiFactory.SetStars(winStars, stars);
            string movesText = $"{moves} move{(moves == 1 ? "" : "s")}  ·  par {par}";
            winInfo.text = newBest ? movesText + "  ·  new best!" : movesText;
            nextLabel.text = isLastLevel ? "All Levels" : "Next";
        }

        public void HideWin() => win.SetActive(false);

        void ShowOnly(GameObject screen)
        {
            title.SetActive(screen == title);
            levelSelect.SetActive(screen == levelSelect);
            hud.SetActive(screen == hud);
            if (screen != hud) win.SetActive(false);
        }

        void BuildTitle()
        {
            title = UiFactory.NewRect("Title", root).Stretch().gameObject;
            Transform t = title.transform;
            UiFactory.NewLabel("Name", t, "SHIFTSCAPE", 132, UiFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(Center, new Vector2(0f, 230f), new Vector2(1000f, 160f));
            UiFactory.NewLabel("Tagline", t, "Slide the ground. Find the way.", 42, UiFactory.Ink)
                .rectTransform.Place(Center, new Vector2(0f, 120f), new Vector2(1000f, 60f));
            UiFactory.NewButton("Play", t, "Play", UiFactory.Primary, Color.white, 56, () => PlayClicked?.Invoke())
                .GetComponent<RectTransform>().Place(Center, new Vector2(0f, -40f), new Vector2(440f, 140f));
            UiFactory.NewButton("Levels", t, "Levels", UiFactory.Secondary, UiFactory.Ink, 46, () => LevelsClicked?.Invoke())
                .GetComponent<RectTransform>().Place(Center, new Vector2(0f, -200f), new Vector2(440f, 120f));
            titleStars = StarTotal(t, new Vector2(0f, -340f));
        }

        void BuildLevelSelect()
        {
            levelSelect = UiFactory.NewRect("Level Select", root).Stretch().gameObject;
            Transform t = levelSelect.transform;
            UiFactory.NewLabel("Heading", t, "Levels", 80, UiFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(Top, new Vector2(0f, -85f), new Vector2(800f, 110f));
            levelSelectStars = StarTotal(t, new Vector2(0f, 0f));
            levelSelectStars.transform.parent.GetComponent<RectTransform>().Place(Top, new Vector2(0f, -165f), new Vector2(300f, 60f));

            levelGrid = UiFactory.NewRect("Grid", t);
            var layout = levelGrid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = GridCell;
            layout.spacing = new Vector2(GridSpacing, GridSpacing);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = GridColumns;
            layout.childAlignment = TextAnchor.UpperCenter;

            UiFactory.NewButton("Back", t, "Back", UiFactory.Secondary, UiFactory.Ink, 44, () => BackClicked?.Invoke())
                .GetComponent<RectTransform>().Place(Bottom, new Vector2(0f, 75f), new Vector2(300f, 100f));
        }

        void RebuildLevelGrid()
        {
            // Unparent first: Destroy is deferred and the grid layout would still count the old cells this frame.
            for (int i = levelGrid.childCount - 1; i >= 0; i--)
            {
                Transform old = levelGrid.GetChild(i);
                old.SetParent(null, false);
                Destroy(old.gameObject);
            }

            var levels = LevelLibrary.Levels;
            int rows = (levels.Count + GridColumns - 1) / GridColumns;
            levelGrid.Place(Center, new Vector2(0f, -25f), new Vector2(
                GridColumns * GridCell.x + (GridColumns - 1) * GridSpacing,
                rows * GridCell.y + (rows - 1) * GridSpacing));

            for (int i = 0; i < levels.Count; i++)
            {
                int index = i;
                bool unlocked = ProgressStore.IsUnlocked(i);
                Button cell = UiFactory.NewButton($"Level {i + 1}", levelGrid, null,
                    unlocked ? UiFactory.Secondary : new Color(1f, 1f, 1f, 0.4f), UiFactory.Ink, 0,
                    () => LevelChosen?.Invoke(index));
                cell.interactable = unlocked;
                Transform c = cell.transform;

                UiFactory.NewLabel("Number", c, (i + 1).ToString(), 60, unlocked ? UiFactory.Ink : new Color(0.14f, 0.2f, 0.26f, 0.45f),
                    TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform.Place(Center, new Vector2(0f, 22f), new Vector2(130f, 90f));

                RectTransform footer = UiFactory.NewRect("Footer", c).Place(Center, new Vector2(0f, -50f), new Vector2(130f, 44f));
                if (unlocked)
                    UiFactory.SetStars(UiFactory.NewStarRow(footer, 34f, 3f), ProgressStore.Stars(levels[i]));
                else
                    UiFactory.NewImage("Padlock", footer, TileSpriteFactory.Padlock, Color.white)
                        .rectTransform.Place(Center, Vector2.zero, new Vector2(48f, 48f));
            }
        }

        void BuildHud()
        {
            hud = UiFactory.NewRect("HUD", root).Stretch().gameObject;
            Transform t = hud.transform;
            hudLevel = UiFactory.NewLabel("Level", t, "", 56, UiFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            hudLevel.rectTransform.Place(Top, new Vector2(0f, -70f), new Vector2(1000f, 80f));
            hudMoves = UiFactory.NewLabel("Moves", t, "", 40, UiFactory.Ink);
            hudMoves.rectTransform.Place(Top, new Vector2(0f, -140f), new Vector2(1000f, 56f));
            hudHint = UiFactory.NewLabel("Hint", t, "", 34, new Color(0.14f, 0.2f, 0.26f, 0.85f), TextAnchor.MiddleCenter, FontStyle.Italic);
            hudHint.rectTransform.Place(Bottom, new Vector2(0f, 205f), new Vector2(1000f, 50f));

            var size = new Vector2(280f, 112f);
            UiFactory.NewButton("Levels", t, "Levels", UiFactory.Secondary, UiFactory.Ink, 42, () => LevelsClicked?.Invoke())
                .GetComponent<RectTransform>().Place(Bottom, new Vector2(-310f, 95f), size);
            undoButton = UiFactory.NewButton("Undo", t, "Undo", UiFactory.Secondary, UiFactory.Ink, 42, () => UndoClicked?.Invoke());
            undoButton.GetComponent<RectTransform>().Place(Bottom, new Vector2(0f, 95f), size);
            UiFactory.NewButton("Restart", t, "Restart", UiFactory.Secondary, UiFactory.Ink, 42, () => RestartClicked?.Invoke())
                .GetComponent<RectTransform>().Place(Bottom, new Vector2(310f, 95f), size);
        }

        void BuildWin()
        {
            win = UiFactory.NewImage("Win", hud.transform, null, UiFactory.Dim, true).rectTransform.Stretch().gameObject;
            Transform card = UiFactory.NewImage("Card", win.transform, UiSprites.RoundedRect, UiFactory.Cream)
                .rectTransform.Place(Center, Vector2.zero, new Vector2(720f, 640f));

            UiFactory.NewLabel("Heading", card, "Solved!", 84, UiFactory.Ink, TextAnchor.MiddleCenter, FontStyle.Bold)
                .rectTransform.Place(Center, new Vector2(0f, 225f), new Vector2(700f, 110f));
            RectTransform starRow = UiFactory.NewRect("Stars", card).Place(Center, new Vector2(0f, 95f), new Vector2(500f, 130f));
            winStars = UiFactory.NewStarRow(starRow, 124f, 18f);
            winInfo = UiFactory.NewLabel("Info", card, "", 40, UiFactory.Ink);
            winInfo.rectTransform.Place(Center, new Vector2(0f, -25f), new Vector2(700f, 60f));

            Button next = UiFactory.NewButton("Next", card, "Next", UiFactory.Primary, Color.white, 52, () => NextClicked?.Invoke());
            next.GetComponent<RectTransform>().Place(Center, new Vector2(0f, -140f), new Vector2(480f, 124f));
            nextLabel = next.GetComponentInChildren<Text>();
            UiFactory.NewButton("Replay", card, "Replay", UiFactory.Secondary, UiFactory.Ink, 40, () => ReplayClicked?.Invoke())
                .GetComponent<RectTransform>().Place(Center, new Vector2(-125f, -265f), new Vector2(230f, 100f));
            UiFactory.NewButton("Levels", card, "Levels", UiFactory.Secondary, UiFactory.Ink, 40, () => LevelsClicked?.Invoke())
                .GetComponent<RectTransform>().Place(Center, new Vector2(125f, -265f), new Vector2(230f, 100f));
        }

        static Text StarTotal(Transform parent, Vector2 position)
        {
            RectTransform group = UiFactory.NewRect("Stars Total", parent).Place(Center, position, new Vector2(300f, 60f));
            UiFactory.NewImage("Star", group, UiSprites.Star, UiFactory.StarOn).rectTransform.Place(Center, new Vector2(-70f, 0f), new Vector2(56f, 56f));
            Text label = UiFactory.NewLabel("Count", group, "", 44, UiFactory.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.rectTransform.Place(Center, new Vector2(50f, 0f), new Vector2(180f, 60f));
            return label;
        }
    }
}
