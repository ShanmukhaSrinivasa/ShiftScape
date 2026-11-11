using UnityEngine;

namespace ShiftScape.Game
{
    // A vertical sky gradient parented to the camera and stretched to fill its view.
    public sealed class Background : MonoBehaviour
    {
        const int Order = -100;
        static readonly Color32 Top = new Color32(168, 214, 236, 255);
        static readonly Color32 Bottom = new Color32(112, 168, 198, 255);

        Camera cam;
        SpriteRenderer sr;

        public static Background Create(Camera camera)
        {
            var go = new GameObject("Background");
            go.transform.SetParent(camera.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 20f);
            var background = go.AddComponent<Background>();
            background.cam = camera;
            return background;
        }

        void Awake()
        {
            const int height = 128;
            var pixels = new Color32[2 * height];
            for (int y = 0; y < height; y++)
            {
                Color32 c = Color32.Lerp(Bottom, Top, y / (height - 1f));
                pixels[y * 2] = pixels[y * 2 + 1] = c;
            }
            var texture = new Texture2D(2, height, TextureFormat.RGBA32, false)
            {
                name = "Background Gradient",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(texture, new Rect(0, 0, 2, height), new Vector2(0.5f, 0.5f), height);
            sr.sortingOrder = Order;
        }

        public void Fit()
        {
            if (cam == null || sr == null) return;
            Vector2 spriteSize = sr.sprite.bounds.size;
            float viewHeight = cam.orthographicSize * 2f;
            float viewWidth = viewHeight * cam.aspect;
            transform.localScale = new Vector3(viewWidth / spriteSize.x * 1.02f, viewHeight / spriteSize.y * 1.02f, 1f);
        }
    }
}
