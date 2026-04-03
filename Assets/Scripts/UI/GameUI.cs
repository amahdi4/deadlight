using UnityEngine;
using UnityEngine.UI;
using Deadlight.Core;
using Deadlight.Systems;
using Deadlight.Data;
using Deadlight.Player;
using Deadlight.Narrative;
using System.Collections;
using System.Collections.Generic;

namespace Deadlight.UI
{
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }

        private Font _font;
        private Canvas _canvas;
        private GameObject _canvasRoot;

        private GameObject _mainMenuPanel;
        private GameObject _mapSelectPanel;
        private GameObject _pausePanel;
        private GameObject _dawnShopPanel;
        private GameObject _gameOverPanel;
        private GameObject _victoryPanel;
        private GameObject _leaderboardPanel;

        private Text _shopPointsText;
        private Text _shopTitleText;
        private Text _shopSummaryText;
        private List<Button> _shopBuyButtons = new List<Button>();

        private GameObject _weaponsTabContent;
        private GameObject _upgradesTabContent;
        private Button _weaponsTabBtn;
        private Button _upgradesTabBtn;

        private HashSet<WeaponType> _purchasedWeapons = new HashSet<WeaponType>();

        private List<Text> _upgradeLabels = new List<Text>();
        private List<Button> _upgradeBuyButtons = new List<Button>();

        private bool _waitingForEnding;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
            {
                _font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            }
            if (_font == null)
            {
                _font = DeadlightUITheme.BodyFont;
            }

            EnsureEventSystem();
            BuildAllUI();
            HideAllPanels();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                GameManager.Instance.OnPauseChanged += OnPauseChanged;
                OnGameStateChanged(GameManager.Instance.CurrentState);
            }
            else
            {
                _mainMenuPanel?.SetActive(true);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
                GameManager.Instance.OnPauseChanged -= OnPauseChanged;
            }
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private void BuildAllUI()
        {
            _canvasRoot = new GameObject("GameUICanvas");
            _canvasRoot.transform.SetParent(transform);

            _canvas = _canvasRoot.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            _canvasRoot.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasRoot.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            _canvasRoot.AddComponent<GraphicRaycaster>();

            BuildMainMenu();
            BuildMapSelect();
            BuildPauseMenu();
            BuildDawnShop();
            BuildGameOverScreen();
            BuildVictoryScreen();
            BuildLeaderboardPanel();
        }

        // ===================== MAIN MENU =====================

        private void BuildMainMenu()
        {
            _mainMenuPanel = CreatePanel(_canvasRoot.transform, "MainMenuPanel");
            var bg = _mainMenuPanel.GetComponent<Image>();
            bg.sprite = null;
            bg.color = new Color(0.08f, 0.11f, 0.14f, 1f);

            var left = CreateStretchMenuSurface(_mainMenuPanel.transform, "MainMenuLeft",
                new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(44f, 44f), new Vector2(572f, -44f),
                new Color(0.05f, 0.07f, 0.09f, 0.94f), 10f);
            var right = CreateStretchMenuSurface(_mainMenuPanel.transform, "MainMenuRight",
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(604f, 44f), new Vector2(-44f, -44f),
                new Color(0.05f, 0.07f, 0.09f, 0.9f), 10f);

            CreateMenuText(left.transform, "Title", "DEADLIGHT", 58, FontStyle.Bold, Color.white,
                new Vector2(0f, 1f), new Vector2(34f, -38f), new Vector2(430f, 60f), TextAnchor.UpperLeft);
            CreateMenuText(left.transform, "Subtitle", "Survival After Dark", 28, FontStyle.Bold, new Color(0.85f, 0.9f, 0.95f),
                new Vector2(0f, 1f), new Vector2(36f, -102f), new Vector2(430f, 34f), TextAnchor.UpperLeft);
            CreateMenuText(left.transform, "Body",
                "Step 1: click a difficulty.\nStep 2: choose a deployment zone.",
                21, FontStyle.Normal, new Color(0.8f, 0.85f, 0.9f),
                new Vector2(0f, 1f), new Vector2(36f, -168f), new Vector2(440f, 64f), TextAnchor.UpperLeft);

            CreateMenuText(left.transform, "DifficultyPromptLabel", "CHOOSE DIFFICULTY", 14, FontStyle.Bold,
                new Color(0.52f, 0.84f, 1f), new Vector2(0f, 1f), new Vector2(36f, -256f), new Vector2(220f, 18f), TextAnchor.UpperLeft);
            CreateMenuText(left.transform, "DifficultyPrompt", "Click a difficulty to continue", 22, FontStyle.Bold,
                Color.white, new Vector2(0f, 1f), new Vector2(36f, -280f), new Vector2(420f, 30f), TextAnchor.UpperLeft);

            CreateMenuActionButton(left.transform, "EasyButton", "Easy", "More recovery room and lower pressure.",
                new Color(0.25f, 0.64f, 0.36f), new Vector2(0f, 1f), new Vector2(36f, -336f), new Vector2(470f, 92f),
                () => OnDifficultySelected(Difficulty.Easy), GetUIAtlasSprite("Button Accept"));
            CreateMenuActionButton(left.transform, "NormalButton", "Normal", "Balanced pacing and enemy pressure.",
                new Color(0.8f, 0.67f, 0.22f), new Vector2(0f, 1f), new Vector2(36f, -438f), new Vector2(470f, 92f),
                () => OnDifficultySelected(Difficulty.Normal), GetUIAtlasSprite("Button Accept"));
            CreateMenuActionButton(left.transform, "HardButton", "Hard", "Little room for mistakes. Aggressive runs.",
                new Color(0.78f, 0.34f, 0.3f), new Vector2(0f, 1f), new Vector2(36f, -540f), new Vector2(470f, 92f),
                () => OnDifficultySelected(Difficulty.Hard), GetUIAtlasSprite("Button Accept"));

            CreateMenuMiniButton(left.transform, "LeaderboardButton", "Leaderboard",
                new Color(0.24f, 0.43f, 0.72f), new Vector2(0f, 0f), new Vector2(36f, 34f), new Vector2(286f, 58f),
                ShowLeaderboard, GetUIAtlasSprite("Panel White"));
            CreateMenuMiniButton(left.transform, "QuitButton", "Quit",
                new Color(0.42f, 0.46f, 0.54f), new Vector2(0f, 0f), new Vector2(338f, 34f), new Vector2(168f, 58f),
                QuitGame, GetUIAtlasSprite("Button Back"));

            CreateMenuText(right.transform, "RightTitle", "Deployment Zones", 34, FontStyle.Bold, Color.white,
                new Vector2(0f, 1f), new Vector2(34f, -34f), new Vector2(420f, 40f), TextAnchor.UpperLeft);
            CreateMenuText(right.transform, "RightBody",
                "Actual overhead previews from the current maps. Pick a difficulty now, then choose the zone on the next screen.",
                20, FontStyle.Normal, new Color(0.8f, 0.85f, 0.9f),
                new Vector2(0f, 1f), new Vector2(34f, -82f), new Vector2(900f, 54f), TextAnchor.UpperLeft);

            CreateMainMenuPreview(right.transform, "TownPreview", LoadMenuPreviewSprite("TownCenter"), "Town Center",
                new Vector2(0f, 1f), new Vector2(34f, -176f), new Vector2(980f, 156f), new Color(0.27f, 0.63f, 0.38f));
            CreateMainMenuPreview(right.transform, "IndustrialPreview", LoadMenuPreviewSprite("Industrial"), "Industrial District",
                new Vector2(0f, 1f), new Vector2(34f, -356f), new Vector2(980f, 156f), new Color(0.8f, 0.55f, 0.25f));
            CreateMainMenuPreview(right.transform, "SuburbanPreview", LoadMenuPreviewSprite("Suburban"), "Suburban Outskirts",
                new Vector2(0f, 1f), new Vector2(34f, -536f), new Vector2(980f, 156f), new Color(0.32f, 0.56f, 0.74f));
        }

        private void OnDifficultySelected(Difficulty difficulty)
        {
            GameManager.Instance?.SetDifficulty(difficulty);
            _mainMenuPanel?.SetActive(false);
            _mapSelectPanel?.SetActive(true);
        }

        // ===================== MAP SELECT =====================

        private void BuildMapSelect()
        {
            _mapSelectPanel = CreatePanel(_canvasRoot.transform, "MapSelectPanel");
            var bg = _mapSelectPanel.GetComponent<Image>();
            bg.sprite = null;
            bg.color = new Color(0.08f, 0.11f, 0.14f, 1f);

            CreateMenuText(_mapSelectPanel.transform, "Title", "Choose a Zone", 50, FontStyle.Bold, Color.white,
                new Vector2(0f, 1f), new Vector2(86f, -64f), new Vector2(420f, 56f), TextAnchor.UpperLeft);
            CreateMenuText(_mapSelectPanel.transform, "Subtitle",
                "Every card uses a real overhead preview from the current level build.",
                19, FontStyle.Normal, new Color(0.76f, 0.81f, 0.87f),
                new Vector2(0f, 1f), new Vector2(88f, -118f), new Vector2(620f, 28f), TextAnchor.UpperLeft);

            CreateMenuMiniButton(_mapSelectPanel.transform, "BackButton", "Back",
                new Color(0.4f, 0.45f, 0.52f), new Vector2(0f, 1f), new Vector2(88f, -30f), new Vector2(140f, 42f),
                () =>
                {
                    _mapSelectPanel?.SetActive(false);
                    _mainMenuPanel?.SetActive(true);
                }, GetUIAtlasSprite("Button Back"));

            CreateMapSelectionCard(_mapSelectPanel.transform, "TownCard", LoadMenuPreviewSprite("TownCenter"),
                "Town Center", "Balanced", "Wide streets, civic lots, and medium cover.",
                new Color(0.27f, 0.63f, 0.38f), new Vector2(0.2f, 0.48f), MapType.TownCenter);
            CreateMapSelectionCard(_mapSelectPanel.transform, "IndustrialCard", LoadMenuPreviewSprite("Industrial"),
                "Industrial District", "Tactical", "Tighter lanes, harder angles, and harsher sightlines.",
                new Color(0.78f, 0.55f, 0.28f), new Vector2(0.5f, 0.48f), MapType.Industrial);
            CreateMapSelectionCard(_mapSelectPanel.transform, "SuburbanCard", LoadMenuPreviewSprite("Suburban"),
                "Suburban Outskirts", "Open", "More mobility, less cover, and longer rotations.",
                new Color(0.34f, 0.56f, 0.74f), new Vector2(0.8f, 0.48f), MapType.Suburban);
        }

        private GameObject CreateMenuSurface(Transform parent, string name, Vector2 anchor, Vector2 size, Color color,
            float shadowAlpha, Vector2? anchoredPos = null, Vector2? pivot = null)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos ?? Vector2.zero;
            rect.sizeDelta = size;

            var image = panel.AddComponent<Image>();
            image.sprite = null;
            image.color = color;

            var shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, shadowAlpha / 255f);
            shadow.effectDistance = new Vector2(0f, -8f);
            return panel;
        }

        private GameObject CreateStretchMenuSurface(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax, Color color, float shadowAlpha)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var image = panel.AddComponent<Image>();
            image.sprite = null;
            image.color = color;

            var shadow = panel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, shadowAlpha / 255f);
            shadow.effectDistance = new Vector2(0f, -8f);
            return panel;
        }

        private Text CreateMenuText(Transform parent, string name, string text, int fontSize, FontStyle style, Color color,
            Vector2 anchor, Vector2 anchoredPos, Vector2 size, TextAnchor alignment)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var label = obj.AddComponent<Text>();
            label.font = _font;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.color = color;
            label.alignment = alignment;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;

            var shadow = obj.AddComponent<Shadow>();
            shadow.enabled = fontSize >= 24;
            shadow.effectColor = new Color(0f, 0f, 0f, 0.16f);
            shadow.effectDistance = new Vector2(1f, -1f);
            return label;
        }

        private void CreateMenuActionButton(Transform parent, string name, string title, string subtitle, Color accent,
            Vector2 anchor, Vector2 anchoredPos, Vector2 size, System.Action onClick, Sprite icon)
        {
            var buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);
            var rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var image = buttonObj.AddComponent<Image>();
            image.sprite = null;
            image.color = new Color(0.11f, 0.14f, 0.18f, 0.98f);
            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            colors.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(() => onClick?.Invoke());

            var badge = new GameObject("Accent");
            badge.transform.SetParent(buttonObj.transform, false);
            var badgeRect = badge.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0f, 0.5f);
            badgeRect.anchorMax = new Vector2(0f, 0.5f);
            badgeRect.pivot = new Vector2(0f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(18f, 0f);
            badgeRect.sizeDelta = new Vector2(42f, 42f);
            var badgeImage = badge.AddComponent<Image>();
            badgeImage.sprite = icon;
            badgeImage.preserveAspect = true;
            badgeImage.color = accent;
            badgeImage.raycastTarget = false;

            CreateMenuText(buttonObj.transform, "Title", title, 32, FontStyle.Bold, Color.white,
                new Vector2(0f, 1f), new Vector2(78f, -16f), new Vector2(size.x - 96f, 34f), TextAnchor.UpperLeft);
            CreateMenuText(buttonObj.transform, "Subtitle", subtitle, 18, FontStyle.Normal, new Color(0.82f, 0.86f, 0.9f),
                new Vector2(0f, 1f), new Vector2(80f, -54f), new Vector2(size.x - 104f, 24f), TextAnchor.UpperLeft);
        }

        private void CreateMenuMiniButton(Transform parent, string name, string title, Color accent,
            Vector2 anchor, Vector2 anchoredPos, Vector2 size, System.Action onClick, Sprite icon)
        {
            var buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);
            var rect = buttonObj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;

            var image = buttonObj.AddComponent<Image>();
            image.sprite = null;
            image.color = new Color(0.1f, 0.12f, 0.16f, 0.96f);
            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(() => onClick?.Invoke());

            if (icon != null)
            {
                var iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(buttonObj.transform, false);
                var iconRect = iconObj.AddComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(14f, 0f);
                iconRect.sizeDelta = new Vector2(22f, 22f);
                var iconImage = iconObj.AddComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
                iconImage.color = accent;
                iconImage.raycastTarget = false;
            }

            CreateMenuText(buttonObj.transform, "Label", title, 20, FontStyle.Bold, Color.white,
                new Vector2(0f, 1f), new Vector2(icon != null ? 46f : 18f, -14f), new Vector2(size.x - 60f, 28f), TextAnchor.UpperLeft);
        }

        private void CreateMainMenuPreview(Transform parent, string name, Sprite preview, string label,
            Vector2 anchor, Vector2 anchoredPos, Vector2 size, Color accent)
        {
            var row = CreateMenuSurface(parent, name, anchor, size, new Color(0.09f, 0.11f, 0.14f, 0.96f), 5f, anchoredPos, new Vector2(0f, 1f));

            var previewObj = new GameObject("Preview");
            previewObj.transform.SetParent(row.transform, false);
            var previewRect = previewObj.AddComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0f, 0.5f);
            previewRect.anchorMax = new Vector2(0f, 0.5f);
            previewRect.pivot = new Vector2(0f, 0.5f);
            previewRect.anchoredPosition = new Vector2(14f, 0f);
            previewRect.sizeDelta = new Vector2(232f, size.y - 28f);
            var previewImage = previewObj.AddComponent<Image>();
            previewImage.sprite = preview;
            previewImage.preserveAspect = false;
            previewImage.color = Color.white;
            previewImage.raycastTarget = false;

            var tintObj = new GameObject("Tint");
            tintObj.transform.SetParent(previewObj.transform, false);
            var tintRect = tintObj.AddComponent<RectTransform>();
            tintRect.anchorMin = Vector2.zero;
            tintRect.anchorMax = Vector2.one;
            tintRect.offsetMin = Vector2.zero;
            tintRect.offsetMax = Vector2.zero;
            var tintImage = tintObj.AddComponent<Image>();
            tintImage.color = new Color(0f, 0f, 0f, 0.18f);
            tintImage.raycastTarget = false;

            CreateMenuText(row.transform, "Label", label, 26, FontStyle.Bold, Color.white,
                new Vector2(0f, 1f), new Vector2(272f, -28f), new Vector2(size.x - 360f, 32f), TextAnchor.UpperLeft);
            CreateMenuText(row.transform, "Desc", "Live overhead capture from the current build.", 18, FontStyle.Normal,
                new Color(0.82f, 0.86f, 0.9f), new Vector2(0f, 1f), new Vector2(274f, -72f), new Vector2(size.x - 380f, 24f), TextAnchor.UpperLeft);

            var marker = new GameObject("Marker");
            marker.transform.SetParent(row.transform, false);
            var markerRect = marker.AddComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(1f, 0.5f);
            markerRect.anchorMax = new Vector2(1f, 0.5f);
            markerRect.pivot = new Vector2(1f, 0.5f);
            markerRect.anchoredPosition = new Vector2(-18f, 0f);
            markerRect.sizeDelta = new Vector2(18f, 18f);
            var markerImage = marker.AddComponent<Image>();
            markerImage.sprite = GetUIAtlasSprite("Panel White");
            markerImage.color = accent;
            markerImage.raycastTarget = false;
        }

        private void CreateMapSelectionCard(Transform parent, string name, Sprite preview, string title, string tag, string desc,
            Color accent, Vector2 anchor, MapType mapType)
        {
            var card = CreateMenuSurface(parent, name, anchor, new Vector2(360f, 520f),
                new Color(0.06f, 0.08f, 0.1f, 0.98f), 10f);
            var button = card.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
            colors.pressedColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            button.onClick.AddListener(() => OnMapSelected(mapType));

            var previewObj = new GameObject("Preview");
            previewObj.transform.SetParent(card.transform, false);
            var previewRect = previewObj.AddComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.5f, 1f);
            previewRect.anchorMax = new Vector2(0.5f, 1f);
            previewRect.pivot = new Vector2(0.5f, 1f);
            previewRect.anchoredPosition = new Vector2(0f, -20f);
            previewRect.sizeDelta = new Vector2(320f, 210f);
            var previewImage = previewObj.AddComponent<Image>();
            previewImage.sprite = preview;
            previewImage.color = Color.white;
            previewImage.raycastTarget = false;

            var previewShade = new GameObject("Shade");
            previewShade.transform.SetParent(previewObj.transform, false);
            var previewShadeRect = previewShade.AddComponent<RectTransform>();
            previewShadeRect.anchorMin = Vector2.zero;
            previewShadeRect.anchorMax = Vector2.one;
            previewShadeRect.offsetMin = Vector2.zero;
            previewShadeRect.offsetMax = Vector2.zero;
            var previewShadeImage = previewShade.AddComponent<Image>();
            previewShadeImage.color = new Color(0f, 0f, 0f, 0.1f);
            previewShadeImage.raycastTarget = false;

            CreateMenuText(card.transform, "Title", title, 30, FontStyle.Bold, Color.white,
                new Vector2(0f, 1f), new Vector2(22f, -254f), new Vector2(280f, 36f), TextAnchor.UpperLeft);
            CreateMenuText(card.transform, "Tag", tag, 16, FontStyle.Bold, accent,
                new Vector2(0f, 1f), new Vector2(22f, -296f), new Vector2(180f, 22f), TextAnchor.UpperLeft);
            CreateMenuText(card.transform, "Desc", desc, 17, FontStyle.Normal, new Color(0.76f, 0.81f, 0.87f),
                new Vector2(0f, 1f), new Vector2(22f, -330f), new Vector2(316f, 76f), TextAnchor.UpperLeft);

            CreateMenuMiniButton(card.transform, "DeployButton", "Deploy",
                accent, new Vector2(0f, 1f), new Vector2(22f, -444f), new Vector2(316f, 52f), () => OnMapSelected(mapType),
                GetUIAtlasSprite("Button Accept"));
        }

        private Sprite LoadMenuPreviewSprite(string key)
        {
            Sprite sprite = Resources.Load<Sprite>($"MenuPreviews/{key}");
            if (sprite != null)
            {
                return sprite;
            }

            Texture2D texture = Resources.Load<Texture2D>($"MenuPreviews/{key}");
            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private Sprite GetUIAtlasSprite(string key)
        {
            Texture2D atlas = Resources.Load<Texture2D>("UIAtlas");
            if (atlas == null)
            {
                return null;
            }

            return key switch
            {
                "Panel White" => Sprite.Create(atlas, new Rect(0f, 25f, 32f, 32f), new Vector2(0.5f, 0.5f), 24f),
                "Panel Black" => Sprite.Create(atlas, new Rect(33f, 25f, 32f, 32f), new Vector2(0.5f, 0.5f), 24f),
                "Button Accept" => Sprite.Create(atlas, new Rect(66f, 25f, 32f, 32f), new Vector2(0.5f, 0.5f), 24f),
                "Button Back" => Sprite.Create(atlas, new Rect(100f, 0f, 24f, 24f), new Vector2(0.5f, 0.5f), 24f),
                _ => null
            };
        }

        private void BuildMapOption(Transform parent, string mapName, string desc,
            string tag, Color accentColor, float yAnchor, MapType mapType)
        {
            float cardWidth = 700f;
            float cardHeight = 120f;
            float accentBarWidth = 5f;

            // Card container
            var container = new GameObject($"MapOption_{mapName}");
            container.transform.SetParent(parent);
            var containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, yAnchor);
            containerRect.anchorMax = new Vector2(0.5f, yAnchor);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(cardWidth, cardHeight);

            // Card background
            var containerImg = container.AddComponent<Image>();
            DeadlightUITheme.ApplyPanel(containerImg, new Color(0.12f, 0.15f, 0.19f, 0.96f), true);

            // Button behavior
            var btn = container.AddComponent<Button>();
            btn.targetGraphic = containerImg;
            var colors = btn.colors;
            colors.normalColor = new Color(1f, 1f, 1f, 0.96f);
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.88f, 0.9f, 0.95f, 1f);
            colors.selectedColor = colors.normalColor;
            btn.colors = colors;
            btn.onClick.AddListener(() => OnMapSelected(mapType));

            // Left accent bar
            var accentBar = new GameObject("AccentBar");
            accentBar.transform.SetParent(container.transform);
            var accentRect = accentBar.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(accentBarWidth, 0f);
            var accentImg = accentBar.AddComponent<Image>();
            accentImg.color = accentColor;
            accentImg.raycastTarget = false;

            // Map name - positioned inside the card
            var nameObj = CreateLeftAlignedText(container.transform, "Name",
                mapName, 24, FontStyle.Bold, Color.white,
                new Vector2(accentBarWidth + 20f, -16f), new Vector2(cardWidth - 160f, 30f));

            // Description - positioned below name, inside the card
            CreateLeftAlignedText(container.transform, "Desc",
                desc, 14, FontStyle.Normal, new Color(0.6f, 0.62f, 0.68f),
                new Vector2(accentBarWidth + 20f, -50f), new Vector2(cardWidth - 160f, 50f));

            // Tag label on the right
            var tagObj = new GameObject("Tag");
            tagObj.transform.SetParent(container.transform);
            var tagRect = tagObj.AddComponent<RectTransform>();
            tagRect.anchorMin = new Vector2(1f, 0.5f);
            tagRect.anchorMax = new Vector2(1f, 0.5f);
            tagRect.pivot = new Vector2(1f, 0.5f);
            tagRect.anchoredPosition = new Vector2(-20f, 0f);
            tagRect.sizeDelta = new Vector2(100f, 28f);
            var tagBg = tagObj.AddComponent<Image>();
            tagBg.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.25f);
            tagBg.raycastTarget = false;

            var tagTextObj = new GameObject("TagText");
            tagTextObj.transform.SetParent(tagObj.transform);
            var tagTextRect = tagTextObj.AddComponent<RectTransform>();
            tagTextRect.anchorMin = Vector2.zero;
            tagTextRect.anchorMax = Vector2.one;
            tagTextRect.offsetMin = Vector2.zero;
            tagTextRect.offsetMax = Vector2.zero;
            var tagText = tagTextObj.AddComponent<Text>();
            tagText.text = tag;
            tagText.fontSize = 13;
            tagText.fontStyle = FontStyle.Bold;
            tagText.alignment = TextAnchor.MiddleCenter;
            DeadlightUITheme.ApplyText(tagText, true, accentColor, false);
            tagText.raycastTarget = false;

            // Bottom border line
            var borderLine = new GameObject("Border");
            borderLine.transform.SetParent(container.transform);
            var borderRect = borderLine.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0f, 0f);
            borderRect.anchorMax = new Vector2(1f, 0f);
            borderRect.pivot = new Vector2(0.5f, 0f);
            borderRect.anchoredPosition = Vector2.zero;
            borderRect.sizeDelta = new Vector2(0f, 1f);
            var borderImg = borderLine.AddComponent<Image>();
            DeadlightUITheme.ApplyLine(borderImg, new Color(0.28f, 0.38f, 0.46f, 0.55f));
            borderImg.raycastTarget = false;
        }

        private GameObject CreateLeftAlignedText(Transform parent, string name,
            string text, int fontSize, FontStyle style, Color color,
            Vector2 topLeftOffset, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeftOffset;
            rect.sizeDelta = size;

            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.UpperLeft;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            DeadlightUITheme.ApplyText(txt, fontSize >= 20 || style == FontStyle.Bold, color, fontSize >= 22);

            return obj;
        }

        private void OnMapSelected(MapType mapType)
        {
            GameManager.Instance?.SetMap(mapType);
            Time.timeScale = 1f;
            GameManager.Instance?.StartSelectedMapRun();
        }

        // ===================== PAUSE MENU =====================

        private void BuildPauseMenu()
        {
            _pausePanel = CreatePanel(_canvasRoot.transform, "PausePanel");
            DeadlightUITheme.ApplyScreenBackground(_pausePanel.GetComponent<Image>(), new Color(0.02f, 0.04f, 0.06f, 0.9f));

            var pauseCard = CreateMenuSurface(_pausePanel.transform, "PauseCard",
                new Vector2(0.5f, 0.5f), new Vector2(460f, 420f), new Color(0.05f, 0.07f, 0.09f, 0.96f), 10f);

            CreateText(pauseCard.transform, "Title",
                "PAUSED", 48, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.87f), new Vector2(0.5f, 0.87f), Vector2.zero, new Vector2(320, 64));

            CreateText(pauseCard.transform, "PauseHint",
                "Select an action", 16, TextAnchor.MiddleCenter, new Color(0.72f, 0.78f, 0.84f),
                new Vector2(0.5f, 0.76f), new Vector2(0.5f, 0.76f), Vector2.zero, new Vector2(220, 24));

            CreateButton(pauseCard.transform, "ResumeButton", "RESUME", new Color(0.2f, 0.65f, 0.3f),
                new Vector2(0.5f, 0.58f), new Vector2(280, 50), OnResume);

            CreateButton(pauseCard.transform, "PauseRestartButton", "RESTART", new Color(0.7f, 0.6f, 0.2f),
                new Vector2(0.5f, 0.43f), new Vector2(280, 50), RestartGame);

            CreateButton(pauseCard.transform, "PauseMainMenuButton", "MAIN MENU", new Color(0.5f, 0.5f, 0.5f),
                new Vector2(0.5f, 0.28f), new Vector2(280, 50), GoToMainMenu);

            CreateButton(pauseCard.transform, "PauseQuitButton", "QUIT GAME", new Color(0.65f, 0.2f, 0.2f),
                new Vector2(0.5f, 0.13f), new Vector2(280, 50), QuitGame);
        }

        private void OnResume()
        {
            GameManager.Instance?.SetPaused(false);
        }

        private void OnPauseChanged(bool paused)
        {
            if (paused && GameManager.Instance != null && GameManager.Instance.IsGameplayState)
            {
                _pausePanel?.SetActive(true);
            }
            else
            {
                _pausePanel?.SetActive(false);
            }
        }

        private void QuitGame()
        {
            GameManager.Instance?.QuitGame();
        }

        private void BuildDawnShop()
        {
            _dawnShopPanel = CreatePanel(_canvasRoot.transform, "DawnShopPanel");
            DeadlightUITheme.ApplyScreenBackground(_dawnShopPanel.GetComponent<Image>(), new Color(0.04f, 0.06f, 0.08f, 0.96f));

            CreateCard(_dawnShopPanel.transform, "ShopFrame",
                new Vector2(0.5f, 0.5f), new Vector2(820, 910), new Color(0.08f, 0.11f, 0.15f, 0.74f), new Color(0.95f, 0.72f, 0.32f, 0.45f));

            _shopTitleText = CreateText(_dawnShopPanel.transform, "ShopTitle",
                "DAWN - Night 1 Survived!", 34, TextAnchor.UpperCenter, new Color(0.95f, 0.85f, 0.4f),
                new Vector2(0.5f, 0.96f), new Vector2(0.5f, 0.96f), Vector2.zero, new Vector2(700, 45)).GetComponent<Text>();
            _shopTitleText.fontStyle = FontStyle.Bold;

            _shopSummaryText = CreateText(_dawnShopPanel.transform, "ShopSummary",
                "", 16, TextAnchor.UpperCenter, new Color(0.7f, 0.7f, 0.7f),
                new Vector2(0.5f, 0.915f), new Vector2(0.5f, 0.915f), Vector2.zero, new Vector2(700, 22)).GetComponent<Text>();

            _shopPointsText = CreateText(_dawnShopPanel.transform, "ShopPoints",
                "Points: 0", 26, TextAnchor.UpperCenter, new Color(0.4f, 1f, 0.4f),
                new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(300, 35)).GetComponent<Text>();
            _shopPointsText.fontStyle = FontStyle.Bold;

            // Supplies row
            var suppliesRow = new GameObject("SuppliesRow");
            suppliesRow.transform.SetParent(_dawnShopPanel.transform);
            var srRect = suppliesRow.AddComponent<RectTransform>();
            srRect.anchorMin = new Vector2(0.5f, 0.82f);
            srRect.anchorMax = new Vector2(0.5f, 0.82f);
            srRect.pivot = new Vector2(0.5f, 0.5f);
            srRect.anchoredPosition = Vector2.zero;
            srRect.sizeDelta = new Vector2(600, 45);

            var healBtn = CreateButton(suppliesRow.transform, "HealBtn", "Health Kit (50)", new Color(0.6f, 0.2f, 0.2f),
                new Vector2(0.25f, 0.5f), new Vector2(220, 40), BuyHealthKit);
            _shopBuyButtons.Add(healBtn.GetComponent<Button>());

            var ammoBtn = CreateButton(suppliesRow.transform, "AmmoBtn", "Ammo Refill (30)", new Color(0.7f, 0.6f, 0.15f),
                new Vector2(0.75f, 0.5f), new Vector2(220, 40), BuyAmmoRefill);
            _shopBuyButtons.Add(ammoBtn.GetComponent<Button>());

            // Armor row
            var armorRow = new GameObject("ArmorRow");
            armorRow.transform.SetParent(_dawnShopPanel.transform);
            var arRect = armorRow.AddComponent<RectTransform>();
            arRect.anchorMin = new Vector2(0.5f, 0.77f);
            arRect.anchorMax = new Vector2(0.5f, 0.77f);
            arRect.pivot = new Vector2(0.5f, 0.5f);
            arRect.anchoredPosition = Vector2.zero;
            arRect.sizeDelta = new Vector2(600, 45);

            var vest1Btn = CreateButton(armorRow.transform, "Vest1Btn", "Vest Lv1 (80)", new Color(0.2f, 0.4f, 0.7f),
                new Vector2(0.15f, 0.5f), new Vector2(160, 36), () => BuyArmor(ArmorTier.Level1, false, 80));
            _shopBuyButtons.Add(vest1Btn.GetComponent<Button>());

            var vest2Btn = CreateButton(armorRow.transform, "Vest2Btn", "Vest Lv2 (180)", new Color(0.2f, 0.4f, 0.7f),
                new Vector2(0.38f, 0.5f), new Vector2(160, 36), () => BuyArmor(ArmorTier.Level2, false, 180));
            _shopBuyButtons.Add(vest2Btn.GetComponent<Button>());

            var helm1Btn = CreateButton(armorRow.transform, "Helm1Btn", "Helm Lv1 (60)", new Color(0.5f, 0.5f, 0.6f),
                new Vector2(0.62f, 0.5f), new Vector2(160, 36), () => BuyArmor(ArmorTier.Level1, true, 60));
            _shopBuyButtons.Add(helm1Btn.GetComponent<Button>());

            var helm2Btn = CreateButton(armorRow.transform, "Helm2Btn", "Helm Lv2 (140)", new Color(0.5f, 0.5f, 0.6f),
                new Vector2(0.85f, 0.5f), new Vector2(160, 36), () => BuyArmor(ArmorTier.Level2, true, 140));
            _shopBuyButtons.Add(helm2Btn.GetComponent<Button>());

            // Tab buttons
            _weaponsTabBtn = CreateButton(_dawnShopPanel.transform, "WeaponsTab", "WEAPONS",
                new Color(0.3f, 0.45f, 0.6f),
                new Vector2(0.35f, 0.755f), new Vector2(200, 38), () => ShowShopTab(true)).GetComponent<Button>();
            _upgradesTabBtn = CreateButton(_dawnShopPanel.transform, "UpgradesTab", "UPGRADES",
                new Color(0.45f, 0.35f, 0.55f),
                new Vector2(0.65f, 0.755f), new Vector2(200, 38), () => ShowShopTab(false)).GetComponent<Button>();

            // Weapons content
            _weaponsTabContent = new GameObject("WeaponsContent");
            _weaponsTabContent.transform.SetParent(_dawnShopPanel.transform);
            var wcRect = _weaponsTabContent.AddComponent<RectTransform>();
            wcRect.anchorMin = new Vector2(0.5f, 0.18f);
            wcRect.anchorMax = new Vector2(0.5f, 0.72f);
            wcRect.pivot = new Vector2(0.5f, 0.5f);
            wcRect.anchoredPosition = Vector2.zero;
            wcRect.sizeDelta = new Vector2(650, 0);

            BuildWeaponItems();

            // Upgrades content
            _upgradesTabContent = new GameObject("UpgradesContent");
            _upgradesTabContent.transform.SetParent(_dawnShopPanel.transform);
            var ucRect = _upgradesTabContent.AddComponent<RectTransform>();
            ucRect.anchorMin = new Vector2(0.5f, 0.18f);
            ucRect.anchorMax = new Vector2(0.5f, 0.72f);
            ucRect.pivot = new Vector2(0.5f, 0.5f);
            ucRect.anchoredPosition = Vector2.zero;
            ucRect.sizeDelta = new Vector2(650, 0);

            BuildUpgradeItems();
            _upgradesTabContent.SetActive(false);

            CreateButton(_dawnShopPanel.transform, "ContinueButton", "Continue to Next Night",
                new Color(0.15f, 0.55f, 0.85f),
                new Vector2(0.5f, 0.08f), new Vector2(350, 55), OnContinueToNextNight);
        }

        private void BuildWeaponItems()
        {
            float y = 0;
            int h = 72;
            AddWeaponShopItem(_weaponsTabContent.transform, "Shotgun", "Close-range, 8 pellets", 100, 1,
                WeaponType.Shotgun, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "SMG", "Auto, fast fire rate", 150, 2,
                WeaponType.SMG, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "Sniper Rifle", "High damage, long range", 250, 2,
                WeaponType.SniperRifle, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "Assault Rifle", "Auto, balanced stats", 200, 3,
                WeaponType.AssaultRifle, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "Grenade Launcher", "Explosive, area damage", 350, 4,
                WeaponType.GrenadeLauncher, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "Flamethrower", "Burn damage over time", 400, 4,
                WeaponType.Flamethrower, ref y, h);
            AddWeaponShopItem(_weaponsTabContent.transform, "Railgun", "Piercing charged shot", 500, 5,
                WeaponType.Railgun, ref y, h);
        }

        private void AddWeaponShopItem(Transform parent, string name, string desc, int cost, int unlockNight,
            WeaponType weaponType, ref float y, int height)
        {
            var root = new GameObject($"Weapon_{name}");
            root.transform.SetParent(parent);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(620, height - 4);

            var bg = root.AddComponent<Image>();
            DeadlightUITheme.ApplyPanel(bg, new Color(0.11f, 0.15f, 0.2f, 0.9f));

            CreateText(root.transform, "Name", name, 22, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(15, 8), new Vector2(300, 30));
            CreateText(root.transform, "Desc", $"{desc}  |  {cost} pts  |  Night {unlockNight}+", 15,
                TextAnchor.MiddleLeft, new Color(0.65f, 0.65f, 0.65f),
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(15, -12), new Vector2(400, 22));

            var wt = weaponType;
            var buyBtn = CreateButton(root.transform, "Buy", "BUY", new Color(0.25f, 0.55f, 0.25f),
                new Vector2(1, 0.5f), new Vector2(90, 38), () => BuyWeapon(wt, cost, unlockNight));
            var btnRect = buyBtn.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0.5f);
            btnRect.anchorMax = new Vector2(1, 0.5f);
            btnRect.pivot = new Vector2(1, 0.5f);
            btnRect.anchoredPosition = new Vector2(-10, 0);

            _shopBuyButtons.Add(buyBtn.GetComponent<Button>());
            y -= height;
        }

        private void BuildUpgradeItems()
        {
            float y = 0;
            int h = 65;
            AddUpgradeItem(_upgradesTabContent.transform, "Damage", ref y, h, () => {
                var u = PlayerUpgrades.Instance;
                if (u != null && u.TryUpgradeDamage()) RefreshShop();
            });
            AddUpgradeItem(_upgradesTabContent.transform, "Fire Rate", ref y, h, () => {
                var u = PlayerUpgrades.Instance;
                if (u != null && u.TryUpgradeFireRate()) RefreshShop();
            });
            AddUpgradeItem(_upgradesTabContent.transform, "Magazine", ref y, h, () => {
                var u = PlayerUpgrades.Instance;
                if (u != null && u.TryUpgradeMagazine()) RefreshShop();
            });
            AddUpgradeItem(_upgradesTabContent.transform, "Max Health", ref y, h, () => {
                var u = PlayerUpgrades.Instance;
                if (u != null && u.TryUpgradeHealth()) RefreshShop();
            });
            AddUpgradeItem(_upgradesTabContent.transform, "Sprint Speed", ref y, h, () => {
                var u = PlayerUpgrades.Instance;
                if (u != null && u.TryUpgradeSprint()) RefreshShop();
            });
        }

        private void AddUpgradeItem(Transform parent, string name, ref float y, int height, System.Action onBuy)
        {
            var root = new GameObject($"Upgrade_{name}");
            root.transform.SetParent(parent);
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1);
            rect.anchorMax = new Vector2(0.5f, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, y);
            rect.sizeDelta = new Vector2(620, height - 4);

            var bg = root.AddComponent<Image>();
            DeadlightUITheme.ApplyPanel(bg, new Color(0.14f, 0.12f, 0.2f, 0.9f));

            var label = CreateText(root.transform, "Label", name, 20, TextAnchor.MiddleLeft, Color.white,
                new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(15, 0), new Vector2(420, height)).GetComponent<Text>();
            _upgradeLabels.Add(label);

            var buyBtn = CreateButton(root.transform, "Buy", "UPGRADE", new Color(0.45f, 0.3f, 0.6f),
                new Vector2(1, 0.5f), new Vector2(110, 36), onBuy);
            var btnRect = buyBtn.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1, 0.5f);
            btnRect.anchorMax = new Vector2(1, 0.5f);
            btnRect.pivot = new Vector2(1, 0.5f);
            btnRect.anchoredPosition = new Vector2(-10, 0);

            _upgradeBuyButtons.Add(buyBtn.GetComponent<Button>());
            y -= height;
        }

        private void ShowShopTab(bool showWeapons)
        {
            if (_weaponsTabContent != null) _weaponsTabContent.SetActive(showWeapons);
            if (_upgradesTabContent != null) _upgradesTabContent.SetActive(!showWeapons);

            if (_weaponsTabBtn != null)
            {
                var c = _weaponsTabBtn.colors;
                c.normalColor = showWeapons ? new Color(0.3f, 0.5f, 0.7f) : new Color(0.2f, 0.2f, 0.3f);
                _weaponsTabBtn.colors = c;
            }
            if (_upgradesTabBtn != null)
            {
                var c = _upgradesTabBtn.colors;
                c.normalColor = !showWeapons ? new Color(0.5f, 0.35f, 0.65f) : new Color(0.2f, 0.2f, 0.3f);
                _upgradesTabBtn.colors = c;
            }
        }

        private void BuyWeapon(WeaponType weaponType, int cost, int unlockNight)
        {
            if (_purchasedWeapons.Contains(weaponType)) return;
            if (PointsSystem.Instance == null || !PointsSystem.Instance.CanAfford(cost)) return;
            int night = GameManager.Instance?.CurrentNight ?? 1;
            if (night < unlockNight) return;
            if (!PointsSystem.Instance.SpendPoints(cost, $"Weapon: {weaponType}")) return;

            _purchasedWeapons.Add(weaponType);

            WeaponData weapon = weaponType switch
            {
                WeaponType.Shotgun => WeaponData.CreateShotgun(),
                WeaponType.SMG => WeaponData.CreateSMG(),
                WeaponType.AssaultRifle => WeaponData.CreateAssaultRifle(),
                WeaponType.GrenadeLauncher => WeaponData.CreateGrenadeLauncher(),
                WeaponType.SniperRifle => WeaponData.CreateSniperRifle(),
                WeaponType.Flamethrower => WeaponData.CreateFlamethrower(),
                WeaponType.Railgun => WeaponData.CreateRailgun(),
                _ => null
            };

            if (weapon != null)
            {
                var player = GameObject.Find("Player");
                var shooting = player?.GetComponent<PlayerShooting>();
                shooting?.SetSecondWeapon(weapon);
            }

            RefreshShop();
        }

        private void BuyHealthKit()
        {
            if (PointsSystem.Instance == null || !PointsSystem.Instance.CanAfford(50)) return;
            if (!PointsSystem.Instance.SpendPoints(50, "Health Kit")) return;
            var player = GameObject.Find("Player");
            player?.GetComponent<PlayerHealth>()?.FullHeal();
            RefreshShop();
        }

        private void BuyAmmoRefill()
        {
            if (PointsSystem.Instance == null || !PointsSystem.Instance.CanAfford(30)) return;
            if (!PointsSystem.Instance.SpendPoints(30, "Ammo Refill")) return;
            var player = GameObject.Find("Player");
            player?.GetComponent<PlayerShooting>()?.AddAmmo(60);
            RefreshShop();
        }

        private void BuyArmor(ArmorTier tier, bool isHelmet, int cost)
        {
            if (PointsSystem.Instance == null || !PointsSystem.Instance.CanAfford(cost)) return;
            if (PlayerArmor.Instance == null) return;

            bool worthBuying = isHelmet
                ? (tier > PlayerArmor.Instance.HelmetTier || PlayerArmor.Instance.HelmetDurability <= 0)
                : (tier > PlayerArmor.Instance.VestTier || PlayerArmor.Instance.VestDurability <= 0);
            if (!worthBuying) return;

            if (!PointsSystem.Instance.SpendPoints(cost, isHelmet ? $"Lv{(int)tier} Helmet" : $"Lv{(int)tier} Vest")) return;

            if (isHelmet)
                PlayerArmor.Instance.EquipHelmet(tier);
            else
                PlayerArmor.Instance.EquipVest(tier);

            RefreshShop();
        }

        private void RefreshShop()
        {
            UpdateShopDisplay();
        }

        private void UpdateShopDisplay()
        {
            if (_shopPointsText != null && PointsSystem.Instance != null)
                _shopPointsText.text = $"Points: {PointsSystem.Instance.CurrentPoints}";

            if (_shopTitleText != null && GameManager.Instance != null)
                _shopTitleText.text = $"DAWN - Night {GameManager.Instance.CurrentNight} Survived!";

            if (_shopSummaryText != null && PointsSystem.Instance != null)
            {
                var stats = PointsSystem.Instance.GetGameStats();
                _shopSummaryText.text = $"Kills: {stats.enemiesKilled}  |  Points Earned: {stats.totalEarned}";
            }

            int night = GameManager.Instance?.CurrentNight ?? 1;

            // Supply buttons (indices 0,1)
            UpdateSupplyButton(0, 50);
            UpdateSupplyButton(1, 30);

            // Weapon buttons (indices 2..8)
            WeaponType[] weaponTypes = { WeaponType.Shotgun, WeaponType.SMG, WeaponType.SniperRifle, WeaponType.AssaultRifle, WeaponType.GrenadeLauncher, WeaponType.Flamethrower, WeaponType.Railgun };
            int[] weaponCosts = { 100, 150, 250, 200, 350, 400, 500 };
            int[] weaponNights = { 1, 2, 2, 3, 4, 4, 5 };
            for (int i = 0; i < weaponTypes.Length; i++)
            {
                int btnIdx = 2 + i;
                if (btnIdx >= _shopBuyButtons.Count) break;
                var btn = _shopBuyButtons[btnIdx];
                bool sold = _purchasedWeapons.Contains(weaponTypes[i]);
                bool canAfford = PointsSystem.Instance != null && PointsSystem.Instance.CanAfford(weaponCosts[i]);
                bool unlocked = night >= weaponNights[i];
                btn.interactable = !sold && canAfford && unlocked;

                var labelText = btn.GetComponentInChildren<Text>();
                if (labelText != null)
                    labelText.text = sold ? "SOLD" : "BUY";
            }

            // Upgrade buttons and labels
            var upgrades = PlayerUpgrades.Instance;
            if (upgrades != null)
            {
                UpdateUpgradeRow(0, upgrades.DamageTier, PlayerUpgrades.MaxDamageTier,
                    upgrades.GetDamageCost(), upgrades.GetDamageDescription(), "Damage");
                UpdateUpgradeRow(1, upgrades.FireRateTier, PlayerUpgrades.MaxFireRateTier,
                    upgrades.GetFireRateCost(), upgrades.GetFireRateDescription(), "Fire Rate");
                UpdateUpgradeRow(2, upgrades.MagazineTier, PlayerUpgrades.MaxMagazineTier,
                    upgrades.GetMagazineCost(), upgrades.GetMagazineDescription(), "Magazine");
                UpdateUpgradeRow(3, upgrades.HealthTier, PlayerUpgrades.MaxHealthTier,
                    upgrades.GetHealthCost(), upgrades.GetHealthDescription(), "Max Health");
                UpdateUpgradeRow(4, upgrades.SprintTier, PlayerUpgrades.MaxSprintTier,
                    upgrades.GetSprintCost(), upgrades.GetSprintDescription(), "Sprint Speed");
            }
        }

        private void UpdateSupplyButton(int index, int cost)
        {
            if (index >= _shopBuyButtons.Count) return;
            _shopBuyButtons[index].interactable = PointsSystem.Instance != null && PointsSystem.Instance.CanAfford(cost);
        }

        private void UpdateUpgradeRow(int index, int currentTier, int maxTier, int cost, string desc, string name)
        {
            if (index >= _upgradeLabels.Count || index >= _upgradeBuyButtons.Count) return;

            string pips = "";
            for (int i = 0; i < maxTier; i++)
                pips += i < currentTier ? "[X]" : "[ ]";

            bool maxed = currentTier >= maxTier;
            string costStr = maxed ? "" : $"  ({cost} pts)";
            _upgradeLabels[index].text = $"{name}  {pips}  {desc}{costStr}";

            _upgradeBuyButtons[index].interactable = !maxed && cost > 0 &&
                PointsSystem.Instance != null && PointsSystem.Instance.CanAfford(cost);

            var labelText = _upgradeBuyButtons[index].GetComponentInChildren<Text>();
            if (labelText != null) labelText.text = maxed ? "MAX" : "UPGRADE";
        }

        private void OnContinueToNextNight()
        {
            _dawnShopPanel?.SetActive(false);
            Time.timeScale = 1f;
            GameManager.Instance?.AdvanceToNextNight();
        }

        private void BuildGameOverScreen()
        {
            _gameOverPanel = CreatePanel(_canvasRoot.transform, "GameOverPanel");
            var bg = _gameOverPanel.GetComponent<Image>();
            DeadlightUITheme.ApplyScreenBackground(bg, new Color(0.03f, 0.04f, 0.06f, 0.94f));

            CreateCard(_gameOverPanel.transform, "GameOverCard",
                new Vector2(0.5f, 0.5f), new Vector2(620, 500), new Color(0.08f, 0.11f, 0.14f, 0.94f), new Color(0.86f, 0.26f, 0.24f, 0.5f));

            var title = CreateText(_gameOverPanel.transform, "Title",
                "YOU DIED", 56, TextAnchor.MiddleCenter, new Color(0.9f, 0.15f, 0.15f),
                new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(500, 80));
            title.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var statsText = CreateText(_gameOverPanel.transform, "Stats", "", 22, TextAnchor.UpperCenter, Color.white,
                new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(500, 200));
            statsText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

            CreateButton(_gameOverPanel.transform, "RestartButton", "Restart", new Color(0.3f, 0.6f, 0.3f),
                new Vector2(0.4f, 0.2f), new Vector2(180, 50), RestartGame);

            CreateButton(_gameOverPanel.transform, "MainMenuButton", "Main Menu", new Color(0.4f, 0.4f, 0.4f),
                new Vector2(0.6f, 0.2f), new Vector2(180, 50), GoToMainMenu);

            _gameOverPanel.AddComponent<GameOverPanelHelper>().Initialize(statsText.GetComponent<Text>());
        }

        private void BuildVictoryScreen()
        {
            _victoryPanel = CreatePanel(_canvasRoot.transform, "VictoryPanel");
            var bg = _victoryPanel.GetComponent<Image>();
            DeadlightUITheme.ApplyScreenBackground(bg, new Color(0.03f, 0.04f, 0.06f, 0.94f));

            CreateCard(_victoryPanel.transform, "VictoryCard",
                new Vector2(0.5f, 0.5f), new Vector2(620, 500), new Color(0.08f, 0.11f, 0.14f, 0.94f), new Color(0.95f, 0.78f, 0.28f, 0.5f));

            var title = CreateText(_victoryPanel.transform, "Title",
                "YOU SURVIVED!", 56, TextAnchor.MiddleCenter, new Color(0.9f, 0.75f, 0.2f),
                new Vector2(0.5f, 0.75f), new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(500, 80));
            title.GetComponent<Text>().fontStyle = FontStyle.Bold;

            var statsText = CreateText(_victoryPanel.transform, "Stats", "", 22, TextAnchor.UpperCenter, Color.white,
                new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(500, 200));
            statsText.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;

            CreateButton(_victoryPanel.transform, "RestartButton", "Restart", new Color(0.3f, 0.6f, 0.3f),
                new Vector2(0.4f, 0.2f), new Vector2(180, 50), RestartGame);

            CreateButton(_victoryPanel.transform, "MainMenuButton", "Main Menu", new Color(0.4f, 0.4f, 0.4f),
                new Vector2(0.6f, 0.2f), new Vector2(180, 50), GoToMainMenu);

            _victoryPanel.AddComponent<VictoryPanelHelper>().Initialize(statsText.GetComponent<Text>());
        }

        private void RestartGame()
        {
            Time.timeScale = 1f;
            HideAllPanels();
            _purchasedWeapons.Clear();
            PlayerUpgrades.Instance?.ResetUpgrades();
            GameManager.Instance?.RestartGame();
        }

        private void GoToMainMenu()
        {
            Time.timeScale = 1f;
            HideAllPanels();
            _purchasedWeapons.Clear();
            PlayerUpgrades.Instance?.ResetUpgrades();
            GameManager.Instance?.ReturnToMainMenu();
        }

        private void OnGameStateChanged(GameState newState)
        {
            HideAllPanels();

            switch (newState)
            {
                case GameState.MainMenu:
                    _mainMenuPanel?.SetActive(true);
                    break;
                case GameState.DayPhase:
                case GameState.NightPhase:
                case GameState.Transition:
                    break;
                case GameState.DawnPhase:
                    _dawnShopPanel?.SetActive(true);
                    Time.timeScale = 0f;
                    UpdateShopDisplay();
                    break;
                case GameState.GameOver:
                    HandleEndingState(false);
                    break;
                case GameState.Victory:
                    HandleEndingState(true);
                    break;
            }
        }

        private void OnEndingSequenceComplete()
        {
            if (!_waitingForEnding)
            {
                return;
            }

            if (EndingSequence.Instance != null)
                EndingSequence.Instance.OnEndingComplete -= OnEndingSequenceComplete;

            _waitingForEnding = false;

            if (GameManager.Instance == null) return;

            if (GameManager.Instance.CurrentState == GameState.Victory)
            {
                ShowVictoryPanel();
            }
            else if (GameManager.Instance.CurrentState == GameState.GameOver)
            {
                ShowGameOverPanel();
            }
        }

        private void HandleEndingState(bool victory)
        {
            LeaderboardManager.Instance?.SubmitRun(victory);

            if (TryQueueEndingSequence())
            {
                return;
            }

            if (victory)
            {
                ShowVictoryPanel();
            }
            else
            {
                ShowGameOverPanel();
            }
        }

        private bool TryQueueEndingSequence()
        {
            if (_waitingForEnding)
            {
                return true;
            }

            if (EndingSequence.Instance == null)
            {
                return false;
            }

            _waitingForEnding = true;
            EndingSequence.Instance.OnEndingComplete += OnEndingSequenceComplete;
            return true;
        }

        private void ShowGameOverPanel()
        {
            _gameOverPanel?.SetActive(true);
            Time.timeScale = 0f;
        }

        private void ShowVictoryPanel()
        {
            _victoryPanel?.SetActive(true);
            Time.timeScale = 0f;
        }

        private void HideAllPanels()
        {
            if (_mainMenuPanel != null) _mainMenuPanel.SetActive(false);
            if (_mapSelectPanel != null) _mapSelectPanel.SetActive(false);
            if (_pausePanel != null) _pausePanel.SetActive(false);
            if (_dawnShopPanel != null) _dawnShopPanel.SetActive(false);
            if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
            if (_victoryPanel != null) _victoryPanel.SetActive(false);
            if (_leaderboardPanel != null) _leaderboardPanel.SetActive(false);
        }

        // ===================== LEADERBOARD =====================

        private void BuildLeaderboardPanel()
        {
            _leaderboardPanel = CreatePanel(_canvasRoot.transform, "LeaderboardPanel");
            DeadlightUITheme.ApplyScreenBackground(_leaderboardPanel.GetComponent<Image>(), new Color(0.04f, 0.05f, 0.08f, 0.96f));

            CreateCard(_leaderboardPanel.transform, "LeaderboardCard",
                new Vector2(0.5f, 0.5f), new Vector2(980, 860), new Color(0.08f, 0.11f, 0.15f, 0.74f), new Color(0.92f, 0.76f, 0.3f, 0.46f));

            CreateText(_leaderboardPanel.transform, "Title",
                "LEADERBOARD", 40, TextAnchor.MiddleCenter, new Color(0.9f, 0.8f, 0.3f),
                new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.92f), Vector2.zero, new Vector2(500, 55));

            var headerText = "RANK    SCORE    NIGHTS    KILLS    DIFFICULTY    MAP";
            CreateText(_leaderboardPanel.transform, "Header", headerText, 16, TextAnchor.MiddleCenter,
                new Color(0.6f, 0.6f, 0.6f),
                new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.84f), Vector2.zero, new Vector2(800, 25));

            for (int i = 0; i < 10; i++)
            {
                float yPos = 0.78f - i * 0.065f;
                CreateText(_leaderboardPanel.transform, $"Entry_{i}", "", 17, TextAnchor.MiddleCenter, Color.white,
                    new Vector2(0.5f, yPos), new Vector2(0.5f, yPos), Vector2.zero, new Vector2(800, 28));
            }

            CreateButton(_leaderboardPanel.transform, "LBBackButton", "BACK", new Color(0.5f, 0.5f, 0.5f),
                new Vector2(0.5f, 0.06f), new Vector2(200, 45), HideLeaderboard);
        }

        private void ShowLeaderboard()
        {
            HideAllPanels();
            _leaderboardPanel?.SetActive(true);
            RefreshLeaderboardDisplay();
        }

        private void HideLeaderboard()
        {
            _leaderboardPanel?.SetActive(false);
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.MainMenu)
            {
                _mainMenuPanel?.SetActive(true);
            }
        }

        private void RefreshLeaderboardDisplay()
        {
            if (_leaderboardPanel == null) return;

            var entries = LeaderboardManager.Instance?.Entries;

            for (int i = 0; i < 10; i++)
            {
                var entryText = _leaderboardPanel.transform.Find($"Entry_{i}")?.GetComponent<Text>();
                if (entryText == null) continue;

                if (entries != null && i < entries.Count)
                {
                    var e = entries[i];
                    string victoryMark = e.victory ? " *" : "";
                    entryText.text = $"#{i + 1}      {e.score}      {e.nightsReached}      {e.kills}      {e.difficulty}      {e.map}{victoryMark}";
                    entryText.color = e.victory ? new Color(0.9f, 0.8f, 0.3f) : Color.white;
                }
                else
                {
                    entryText.text = $"#{i + 1}      ---";
                    entryText.color = new Color(0.4f, 0.4f, 0.4f);
                }
            }
        }

        private GameObject CreatePanel(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var img = obj.AddComponent<Image>();
            DeadlightUITheme.ApplyScreenBackground(img, DeadlightUITheme.ScreenTint);
            return obj;
        }

        private GameObject CreateText(Transform parent, string name, string text, int fontSize,
            TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            var txt = obj.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            DeadlightUITheme.ApplyText(txt, fontSize >= 20, color, fontSize >= 28);

            return obj;
        }

        private GameObject CreateButton(Transform parent, string name, string label, Color color,
            Vector2 anchor, Vector2 size, System.Action onClick)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            var img = obj.AddComponent<Image>();
            var btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;

            if (onClick != null)
                btn.onClick.AddListener(() => onClick());

            var labelObj = CreateText(obj.transform, "Label", label, 20, TextAnchor.MiddleCenter, Color.white,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            labelObj.GetComponent<Text>().fontStyle = FontStyle.Bold;
            DeadlightUITheme.ApplyButton(btn, img, labelObj.GetComponent<Text>(), color, color.grayscale < 0.4f);

            return obj;
        }

        private GameObject CreateCard(Transform parent, string name, Vector2 anchor, Vector2 size, Color fill, Color accent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            var image = obj.AddComponent<Image>();
            DeadlightUITheme.ApplyPanel(image, fill, true);
            return obj;
        }

        private void CreateEdgeTrim(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Color color)
        {
            var trim = new GameObject(name);
            trim.transform.SetParent(parent);
            var rect = trim.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            var image = trim.AddComponent<Image>();
            DeadlightUITheme.ApplyLine(image, color);
            image.raycastTarget = false;
        }
    }

    internal class GameOverPanelHelper : MonoBehaviour
    {
        private Text _statsText;

        public void Initialize(Text statsText)
        {
            _statsText = statsText;
        }

        private void OnEnable()
        {
            if (_statsText == null) return;

            int nightReached = Core.GameManager.Instance != null ? Core.GameManager.Instance.CurrentNight : 1;
            string difficulty = Core.GameManager.Instance != null ? Core.GameManager.Instance.CurrentDifficulty.ToString() : "Normal";
            string map = Core.GameManager.Instance != null ? Core.GameManager.Instance.SelectedMap.ToString() : "TownCenter";
            int kills = 0;
            int totalEarned = 0;

            if (PointsSystem.Instance != null)
            {
                var stats = PointsSystem.Instance.GetGameStats();
                kills = stats.enemiesKilled;
                totalEarned = stats.totalEarned;
            }

            int rank = -1;
            int finalScore = 0;
            if (LeaderboardManager.Instance != null && LeaderboardManager.Instance.Entries.Count > 0)
            {
                finalScore = LeaderboardManager.Instance.Entries[0].score;
                rank = 1;
                foreach (var entry in LeaderboardManager.Instance.Entries)
                {
                    if (entry.nightsReached == nightReached && entry.kills == kills)
                    {
                        finalScore = entry.score;
                        rank = LeaderboardManager.Instance.GetRank(entry.score);
                        break;
                    }
                }
            }

            _statsText.text = $"Night Reached: {nightReached}\n" +
                $"Enemies Killed: {kills}\n" +
                $"Points Earned: {totalEarned}\n" +
                $"Difficulty: {difficulty}\n" +
                $"Map: {map}\n" +
                (rank > 0 ? $"Leaderboard Rank: #{rank}  (Score: {finalScore})" : "");
        }
    }

    internal class VictoryPanelHelper : MonoBehaviour
    {
        private Text _statsText;

        public void Initialize(Text statsText)
        {
            _statsText = statsText;
        }

        private void OnEnable()
        {
            if (_statsText == null) return;

            string difficulty = Core.GameManager.Instance != null ? Core.GameManager.Instance.CurrentDifficulty.ToString() : "Normal";
            string map = Core.GameManager.Instance != null ? Core.GameManager.Instance.SelectedMap.ToString() : "TownCenter";
            int kills = 0;
            int totalEarned = 0;
            int nightsSurvived = 5;

            if (PointsSystem.Instance != null)
            {
                var stats = PointsSystem.Instance.GetGameStats();
                kills = stats.enemiesKilled;
                totalEarned = stats.totalEarned;
                nightsSurvived = stats.nightsSurvived;
            }

            int rank = -1;
            int finalScore = 0;
            if (LeaderboardManager.Instance != null && LeaderboardManager.Instance.Entries.Count > 0)
            {
                var latestEntry = LeaderboardManager.Instance.Entries[0];
                for (int i = 0; i < LeaderboardManager.Instance.Entries.Count; i++)
                {
                    if (LeaderboardManager.Instance.Entries[i].victory)
                    {
                        latestEntry = LeaderboardManager.Instance.Entries[i];
                        break;
                    }
                }
                finalScore = latestEntry.score;
                rank = LeaderboardManager.Instance.GetRank(finalScore);
            }

            _statsText.text = $"ALL {nightsSurvived} NIGHTS SURVIVED!\n" +
                $"Enemies Killed: {kills}\n" +
                $"Points Earned: {totalEarned}\n" +
                $"Difficulty: {difficulty} ({GetMultiplierText(difficulty)})\n" +
                $"Map: {map}\n" +
                (rank > 0 ? $"Leaderboard Rank: #{rank}  (Score: {finalScore})" : "");
        }

        private string GetMultiplierText(string difficulty)
        {
            return difficulty switch
            {
                "Easy" => "0.75x",
                "Normal" => "1.0x",
                "Hard" => "1.5x",
                _ => "1.0x"
            };
        }
    }
}
