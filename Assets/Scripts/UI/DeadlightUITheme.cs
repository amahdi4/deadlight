using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Deadlight.UI
{
    public static class DeadlightUITheme
    {
        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        private static Font headingFont;
        private static Font bodyFont;

        public static readonly Color ScreenTint = new Color(0.05f, 0.08f, 0.11f, 0.96f);
        public static readonly Color SurfaceTint = new Color(0.09f, 0.14f, 0.18f, 0.96f);
        public static readonly Color SurfaceAltTint = new Color(0.12f, 0.17f, 0.22f, 0.94f);
        public static readonly Color SoftEdge = new Color(0.22f, 0.72f, 0.86f, 0.45f);
        public static readonly Color Warning = new Color(0.95f, 0.56f, 0.24f, 1f);
        public static readonly Color Danger = new Color(0.9f, 0.26f, 0.26f, 1f);
        public static readonly Color Success = new Color(0.3f, 0.88f, 0.52f, 1f);
        public static readonly Color TextPrimary = new Color(0.95f, 0.98f, 1f, 1f);
        public static readonly Color TextSecondary = new Color(0.7f, 0.8f, 0.88f, 1f);

        public static Font HeadingFont => headingFont ??= LoadFont("Fonts/Rajdhani-SemiBold");
        public static Font BodyFont => bodyFont ??= LoadFont("Fonts/Rajdhani-Medium");
        public static Sprite CrosshairSprite => GetOrCreateSprite("Crosshair", CreateCrosshairSprite);

        public static void ApplyScreenBackground(Image image, Color tint)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = GetOrCreateSprite("ScreenBackdrop", CreateScreenBackdropSprite);
            image.type = Image.Type.Simple;
            image.color = tint;
            image.material = null;
        }

        public static void ApplyPanel(Image image, Color tint, bool emphasis = false)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = GetOrCreateSprite(emphasis ? "PanelStrong" : "PanelSoft", () => CreatePanelSprite(emphasis));
            image.type = Image.Type.Sliced;
            image.color = tint;
            image.material = null;
        }

        public static void ApplyButton(Button button, Image image, Text label, Color accent, bool quiet = false)
        {
            if (image != null)
            {
                image.sprite = GetOrCreateSprite("ButtonChrome", CreateButtonSprite);
                image.type = Image.Type.Sliced;
                image.color = quiet
                    ? new Color(accent.r * 0.55f, accent.g * 0.55f, accent.b * 0.65f, 0.9f)
                    : accent;
            }

            if (label != null)
            {
                ApplyText(label, true, TextPrimary, true);
            }

            if (button != null)
            {
                var colors = button.colors;
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.08f;
                colors.normalColor = quiet
                    ? new Color(1f, 1f, 1f, 0.95f)
                    : new Color(0.96f, 0.98f, 1f, 1f);
                colors.highlightedColor = quiet
                    ? new Color(1.12f, 1.12f, 1.12f, 1f)
                    : new Color(1.12f, 1.12f, 1.1f, 1f);
                colors.pressedColor = new Color(0.84f, 0.88f, 0.95f, 1f);
                colors.selectedColor = colors.highlightedColor;
                colors.disabledColor = new Color(0.55f, 0.6f, 0.68f, 0.55f);
                button.colors = colors;
            }
        }

        public static void ApplyText(Text text, bool heading, Color color, bool outlined = false)
        {
            if (text == null)
            {
                return;
            }

            text.font = heading ? HeadingFont : BodyFont;
            text.color = color;
            text.lineSpacing = heading ? 1f : 1.05f;
            text.supportRichText = false;

            var shadow = EnsureComponent<Shadow>(text.gameObject);
            shadow.effectColor = new Color(0f, 0f, 0f, heading ? 0.72f : 0.55f);
            shadow.effectDistance = heading ? new Vector2(2f, -2f) : new Vector2(1f, -1f);

            var outline = EnsureComponent<Outline>(text.gameObject);
            outline.enabled = outlined;
            outline.effectColor = new Color(0f, 0f, 0f, 0.28f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        public static void ApplyLine(Image image, Color tint)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = GetOrCreateSprite("Line", CreateLineSprite);
            image.type = Image.Type.Sliced;
            image.color = tint;
        }

        public static Font LoadFont(string resourcePath)
        {
            Font font = Resources.Load<Font>(resourcePath);
            if (font != null)
            {
                return font;
            }

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            if (font != null)
            {
                return font;
            }

            string[] installedFonts = Font.GetOSInstalledFontNames();
            if (installedFonts != null && installedFonts.Length > 0)
            {
                return Font.CreateDynamicFontFromOSFont(installedFonts[0], 16);
            }

            return null;
        }

        private static Sprite GetOrCreateSprite(string key, System.Func<Sprite> factory)
        {
            if (SpriteCache.TryGetValue(key, out Sprite cached) && cached != null)
            {
                return cached;
            }

            Sprite sprite = factory();
            SpriteCache[key] = sprite;
            return sprite;
        }

        private static Sprite CreateScreenBackdropSprite()
        {
            const int size = 160;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.52f, size * 0.68f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float ty = y / (size - 1f);

                    Color vertical = Color.Lerp(
                        new Color(0.02f, 0.03f, 0.05f, 1f),
                        new Color(0.09f, 0.13f, 0.17f, 1f),
                        Mathf.Pow(ty, 0.75f));

                    float vignette = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), center) / (size * 0.66f));
                    float grid = ((x % 14 == 0) || (y % 14 == 0)) ? 0.025f : 0f;
                    float scanline = ((y % 4) == 0) ? 0.02f : 0f;

                    vertical *= 1f - vignette * 0.32f;
                    vertical += new Color(grid, grid * 1.3f, grid * 1.6f, 0f);
                    vertical += new Color(scanline * 0.4f, scanline * 0.55f, scanline * 0.7f, 0f);
                    pixels[y * size + x] = vertical;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static Sprite CreatePanelSprite(bool emphasis)
        {
            const int size = 64;
            const int border = 10;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                    bool isBorder = edge < 2;
                    bool isInnerRing = edge < 5;
                    float noise = ((x * 17 + y * 23) % 11) * 0.004f;

                    Color color = emphasis
                        ? new Color(0.35f, 0.62f, 0.76f, 0.85f)
                        : new Color(0.24f, 0.42f, 0.54f, 0.7f);

                    if (!isBorder)
                    {
                        color = isInnerRing
                            ? new Color(0.12f, 0.16f, 0.22f, 0.92f)
                            : new Color(0.06f, 0.09f, 0.12f, 0.92f);
                    }

                    color += new Color(noise, noise, noise, 0f);
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }

        private static Sprite CreateButtonSprite()
        {
            const int size = 64;
            const int border = 12;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                    float vertical = y / (size - 1f);
                    Color color;

                    if (edge < 2)
                    {
                        color = new Color(0.92f, 0.98f, 1f, 0.88f);
                    }
                    else if (edge < 5)
                    {
                        color = new Color(0.35f, 0.45f, 0.55f, 0.95f);
                    }
                    else
                    {
                        color = Color.Lerp(
                            new Color(0.16f, 0.2f, 0.26f, 1f),
                            new Color(0.07f, 0.1f, 0.14f, 1f),
                            vertical);
                    }

                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                size,
                0,
                SpriteMeshType.FullRect,
                new Vector4(border, border, border, border));
        }

        private static Sprite CreateLineSprite()
        {
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8);
        }

        private static Sprite CreateCrosshairSprite()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            DrawRect(pixels, size, 29, 29, 6, 6, new Color(1f, 1f, 1f, 0.95f));
            DrawRect(pixels, size, 31, 10, 2, 12, new Color(1f, 1f, 1f, 0.95f));
            DrawRect(pixels, size, 31, 42, 2, 12, new Color(1f, 1f, 1f, 0.95f));
            DrawRect(pixels, size, 10, 31, 12, 2, new Color(1f, 1f, 1f, 0.95f));
            DrawRect(pixels, size, 42, 31, 12, 2, new Color(1f, 1f, 1f, 0.95f));
            DrawRect(pixels, size, 27, 27, 10, 10, new Color(0.12f, 0.18f, 0.22f, 0.3f));

            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private static void DrawRect(Color[] pixels, int width, int x, int y, int rectWidth, int rectHeight, Color color)
        {
            for (int yy = y; yy < y + rectHeight; yy++)
            {
                for (int xx = x; xx < x + rectWidth; xx++)
                {
                    if (xx < 0 || yy < 0 || xx >= width || yy >= width)
                    {
                        continue;
                    }

                    pixels[yy * width + xx] = color;
                }
            }
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T component = target.GetComponent<T>();
            if (component == null)
            {
                component = target.AddComponent<T>();
            }

            return component;
        }
    }
}
