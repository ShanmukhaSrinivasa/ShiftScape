using ShiftScape.Core;
using UnityEngine;

namespace ShiftScape.Game
{
    // Placeholder art drawn in code so the game is playable before real sprites exist.
    public static class TileSpriteFactory
    {
        public const int Size = 64;

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 Border = new Color32(74, 98, 63, 255);
        static readonly Color32 Ground = new Color32(118, 154, 92, 255);
        static readonly Color32 GroundSpeck = new Color32(134, 170, 106, 255);
        static readonly Color32 PathEdge = new Color32(120, 88, 56, 255);
        static readonly Color32 PathFill = new Color32(226, 200, 146, 255);
        static readonly Color32 StoneBorder = new Color32(78, 82, 92, 255);
        static readonly Color32 Stone = new Color32(134, 138, 146, 255);
        static readonly Color32 StoneSpeck = new Color32(150, 154, 162, 255);
        static readonly Color32 Bolt = new Color32(206, 208, 216, 255);
        static readonly Color32 BoltEdge = new Color32(60, 62, 70, 255);
        static readonly Color32 CrystalFill = new Color32(88, 214, 236, 255);
        static readonly Color32 CrystalLight = new Color32(196, 248, 255, 255);
        static readonly Color32 CrystalEdge = new Color32(22, 92, 124, 255);

        // 16 exit combinations x fixed x crystal.
        const int VariantCount = 64;
        const int AtlasColumns = 8;

        static readonly Sprite[] tileSprites = new Sprite[VariantCount];
        static Texture2D tileAtlas;
        static Sprite white, start, goal, walker, padlock;

        public static Sprite Tile(Tile tile)
        {
            int key = (int)tile.Exits | (tile.IsFixed ? 16 : 0) | (tile.HasCrystal ? 32 : 0);
            if (tileSprites[key] != null) return tileSprites[key];
            if (tileAtlas == null) tileAtlas = BuildTileAtlas();

            var rect = new Rect(key % AtlasColumns * Size, key / AtlasColumns * Size, Size, Size);
            Sprite sprite = Sprite.Create(tileAtlas, rect, new Vector2(0.5f, 0.5f), Size, 0, SpriteMeshType.FullRect);
            sprite.name = $"Tile {tile.Exits.ToLetters()}{(tile.HasCrystal ? "+" : "")}{(tile.IsFixed ? "*" : "")}";
            tileSprites[key] = sprite;
            return sprite;
        }

        // Every tile variant lives in one texture. With a texture per variant, the Direct3D 12 editor drew
        // all tiles on the same sorting order with a single one of those textures.
        static Texture2D BuildTileAtlas()
        {
            int side = Size * AtlasColumns;
            var pixels = new Color32[side * side];
            for (int key = 0; key < VariantCount; key++)
            {
                var variant = new Tile((Direction)(key & 15), (key & 16) != 0, (key & 32) != 0);
                DrawTile(variant, pixels, side, key % AtlasColumns * Size, key / AtlasColumns * Size);
            }
            var texture = new Texture2D(side, side, TextureFormat.RGBA32, false)
            {
                name = "Tile Atlas",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        static void DrawTile(Tile tile, Color32[] pixels, int stride, int originX, int originY)
        {
            Direction exits = tile.Exits;
            bool hasPath = exits != Direction.None;
            Color32 border = tile.IsFixed ? StoneBorder : Border;
            Color32 ground = tile.IsFixed ? Stone : Ground;
            Color32 speck = tile.IsFixed ? StoneSpeck : GroundSpeck;

            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float fx = x + 0.5f - Size * 0.5f;
                float fy = y + 0.5f - Size * 0.5f;
                Color32 c;
                if (hasPath && InPath(exits, fx, fy, 7f)) c = PathFill;
                else if (hasPath && InPath(exits, fx, fy, 10f)) c = PathEdge;
                else if (x < 1 || y < 1 || x >= Size - 1 || y >= Size - 1) c = Clear;
                else if (x < 4 || y < 4 || x >= Size - 4 || y >= Size - 4) c = border;
                else if (tile.IsFixed && BoltAt(x, y, out bool boltEdge)) c = boltEdge ? BoltEdge : Bolt;
                else c = (x * 7 + y * 13) % 23 == 0 ? speck : ground;

                if (tile.HasCrystal)
                {
                    // A diamond with a lit upper-left facet.
                    float v = Mathf.Abs(fx) / 10f + Mathf.Abs(fy) / 14f;
                    if (v <= 1f) c = v > 0.78f ? CrystalEdge : (fx < 0f && fy > 0f ? CrystalLight : CrystalFill);
                }
                pixels[(originY + y) * stride + originX + x] = c;
            }
        }

        static bool BoltAt(int x, int y, out bool edge)
        {
            const float inset = 10.5f;
            float bx = x + 0.5f < Size * 0.5f ? inset : Size - inset;
            float by = y + 0.5f < Size * 0.5f ? inset : Size - inset;
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(bx, by));
            edge = d > 2.6f;
            return d <= 4f;
        }

        // Texture space has y pointing up, so the Up arm covers the top half.
        static bool InPath(Direction exits, float fx, float fy, float halfWidth)
        {
            bool vertical = Mathf.Abs(fx) <= halfWidth;
            bool horizontal = Mathf.Abs(fy) <= halfWidth;
            return (exits.Has(Direction.Up) && vertical && fy >= -halfWidth)
                || (exits.Has(Direction.Down) && vertical && fy <= halfWidth)
                || (exits.Has(Direction.Right) && horizontal && fx >= -halfWidth)
                || (exits.Has(Direction.Left) && horizontal && fx <= halfWidth);
        }

        public static Sprite White
        {
            get
            {
                if (white != null) return white;
                var pixels = new Color32[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
                white = Make(pixels, 4, "White");
                return white;
            }
        }

        public static Sprite StartMarker =>
            start != null ? start : start = Disc(11f, 3f, new Color32(64, 150, 214, 255), new Color32(255, 255, 255, 255), "Start");

        public static Sprite GoalMarker =>
            goal != null ? goal : goal = Ring(21f, 13f, new Color32(250, 204, 64, 255), new Color32(150, 104, 20, 255), "Goal");

        public static Sprite Walker =>
            walker != null ? walker : walker = Disc(15f, 3f, new Color32(236, 104, 64, 255), new Color32(255, 255, 255, 255), "Walker");

        public static Sprite Padlock
        {
            get
            {
                if (padlock != null) return padlock;
                var body = new Color32(70, 70, 82, 255);
                var shine = new Color32(150, 150, 166, 255);
                var pixels = new Color32[Size * Size];
                for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float fx = x + 0.5f - 32f;
                    float fy = y + 0.5f - 38f;
                    float d = Mathf.Sqrt(fx * fx + fy * fy);
                    bool inBody = x >= 14 && x < 50 && y >= 6 && y < 38;
                    bool inShackle = fy >= 0f && d >= 9f && d <= 15f;
                    Color32 c = Clear;
                    if (inShackle) c = shine;
                    if (inBody) c = (x < 17 || x >= 47 || y < 9 || y >= 35) ? shine : body;
                    if (x >= 30 && x < 34 && y >= 16 && y < 28) c = shine;
                    pixels[y * Size + x] = c;
                }
                padlock = Make(pixels, Size, "Padlock");
                return padlock;
            }
        }

        static Sprite Disc(float radius, float outline, Color32 fill, Color32 edge, string name)
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float d = Distance(x, y);
                pixels[y * Size + x] = d <= radius - outline ? fill : d <= radius ? edge : Clear;
            }
            return Make(pixels, Size, name);
        }

        static Sprite Ring(float outer, float inner, Color32 fill, Color32 edge, string name)
        {
            var pixels = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float d = Distance(x, y);
                Color32 c = Clear;
                if (d >= inner && d <= outer) c = (d < inner + 2f || d > outer - 2f) ? edge : fill;
                pixels[y * Size + x] = c;
            }
            return Make(pixels, Size, name);
        }

        static float Distance(int x, int y)
        {
            float fx = x + 0.5f - Size * 0.5f;
            float fy = y + 0.5f - Size * 0.5f;
            return Mathf.Sqrt(fx * fx + fy * fy);
        }

        internal static Sprite Make(Color32[] pixels, int size, string name,
            FilterMode filter = FilterMode.Point, float pixelsPerUnit = 0f, Vector4 border = default)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = filter,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                pixelsPerUnit > 0f ? pixelsPerUnit : size, 0, SpriteMeshType.FullRect, border);
            sprite.name = name;
            return sprite;
        }
    }
}
