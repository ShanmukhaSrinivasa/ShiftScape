using System.Collections;
using System.Collections.Generic;
using ShiftScape.Core;
using UnityEngine;

namespace ShiftScape.Game
{
    public sealed class BoardView : MonoBehaviour
    {
        const float ShiftSeconds = 0.16f;
        const float WalkSecondsPerCell = 0.14f;
        const int FrameOrder = 0;
        const int TileOrder = 10;
        const int LockOrder = 15;
        const int MarkerOrder = 20;
        const int WalkerOrder = 30;

        static readonly Color SolvedTint = new Color(1f, 0.92f, 0.55f);

        sealed class TileView
        {
            public Transform Transform;
            public SpriteRenderer Renderer;
            public Tile Data;
        }

        Board board;
        TileView[] grid;
        Transform content;
        Transform walker;

        public bool IsBusy { get; private set; }
        public int Width => board.Width;
        public int Height => board.Height;
        public bool HasLockedRow { get; private set; }
        public bool HasLockedColumn { get; private set; }

        public void Build(Board source)
        {
            StopAllCoroutines();
            IsBusy = false;
            board = source;
            if (content != null) Destroy(content.gameObject);
            content = new GameObject("Content").transform;
            content.SetParent(transform, false);

            CreateQuad("Shadow", new Vector3(0.1f, -0.14f, 0f), new Vector2(Width + 0.3f, Height + 0.3f), new Color(0f, 0f, 0f, 0.25f), FrameOrder - 1);
            CreateQuad("Frame", Vector3.zero, new Vector2(Width + 0.3f, Height + 0.3f), new Color(0.29f, 0.22f, 0.16f), FrameOrder);

            var mask = new GameObject("Mask").AddComponent<SpriteMask>();
            mask.sprite = TileSpriteFactory.White;
            mask.transform.SetParent(content, false);
            mask.transform.localScale = new Vector3(Width, Height, 1f);

            grid = new TileView[Width * Height];
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                grid[y * Width + x] = CreateTile(board[x, y], CellLocal(x, y));

            HasLockedRow = HasLockedColumn = false;
            for (int y = 0; y < Height; y++)
            {
                if (!board.IsRowLocked(y)) continue;
                CreateLockedLine(true, y);
                HasLockedRow = true;
            }
            for (int x = 0; x < Width; x++)
            {
                if (!board.IsColumnLocked(x)) continue;
                CreateLockedLine(false, x);
                HasLockedColumn = true;
            }

            CreateMarker("Start", TileSpriteFactory.StartMarker, board.Start, MarkerOrder);
            CreateMarker("Goal", TileSpriteFactory.GoalMarker, board.Goal, MarkerOrder);
            walker = CreateMarker("Walker", TileSpriteFactory.Walker, board.Start, WalkerOrder);
            walker.localScale = Vector3.one * 0.9f;
        }

        public Vector3 CellLocal(int x, int y) =>
            new Vector3(x - (Width - 1) * 0.5f, (Height - 1) * 0.5f - y, 0f);

        public Vector3 CellWorld(GridPos p) => transform.TransformPoint(CellLocal(p.X, p.Y));

        public bool TryWorldToCell(Vector3 world, out GridPos cell)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            int x = Mathf.FloorToInt(local.x + Width * 0.5f);
            int y = Mathf.FloorToInt(Height * 0.5f - local.y);
            cell = new GridPos(x, y);
            return board != null && board.Contains(cell);
        }

        public void AnimateShift(ShiftMove move) => StartCoroutine(ShiftRoutine(move));

        public IEnumerator WalkPath(List<GridPos> path)
        {
            IsBusy = true;
            Collect(path[0]);
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 from = CellLocal(path[i - 1].X, path[i - 1].Y);
                Vector3 to = CellLocal(path[i].X, path[i].Y);
                for (float t = 0f; t < 1f; t += Time.deltaTime / WalkSecondsPerCell)
                {
                    walker.localPosition = Vector3.Lerp(from, to, t);
                    yield return null;
                }
                walker.localPosition = to;
                Collect(path[i]);
            }
            foreach (GridPos p in path) grid[p.Y * Width + p.X].Renderer.color = SolvedTint;
            IsBusy = false;
        }

        // Only the visuals change; the board keeps its crystals so restart and undo stay consistent.
        void Collect(GridPos cell)
        {
            TileView tile = grid[cell.Y * Width + cell.X];
            if (!tile.Data.HasCrystal) return;
            tile.Data = tile.Data.WithoutCrystal;
            tile.Renderer.sprite = TileSpriteFactory.Tile(tile.Data);
            StartCoroutine(Pop(tile.Transform));
        }

        static IEnumerator Pop(Transform target)
        {
            const float seconds = 0.2f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / seconds)
            {
                target.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
            target.localScale = Vector3.one;
        }

        IEnumerator ShiftRoutine(ShiftMove move)
        {
            IsBusy = true;
            int length = move.IsRow ? Width : Height;

            var slots = new List<int>(length);
            for (int i = 0; i < length; i++)
                if (!board[LineCell(move, i)].IsFixed) slots.Add(i);
            int count = slots.Count;

            var movers = new TileView[count];
            var from = new Vector3[count];
            var to = new Vector3[count];
            var final = new Vector3[count];
            var destination = new int[count];
            var ghosts = new List<TileView>();
            var ghostFrom = new List<Vector3>();
            var ghostTo = new List<Vector3>();

            for (int k = 0; k < count; k++)
            {
                movers[k] = grid[CellIndex(move, slots[k])];
                from[k] = movers[k].Transform.localPosition;
                int target = k + move.Step;
                int wrapped = (target % count + count) % count;
                destination[k] = wrapped;
                final[k] = LineLocal(move, slots[wrapped]);

                if (target == wrapped)
                {
                    to[k] = final[k];
                    continue;
                }
                // The wrapping tile slides out past one edge while a copy slides in from the other.
                to[k] = LineLocal(move, move.Step > 0 ? length : -1);
                TileView ghost = CreateTile(movers[k].Data, LineLocal(move, move.Step > 0 ? -1 : length));
                ghost.Renderer.color = movers[k].Renderer.color;
                ghosts.Add(ghost);
                ghostFrom.Add(ghost.Transform.localPosition);
                ghostTo.Add(final[k]);
            }

            for (float t = 0f; t < 1f; t += Time.deltaTime / ShiftSeconds)
            {
                float e = t * t * (3f - 2f * t);
                for (int k = 0; k < count; k++) movers[k].Transform.localPosition = Vector3.LerpUnclamped(from[k], to[k], e);
                for (int g = 0; g < ghosts.Count; g++) ghosts[g].Transform.localPosition = Vector3.LerpUnclamped(ghostFrom[g], ghostTo[g], e);
                yield return null;
            }

            foreach (TileView ghost in ghosts) Destroy(ghost.Transform.gameObject);
            for (int k = 0; k < count; k++)
            {
                movers[k].Transform.localPosition = final[k];
                grid[CellIndex(move, slots[destination[k]])] = movers[k];
            }
            IsBusy = false;
        }

        GridPos LineCell(ShiftMove move, int i) => move.IsRow ? new GridPos(i, move.Index) : new GridPos(move.Index, i);

        int CellIndex(ShiftMove move, int i)
        {
            GridPos p = LineCell(move, i);
            return p.Y * Width + p.X;
        }

        Vector3 LineLocal(ShiftMove move, int i) => move.IsRow ? CellLocal(i, move.Index) : CellLocal(move.Index, i);

        TileView CreateTile(Tile tile, Vector3 local)
        {
            var go = new GameObject("Tile");
            go.transform.SetParent(content, false);
            go.transform.localPosition = local;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = TileSpriteFactory.Tile(tile);
            sr.sortingOrder = TileOrder;
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            return new TileView { Transform = go.transform, Renderer = sr, Data = tile };
        }

        void CreateLockedLine(bool isRow, int index)
        {
            Vector3 center = isRow ? CellLocal(0, index) : CellLocal(index, 0);
            if (isRow) center.x = 0f;
            else center.y = 0f;
            Vector2 size = isRow ? new Vector2(Width, 1f) : new Vector2(1f, Height);
            CreateQuad("Locked", center, size, new Color(0.1f, 0.1f, 0.18f, 0.22f), LockOrder);

            float edgeX = Width * 0.5f + 0.55f;
            float edgeY = Height * 0.5f + 0.55f;
            Vector3 a = isRow ? new Vector3(-edgeX, center.y, 0f) : new Vector3(center.x, edgeY, 0f);
            Vector3 b = isRow ? new Vector3(edgeX, center.y, 0f) : new Vector3(center.x, -edgeY, 0f);
            foreach (Vector3 p in new[] { a, b })
            {
                SpriteRenderer icon = CreateSprite("Padlock", TileSpriteFactory.Padlock, p, LockOrder);
                icon.transform.localScale = Vector3.one * 0.6f;
            }
        }

        void CreateQuad(string name, Vector3 local, Vector2 size, Color color, int order)
        {
            SpriteRenderer sr = CreateSprite(name, TileSpriteFactory.White, local, order);
            sr.transform.localScale = new Vector3(size.x, size.y, 1f);
            sr.color = color;
        }

        Transform CreateMarker(string name, Sprite sprite, GridPos cell, int order) =>
            CreateSprite(name, sprite, CellLocal(cell.X, cell.Y), order).transform;

        SpriteRenderer CreateSprite(string name, Sprite sprite, Vector3 local, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(content, false);
            go.transform.localPosition = local;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }
    }
}
