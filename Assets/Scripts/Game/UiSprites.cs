using UnityEngine;

namespace ShiftScape.Game
{
    // White shapes meant to be tinted by Image.color.
    public static class UiSprites
    {
        static Sprite roundedRect, star, speakerOn, speakerOff;

        public static Sprite SpeakerOn => speakerOn != null ? speakerOn : speakerOn = Speaker(false);
        public static Sprite SpeakerOff => speakerOff != null ? speakerOff : speakerOff = Speaker(true);

        static Sprite Speaker(bool muted)
        {
            const int size = 96;
            var cone = new[] { new Vector2(20f, 36f), new Vector2(34f, 36f), new Vector2(52f, 20f), new Vector2(52f, 76f), new Vector2(34f, 60f), new Vector2(20f, 60f) };
            var pixels = new Color32[size * size];
            const int samples = 3;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int inside = 0;
                for (int sy = 0; sy < samples; sy++)
                for (int sx = 0; sx < samples; sx++)
                {
                    var p = new Vector2(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples);
                    if (Contains(cone, p) || (muted ? OnCross(p) : OnWave(p))) inside++;
                }
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(inside * 255 / (samples * samples)));
            }
            return TileSpriteFactory.Make(pixels, size, muted ? "UI Speaker Off" : "UI Speaker On", FilterMode.Bilinear, 100f);
        }

        static bool OnWave(Vector2 p)
        {
            Vector2 d = p - new Vector2(52f, 48f);
            if (d.x <= 0f || Mathf.Abs(d.y) > d.x * 1.1f) return false;
            float r = d.magnitude;
            return Mathf.Abs(r - 16f) < 3.2f || Mathf.Abs(r - 28f) < 3.2f;
        }

        static bool OnCross(Vector2 p)
        {
            Vector2 d = p - new Vector2(70f, 48f);
            if (Mathf.Abs(d.x) > 13f || Mathf.Abs(d.y) > 13f) return false;
            return Mathf.Abs(d.x - d.y) < 4.5f || Mathf.Abs(d.x + d.y) < 4.5f;
        }

        // 9-sliced, so it stretches to any button or panel size without distorting the corners.
        public static Sprite RoundedRect
        {
            get
            {
                if (roundedRect != null) return roundedRect;
                const int size = 96;
                const float radius = 28f;
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    byte alpha = (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
                float b = radius + 2f;
                roundedRect = TileSpriteFactory.Make(pixels, size, "UI Rounded Rect", FilterMode.Bilinear, 100f, new Vector4(b, b, b, b));
                return roundedRect;
            }
        }

        public static Sprite Star
        {
            get
            {
                if (star != null) return star;
                const int size = 128;
                var points = new Vector2[10];
                for (int i = 0; i < 10; i++)
                {
                    float angle = Mathf.PI * 0.5f + i * Mathf.PI / 5f;
                    float r = i % 2 == 0 ? 60f : 25f;
                    points[i] = new Vector2(64f + Mathf.Cos(angle) * r, 62f + Mathf.Sin(angle) * r);
                }
                var pixels = new Color32[size * size];
                const int samples = 4;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int inside = 0;
                    for (int sy = 0; sy < samples; sy++)
                    for (int sx = 0; sx < samples; sx++)
                        if (Contains(points, new Vector2(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples))) inside++;
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(inside * 255 / (samples * samples)));
                }
                star = TileSpriteFactory.Make(pixels, size, "UI Star", FilterMode.Bilinear, 100f);
                return star;
            }
        }

        static bool Contains(Vector2[] polygon, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Vector2 a = polygon[i], b = polygon[j];
                if ((a.y > p.y) != (b.y > p.y) && p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }
    }
}
