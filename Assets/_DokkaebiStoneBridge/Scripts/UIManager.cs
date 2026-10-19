using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace DokkaebiStoneBridge
{
    // 타이틀 화면 / 스테이지 선택 / HUD / 일시정지 / 설정을 담당하는 UI 매니저.
    // 모든 UI 오브젝트는 Awake 시점에 코드로 생성한다.
    //
    // 타이틀/일시정지는 참고 이미지("도깨비 홀씨" 목업)에서 잘라낸 완성된 아트(배경+버튼
    // 글자까지 포함)를 그대로 배경 이미지로 깔고, 그 위에 투명한 Button 히트존만 정확한
    // 위치에 올리는 방식으로 만든다 (별도 버튼 스프라이트/라벨 재구성 없이 픽셀 단위로
    // 원본 디자인을 그대로 재현하기 위함).
    //
    // 발각 플래시 / 스테이지 클리어 팝업은 사용자 요청으로 사용하지 않는다(2026-10-19).
    public class UIManager : MonoBehaviour
    {
        [Header("타이틀 (레거시 - 현재는 타이틀 배경 이미지에 로고가 포함되어 있어 사용되지 않음)")]
        public string gameTitle = "도깨비 홀씨";
        public string subtitle = "씨앗을 모아 세계수를 되살리자";

        [Header("귀여운 컬러 팔레트 (스테이지 선택 / 설정 패널 등 레거시 UI에서 사용)")]
        public Color scrimColor = new Color(0.17f, 0.12f, 0.24f, 0.82f);
        public Color cardColor = new Color(1f, 0.96f, 0.88f, 1f);
        public Color cardShadowColor = new Color(0.25f, 0.15f, 0.1f, 0.22f);
        public Color textColor = new Color(0.36f, 0.24f, 0.22f);
        public Color titleColor = new Color(0.93f, 0.53f, 0.22f);
        public Color primaryButtonColor = new Color(1f, 0.64f, 0.52f);
        public Color secondaryButtonColor = new Color(0.62f, 0.85f, 0.75f);
        public Color dangerButtonColor = new Color(0.86f, 0.5f, 0.46f);
        public Color accentGold = new Color(1f, 0.78f, 0.34f);
        public Color lockedColor = new Color(0.62f, 0.6f, 0.58f);

        [Header("디버그")]
        [Tooltip("켜면 이미지 위에 올라간 투명 버튼 히트존이 반투명하게 보여서 위치를 눈으로 확인할 수 있다.")]
        public bool debugShowHitZones = false;

        private Canvas _canvas;
        private GameObject _titlePanel;
        private GameObject _stageSelectPanel;
        private GameObject _hudPanel;
        private GameObject _pausePanel;
        private GameObject _settingsPanel;
        private Text _seedText;
        private Slider _volumeSlider;

        private Button _continueButton;
        private CanvasGroup _continueCanvasGroup;

        private Sprite _titleBgSprite;
        private Sprite _pauseBgSprite;
        private Sprite _seedIconSprite;
        private Sprite _pauseIconSprite;

        // 2026-10-19 2차 UI 아트 라운드에서 추가된 파츠 분리 에셋들
        // (로고/배경/버튼 4종×2상태/HUD 아이콘/일시정지 아이콘 2상태/조작법 아이콘 4종/대화창 배경 2종).
        private Sprite _logoSprite;
        private Sprite _bgSceneSprite;
        private Sprite _btnStartN, _btnStartP;
        private Sprite _btnContinueN, _btnContinueP;
        private Sprite _btnSettingsN, _btnSettingsP;
        private Sprite _btnQuitN, _btnQuitP;
        private Sprite _pauseIconN, _pauseIconP;
        private Sprite _keyWasdSprite, _keyESprite, _keyMouseSprite, _keyEscSprite;
        private Sprite _dialogueBoxDefaultSprite, _dialogueBoxPortraitSprite;

        public static UIManager Instance { get; private set; }

        private GameObject _controlHintsPanel;
        private bool _controlHintsShownThisStage;

        private GameObject _dialoguePanel;
        private Image _dialogueBoxImage;
        private Text _dialogueText;
        private CanvasGroup _dialogueCanvasGroup;
        public bool DialogueAdvanceRequested { get; private set; }

        private readonly Button[] _stageButtons = new Button[StageProgress.StageCount];
        private readonly Image[] _stageButtonImages = new Image[StageProgress.StageCount];
        private readonly Text[] _stageButtonLabels = new Text[StageProgress.StageCount];

        private const string VolumePrefKey = "DSB_MasterVolume";

        private void Awake()
        {
            Instance = this;
            EnsureEventSystem();
            LoadSprites();
            BuildCanvas();
            BuildTitlePanel();
            BuildStageSelectPanel();
            BuildHud();
            BuildControlHints();
            BuildDialoguePanel();
            BuildPausePanel();
            BuildSettingsPanel();

            float savedVolume = PlayerPrefs.GetFloat(VolumePrefKey, 1f);
            AudioListener.volume = savedVolume;
            if (_volumeSlider != null) _volumeSlider.SetValueWithoutNotify(savedVolume);
        }

        private void LoadSprites()
        {
            _titleBgSprite = Resources.Load<Sprite>("UI/title_bg");
            _pauseBgSprite = Resources.Load<Sprite>("UI/panel_pause");
            _seedIconSprite = Resources.Load<Sprite>("UI/icon_seed_v2") ?? Resources.Load<Sprite>("UI/icon_seed");
            _pauseIconSprite = Resources.Load<Sprite>("UI/icon_pause");

            _logoSprite = Resources.Load<Sprite>("UI/title_logo");
            _bgSceneSprite = Resources.Load<Sprite>("UI/bg_worldtree_scene");
            _btnStartN = Resources.Load<Sprite>("UI/btn_title_start_n");
            _btnStartP = Resources.Load<Sprite>("UI/btn_title_start_p");
            _btnContinueN = Resources.Load<Sprite>("UI/btn_title_continue_n");
            _btnContinueP = Resources.Load<Sprite>("UI/btn_title_continue_p");
            _btnSettingsN = Resources.Load<Sprite>("UI/btn_title_settings_n");
            _btnSettingsP = Resources.Load<Sprite>("UI/btn_title_settings_p");
            _btnQuitN = Resources.Load<Sprite>("UI/btn_title_quit_n");
            _btnQuitP = Resources.Load<Sprite>("UI/btn_title_quit_p");
            _pauseIconN = Resources.Load<Sprite>("UI/icon_pause_n");
            _pauseIconP = Resources.Load<Sprite>("UI/icon_pause_p");
            _keyWasdSprite = Resources.Load<Sprite>("UI/icon_key_wasd");
            _keyESprite = Resources.Load<Sprite>("UI/icon_key_e");
            _keyMouseSprite = Resources.Load<Sprite>("UI/icon_key_mouse");
            _keyEscSprite = Resources.Load<Sprite>("UI/icon_key_esc");
            _dialogueBoxDefaultSprite = Resources.Load<Sprite>("UI/dialogue_box_default");
            _dialogueBoxPortraitSprite = Resources.Load<Sprite>("UI/dialogue_box_portrait");
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged += HandleStateChanged;
                GameManager.Instance.OnSeedCountChanged += HandleSeedCountChanged;
                HandleStateChanged(GameManager.Instance.State);
                HandleSeedCountChanged(GameManager.Instance.CollectedFragments, GameManager.Instance.totalFragments);
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
                GameManager.Instance.OnSeedCountChanged -= HandleSeedCountChanged;
            }
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<UnityEngine.EventSystems.EventSystem>();
            go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        private void BuildCanvas()
        {
            var canvasGO = new GameObject("UI_Canvas");
            canvasGO.transform.SetParent(transform, false);
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        private RectTransform MakeFullRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private RectTransform MakeScrim(string name, Transform parent, Color color)
        {
            var rt = MakeFullRect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        private RectTransform MakeCard(string name, Transform parent, Vector2 size, Color color)
        {
            var shadowGO = new GameObject(name + "_Shadow", typeof(RectTransform));
            shadowGO.transform.SetParent(parent, false);
            var shadowRt = shadowGO.GetComponent<RectTransform>();
            shadowRt.anchorMin = new Vector2(0.5f, 0.5f);
            shadowRt.anchorMax = new Vector2(0.5f, 0.5f);
            shadowRt.sizeDelta = size;
            shadowRt.anchoredPosition = new Vector2(0, -10);
            var shadowImg = shadowGO.AddComponent<Image>();
            shadowImg.sprite = UIStyleKit.RoundedRect;
            shadowImg.type = Image.Type.Sliced;
            shadowImg.color = cardShadowColor;

            var cardGO = new GameObject(name, typeof(RectTransform));
            cardGO.transform.SetParent(parent, false);
            var cardRt = cardGO.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = size;
            cardRt.anchoredPosition = Vector2.zero;
            var cardImg = cardGO.AddComponent<Image>();
            cardImg.sprite = UIStyleKit.RoundedRect;
            cardImg.type = Image.Type.Sliced;
            cardImg.color = color;
            cardGO.AddComponent<UIPopIn>();

            return cardRt;
        }

        // 참고 이미지에서 그대로 잘라온 완성된 아트(배경+버튼+글자)를 보여주는 카드.
        // 카드 크기를 스프라이트의 원본 가로세로 비율과 똑같이 맞춰서, 그 위에 올리는
        // 투명 버튼 히트존 좌표가 이미지 비율과 정확히 일치하도록 만든다.
        private RectTransform MakeArtCard(string name, Transform parent, Sprite sprite, float height)
        {
            float aspect = sprite != null ? (float)sprite.texture.width / sprite.texture.height : 1f;
            var size = new Vector2(height * aspect, height);

            var cardGO = new GameObject(name, typeof(RectTransform));
            cardGO.transform.SetParent(parent, false);
            var rt = cardGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;

            var img = cardGO.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = false;
            cardGO.AddComponent<UIPopIn>();

            return rt;
        }

        private Text MakeText(string name, Transform parent, string content, int fontSize, Color color, TextAnchor anchor, bool bold = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = anchor;
            if (bold) text.fontStyle = FontStyle.Bold;
            return text;
        }

        private void AddShadow(Text t)
        {
            var shadow = t.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.2f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private Button MakeButton(string name, Transform parent, string label, Vector2 anchoredPos, Vector2 size, Color color, System.Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;

            var img = go.AddComponent<Image>();
            img.sprite = UIStyleKit.RoundedRect;
            img.type = Image.Type.Sliced;
            img.color = color;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = color;
            colors.highlightedColor = UIStyleKit.Lighten(color, 0.15f);
            colors.pressedColor = UIStyleKit.Darken(color, 0.12f);
            colors.selectedColor = color;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick());
            go.AddComponent<UIButtonPunch>();

            var labelText = MakeText(name + "_Label", go.transform, label, 30, Color.white, TextAnchor.MiddleCenter, true);
            var labelRt = labelText.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            AddShadow(labelText);

            return btn;
        }

        // 완성된 아트 위에 올리는 '보이지 않는' 버튼. ax/ay는 parent 기준 0~1 anchor 좌표
        // (0,0)=왼쪽 아래, (1,1)=오른쪽 위), w/h는 parent의 로컬 픽셀 크기 기준 히트존 크기.
        private Button MakeInvisibleButton(string name, Transform parent, float ax, float ay, float w, float h, System.Action onClick)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(ax, ay);
            rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);

            var img = go.AddComponent<Image>();
            img.color = debugShowHitZones ? new Color(1f, 0.1f, 0.6f, 0.35f) : new Color(1f, 1f, 1f, 0f);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());
            return btn;
        }

        // ---------------------------------------------------------------
        // 타이틀 화면 (2026-10-19 2차 UI 라운드: 배경/로고/버튼이 파츠로 분리된
        // 새 아트로 교체. 기존의 "완성 배경 위에 투명 버튼" 방식 대신, 실제 버튼
        // 이미지를 깔고 Unity Button의 SpriteSwap으로 기본/눌림 상태를 그대로 보여준다.)
        // ---------------------------------------------------------------
        private void BuildTitlePanel()
        {
            var rt = MakeFullRect("TitlePanel", _canvas.transform);
            _titlePanel = rt.gameObject;

            bool haveNewArt = _bgSceneSprite != null && _logoSprite != null && _btnStartN != null;

            if (!haveNewArt)
            {
                // 새 파츠 에셋이 없으면(구버전 폴백) 기존 1-5 방식의 합성 배경을 그대로 사용.
                var bgGO = new GameObject("TitleBg", typeof(RectTransform));
                bgGO.transform.SetParent(rt, false);
                var bgRt = bgGO.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
                var bgImg = bgGO.AddComponent<Image>();
                bgImg.sprite = _titleBgSprite;
                var fitter = bgGO.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fitter.aspectRatio = _titleBgSprite != null ? (float)_titleBgSprite.texture.width / _titleBgSprite.texture.height : 1.5f;

                MakeInvisibleButton("StartHitZone", rt, 0.466f, 0.391f, 424f, 125f, () =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.BeginStage(1);
                });
                var legacyContinue = MakeInvisibleButton("ContinueHitZone", rt, 0.460f, 0.248f, 350f, 100f, () =>
                {
                    if (GameManager.Instance != null && StageProgress.HasAnyProgress()) GameManager.Instance.OpenStageSelect();
                });
                _continueButton = legacyContinue;
                MakeInvisibleButton("SettingsHitZone", rt, 0.460f, 0.138f, 350f, 97f, OpenSettings);
                MakeInvisibleButton("QuitHitZone", rt, 0.460f, 0.070f, 350f, 92f, () =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.QuitGame();
                });
                return;
            }

            // 배경(세계수 섬 풍경) - 화면 전체를 꽉 채운다.
            var sceneGO = new GameObject("TitleBgScene", typeof(RectTransform));
            sceneGO.transform.SetParent(rt, false);
            var sceneRt = sceneGO.GetComponent<RectTransform>();
            sceneRt.anchorMin = Vector2.zero;
            sceneRt.anchorMax = Vector2.one;
            sceneRt.offsetMin = Vector2.zero;
            sceneRt.offsetMax = Vector2.zero;
            var sceneImg = sceneGO.AddComponent<Image>();
            sceneImg.sprite = _bgSceneSprite;
            var sceneFitter = sceneGO.AddComponent<AspectRatioFitter>();
            sceneFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            sceneFitter.aspectRatio = (float)_bgSceneSprite.texture.width / _bgSceneSprite.texture.height;

            // 반투명 스크림을 배경 위에 살짝 깔아서 오른쪽 버튼 영역의 글자 가독성을 보장.
            var scrimGO = new GameObject("TitleScrim", typeof(RectTransform));
            scrimGO.transform.SetParent(rt, false);
            var scrimRt = scrimGO.GetComponent<RectTransform>();
            scrimRt.anchorMin = new Vector2(0.55f, 0f);
            scrimRt.anchorMax = new Vector2(1f, 1f);
            scrimRt.offsetMin = Vector2.zero;
            scrimRt.offsetMax = Vector2.zero;
            var scrimImg = scrimGO.AddComponent<Image>();
            scrimImg.color = new Color(0.98f, 0.93f, 0.8f, 0.28f);

            // 로고 (왼쪽 위)
            float logoAspect = (float)_logoSprite.texture.width / _logoSprite.texture.height;
            float logoHeight = 260f;
            var logoGO = new GameObject("TitleLogo", typeof(RectTransform));
            logoGO.transform.SetParent(rt, false);
            var logoRt = logoGO.GetComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0f, 1f);
            logoRt.anchorMax = new Vector2(0f, 1f);
            logoRt.pivot = new Vector2(0f, 1f);
            logoRt.anchoredPosition = new Vector2(50f, -40f);
            logoRt.sizeDelta = new Vector2(logoHeight * logoAspect, logoHeight);
            var logoImg = logoGO.AddComponent<Image>();
            logoImg.sprite = _logoSprite;
            logoImg.preserveAspect = true;
            logoGO.AddComponent<UIPopIn>();

            // 버튼 4개 - 화면 오른쪽에 세로로 쌓는다.
            float btnHeight = 96f;
            float[] ys = { 185f, 60f, -65f, -190f };

            MakeStateButton("StartButton", rt, _btnStartN, _btnStartP, new Vector2(0.78f, 0.5f), new Vector2(0f, ys[0]), btnHeight, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.BeginStage(1);
            });

            var continueBtn = MakeStateButton("ContinueButton", rt, _btnContinueN, _btnContinueP, new Vector2(0.78f, 0.5f), new Vector2(0f, ys[1]), btnHeight, () =>
            {
                if (GameManager.Instance != null && StageProgress.HasAnyProgress()) GameManager.Instance.OpenStageSelect();
            });
            _continueButton = continueBtn;
            _continueCanvasGroup = continueBtn.gameObject.AddComponent<CanvasGroup>();

            MakeStateButton("SettingsButton", rt, _btnSettingsN, _btnSettingsP, new Vector2(0.78f, 0.5f), new Vector2(0f, ys[2]), btnHeight, OpenSettings);

            MakeStateButton("QuitButton", rt, _btnQuitN, _btnQuitP, new Vector2(0.78f, 0.5f), new Vector2(0f, ys[3]), btnHeight, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.QuitGame();
            });
        }

        // 기본/눌림 두 스프라이트를 Unity Button의 SpriteSwap 전환으로 그대로 보여주는 버튼.
        // anchor는 parent 기준 0~1 좌표, anchoredPos는 그 anchor 지점에서의 픽셀 오프셋,
        // height는 버튼 높이(px)이고 width는 스프라이트의 원본 가로세로 비율로 자동 계산한다.
        private Button MakeStateButton(string name, Transform parent, Sprite normalSprite, Sprite pressedSprite, Vector2 anchor, Vector2 anchoredPos, float height, System.Action onClick)
        {
            float aspect = normalSprite != null ? (float)normalSprite.texture.width / normalSprite.texture.height : 3.5f;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(height * aspect, height);

            var img = go.AddComponent<Image>();
            img.sprite = normalSprite;
            img.preserveAspect = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.SpriteSwap;
            var spriteState = btn.spriteState;
            spriteState.highlightedSprite = pressedSprite;
            spriteState.pressedSprite = pressedSprite;
            spriteState.selectedSprite = normalSprite;
            spriteState.disabledSprite = normalSprite;
            btn.spriteState = spriteState;
            btn.onClick.AddListener(() => onClick());
            go.AddComponent<UIButtonPunch>();

            return btn;
        }

        private void RefreshTitleButtons()
        {
            bool hasProgress = StageProgress.HasAnyProgress();
            if (_continueButton != null) _continueButton.interactable = hasProgress;
            if (_continueCanvasGroup != null)
            {
                _continueCanvasGroup.alpha = hasProgress ? 1f : 0.55f;
            }
        }

        // ---------------------------------------------------------------
        // 스테이지 선택 (기존 디자인 유지 - 참고 이미지에 해당 화면이 없음)
        // ---------------------------------------------------------------
        private void BuildStageSelectPanel()
        {
            var scrim = MakeScrim("StageSelectPanel", _canvas.transform, scrimColor);
            _stageSelectPanel = scrim.gameObject;

            int count = StageProgress.StageCount;
            int columns = Mathf.Min(count, 5);
            int rows = Mathf.CeilToInt(count / (float)columns);

            float cardWidth = Mathf.Max(920f, columns * 150f + 40f);
            float cardHeight = Mathf.Max(440f, 300f + rows * 120f);
            var card = MakeCard("StageSelectCard", scrim, new Vector2(cardWidth, cardHeight), cardColor);

            var title = MakeText("StageSelectTitle", card, "스테이지 선택", 38, titleColor, TextAnchor.MiddleCenter, true);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(600, 70);
            titleRt.anchoredPosition = new Vector2(0, cardHeight * 0.5f - 60f);
            AddShadow(title);

            float slotSize = 110f;
            float spacing = 18f;
            float rowSpacing = 18f;
            float totalWidth = columns * slotSize + (columns - 1) * spacing;
            float startX = -totalWidth / 2f + slotSize / 2f;
            float totalHeight = rows * slotSize + (rows - 1) * rowSpacing;
            float startY = totalHeight / 2f - slotSize / 2f;

            for (int i = 0; i < count; i++)
            {
                int stageNum = i + 1;
                int col = i % columns;
                int row = i / columns;
                float x = startX + col * (slotSize + spacing);
                float y = startY - row * (slotSize + rowSpacing) + 20f;

                var go = new GameObject("StageButton_" + stageNum, typeof(RectTransform));
                go.transform.SetParent(card, false);
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(slotSize, slotSize);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(x, y);

                var img = go.AddComponent<Image>();
                img.sprite = UIStyleKit.RoundedRect;
                img.type = Image.Type.Sliced;
                img.color = primaryButtonColor;

                var btn = go.AddComponent<Button>();
                btn.targetGraphic = img;
                int capturedStage = stageNum;
                btn.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null) GameManager.Instance.SelectStage(capturedStage);
                });
                go.AddComponent<UIButtonPunch>();

                var label = MakeText("StageButton_" + stageNum + "_Label", go.transform, stageNum.ToString(), 32, Color.white, TextAnchor.MiddleCenter, true);
                var labelRt = label.rectTransform;
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = Vector2.zero;
                labelRt.offsetMax = Vector2.zero;
                AddShadow(label);

                _stageButtons[i] = btn;
                _stageButtonImages[i] = img;
                _stageButtonLabels[i] = label;
            }

            MakeButton("StageSelectBackButton", card, "뒤로가기", new Vector2(0, -cardHeight * 0.5f + 55f), new Vector2(300, 68), secondaryButtonColor, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.BackToTitleFromStageSelect();
            });
        }

        private void RefreshStageButtons()
        {
            for (int i = 0; i < StageProgress.StageCount; i++)
            {
                int stageNum = i + 1;
                bool unlocked = StageProgress.IsUnlocked(stageNum);
                bool cleared = StageProgress.IsCleared(stageNum);
                Color color = !unlocked ? lockedColor : (cleared ? accentGold : primaryButtonColor);

                if (_stageButtonImages[i] != null) _stageButtonImages[i].color = color;

                if (_stageButtons[i] != null)
                {
                    _stageButtons[i].interactable = unlocked;
                    var colors = _stageButtons[i].colors;
                    colors.normalColor = color;
                    colors.highlightedColor = UIStyleKit.Lighten(color, 0.15f);
                    colors.pressedColor = UIStyleKit.Darken(color, 0.12f);
                    colors.disabledColor = color;
                    colors.fadeDuration = 0.08f;
                    _stageButtons[i].colors = colors;
                }

                if (_stageButtonLabels[i] != null) _stageButtonLabels[i].text = unlocked ? stageNum.ToString() : "잠김";
            }
        }

        // ---------------------------------------------------------------
        // HUD (씨앗 카운터 + 일시정지 버튼)
        // ---------------------------------------------------------------
        private void BuildHud()
        {
            var rt = MakeFullRect("HudPanel", _canvas.transform);
            _hudPanel = rt.gameObject;

            var badgeGO = new GameObject("SeedBadge", typeof(RectTransform));
            badgeGO.transform.SetParent(rt, false);
            var badgeRt = badgeGO.GetComponent<RectTransform>();
            badgeRt.anchorMin = new Vector2(0f, 1f);
            badgeRt.anchorMax = new Vector2(0f, 1f);
            badgeRt.pivot = new Vector2(0f, 1f);
            badgeRt.anchoredPosition = new Vector2(30, -30);
            badgeRt.sizeDelta = new Vector2(260, 68);
            var badgeImg = badgeGO.AddComponent<Image>();
            badgeImg.sprite = UIStyleKit.RoundedRect;
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = cardColor;

            var iconGO = new GameObject("SeedIcon", typeof(RectTransform));
            iconGO.transform.SetParent(badgeGO.transform, false);
            var iconRt = iconGO.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(10, 0);
            iconRt.sizeDelta = new Vector2(52, 52);
            var iconImg = iconGO.AddComponent<Image>();
            if (_seedIconSprite != null)
            {
                iconImg.sprite = _seedIconSprite;
                iconImg.preserveAspect = true;
            }
            else
            {
                iconImg.sprite = UIStyleKit.Circle;
                iconImg.color = accentGold;
            }

            _seedText = MakeText("SeedText", badgeGO.transform, "씨앗 0 / 0", 26, textColor, TextAnchor.MiddleLeft, true);
            var seedRt = _seedText.rectTransform;
            seedRt.anchorMin = new Vector2(0f, 0f);
            seedRt.anchorMax = new Vector2(1f, 1f);
            seedRt.offsetMin = new Vector2(68, 0);
            seedRt.offsetMax = new Vector2(-14, 0);

            var pauseBtnGO = new GameObject("PauseButton", typeof(RectTransform));
            pauseBtnGO.transform.SetParent(rt, false);
            var pauseBtnRt = pauseBtnGO.GetComponent<RectTransform>();
            pauseBtnRt.anchorMin = new Vector2(1f, 1f);
            pauseBtnRt.anchorMax = new Vector2(1f, 1f);
            pauseBtnRt.pivot = new Vector2(1f, 1f);
            pauseBtnRt.anchoredPosition = new Vector2(-30, -30);
            pauseBtnRt.sizeDelta = new Vector2(76, 74);
            var pauseBtnImg = pauseBtnGO.AddComponent<Image>();
            Sprite pauseNormal = _pauseIconN != null ? _pauseIconN : _pauseIconSprite;
            if (pauseNormal != null)
            {
                pauseBtnImg.sprite = pauseNormal;
                pauseBtnImg.preserveAspect = true;
            }
            var pauseBtn = pauseBtnGO.AddComponent<Button>();
            pauseBtn.targetGraphic = pauseBtnImg;
            if (_pauseIconP != null)
            {
                pauseBtn.transition = Selectable.Transition.SpriteSwap;
                var pst = pauseBtn.spriteState;
                pst.highlightedSprite = _pauseIconP;
                pst.pressedSprite = _pauseIconP;
                pauseBtn.spriteState = pst;
            }
            pauseBtn.onClick.AddListener(() =>
            {
                if (GameManager.Instance != null) GameManager.Instance.TogglePause();
            });
            pauseBtnGO.AddComponent<UIButtonPunch>();
        }

        // ---------------------------------------------------------------
        // 조작법 안내 아이콘 (2026-10-19 2차 UI 라운드 신규) - 1스테이지(튜토리얼) 시작 시에만
        // 화면 하단에 WASD/E/마우스/ESC 4종 아이콘을 잠깐 보여주고 자동으로 사라진다.
        // ---------------------------------------------------------------
        private void BuildControlHints()
        {
            if (_keyWasdSprite == null && _keyESprite == null && _keyMouseSprite == null && _keyEscSprite == null) return;

            var go = new GameObject("ControlHintsPanel", typeof(RectTransform));
            go.transform.SetParent(_canvas.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 40f);
            rt.sizeDelta = new Vector2(900f, 150f);
            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            _controlHintsPanel = go;

            var entries = new (Sprite icon, string label)[]
            {
                (_keyWasdSprite, "이동"),
                (_keyESprite, "방향 조절 / 상호작용"),
                (_keyMouseSprite, "바닥 상호작용"),
                (_keyEscSprite, "메뉴 열기"),
            };

            float slot = 210f;
            float startX = -(entries.Length - 1) * slot * 0.5f;

            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].icon == null) continue;
                float x = startX + i * slot;

                var iconGO = new GameObject("HintIcon_" + i, typeof(RectTransform));
                iconGO.transform.SetParent(go.transform, false);
                var iconRt = iconGO.GetComponent<RectTransform>();
                iconRt.anchorMin = new Vector2(0.5f, 1f);
                iconRt.anchorMax = new Vector2(0.5f, 1f);
                iconRt.pivot = new Vector2(0.5f, 1f);
                iconRt.anchoredPosition = new Vector2(x, 0f);
                iconRt.sizeDelta = new Vector2(96f, 96f);
                var iconImg = iconGO.AddComponent<Image>();
                iconImg.sprite = entries[i].icon;
                iconImg.preserveAspect = true;

                var label = MakeText("HintLabel_" + i, go.transform, entries[i].label, 20, textColor, TextAnchor.MiddleCenter, true);
                var labelRt = label.rectTransform;
                labelRt.anchorMin = new Vector2(0.5f, 1f);
                labelRt.anchorMax = new Vector2(0.5f, 1f);
                labelRt.pivot = new Vector2(0.5f, 1f);
                labelRt.anchoredPosition = new Vector2(x, -102f);
                labelRt.sizeDelta = new Vector2(slot - 10f, 36f);
                AddShadow(label);
            }

            go.SetActive(false);
        }

        private Coroutine _controlHintsRoutine;

        private void ShowControlHintsForStage1()
        {
            if (_controlHintsPanel == null || _controlHintsShownThisStage) return;
            _controlHintsShownThisStage = true;
            if (_controlHintsRoutine != null) StopCoroutine(_controlHintsRoutine);
            _controlHintsRoutine = StartCoroutine(ControlHintsRoutine());
        }

        private IEnumerator ControlHintsRoutine()
        {
            _controlHintsPanel.SetActive(true);
            var cg = _controlHintsPanel.GetComponent<CanvasGroup>();
            float t = 0f;
            const float fadeIn = 0.3f, hold = 5.5f, fadeOut = 0.6f;
            while (t < fadeIn) { t += Time.unscaledDeltaTime; cg.alpha = Mathf.Clamp01(t / fadeIn); yield return null; }
            cg.alpha = 1f;
            yield return new WaitForSecondsRealtime(hold);
            t = 0f;
            while (t < fadeOut) { t += Time.unscaledDeltaTime; cg.alpha = 1f - Mathf.Clamp01(t / fadeOut); yield return null; }
            cg.alpha = 0f;
            _controlHintsPanel.SetActive(false);
        }

        // ---------------------------------------------------------------
        // 대화창 (2026-10-19 2차 UI 라운드 신규) - SeedDialogueTrigger가 호출하는
        // 화면 하단 고정 대화 박스. 초상화 유무에 따라 배경 스프라이트만 바꿔 끼운다
        // (초상화 자체는 배경 아트에 이미 그려져 있음 - 8스테이지 조우 캐릭터 전용).
        // 우측 하단 삼각형을 클릭하면 그 줄을 바로 넘길 수 있다(DialogueAdvanceRequested).
        // ---------------------------------------------------------------
        private void BuildDialoguePanel()
        {
            if (_dialogueBoxDefaultSprite == null && _dialogueBoxPortraitSprite == null) return;

            var go = new GameObject("DialoguePanel", typeof(RectTransform));
            go.transform.SetParent(_canvas.transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 60f);
            float boxHeight = 170f;
            Sprite baseSprite = _dialogueBoxDefaultSprite != null ? _dialogueBoxDefaultSprite : _dialogueBoxPortraitSprite;
            float aspect = (float)baseSprite.texture.width / baseSprite.texture.height;
            rt.sizeDelta = new Vector2(boxHeight * aspect, boxHeight);
            _dialogueCanvasGroup = go.AddComponent<CanvasGroup>();
            _dialoguePanel = go;

            _dialogueBoxImage = go.AddComponent<Image>();
            _dialogueBoxImage.sprite = baseSprite;
            _dialogueBoxImage.preserveAspect = true;

            _dialogueText = MakeText("DialogueText", go.transform, "", 26, textColor, TextAnchor.MiddleLeft, true);
            var textRt = _dialogueText.rectTransform;
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 1f);
            // 초상화가 있을 때는 왼쪽에 공간을 더 비워둔다(배경에 이미 초상화가 그려져 있으므로).
            textRt.offsetMin = new Vector2(boxHeight * aspect * 0.26f, 20f);
            textRt.offsetMax = new Vector2(-70f, -34f);
            _dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _dialogueText.verticalOverflow = VerticalWrapMode.Truncate;

            var skipBtnGO = new GameObject("DialogueAdvanceHitZone", typeof(RectTransform));
            skipBtnGO.transform.SetParent(go.transform, false);
            var skipRt = skipBtnGO.GetComponent<RectTransform>();
            skipRt.anchorMin = new Vector2(1f, 0f);
            skipRt.anchorMax = new Vector2(1f, 1f);
            skipRt.pivot = new Vector2(1f, 0.5f);
            skipRt.anchoredPosition = new Vector2(-10f, 0f);
            skipRt.sizeDelta = new Vector2(90f, 0f);
            var skipImg = skipBtnGO.AddComponent<Image>();
            skipImg.color = debugShowHitZones ? new Color(1f, 0.1f, 0.6f, 0.35f) : new Color(1f, 1f, 1f, 0f);
            var skipBtn = skipBtnGO.AddComponent<Button>();
            skipBtn.targetGraphic = skipImg;
            skipBtn.onClick.AddListener(() => { DialogueAdvanceRequested = true; });

            _dialogueCanvasGroup.alpha = 0f;
            go.SetActive(false);
        }

        // SeedDialogueTrigger가 호출: 대화창을 보여주고 한 줄을 표시한다.
        public void ShowDialogueLine(string text, bool showPortrait)
        {
            if (_dialoguePanel == null) return;
            DialogueAdvanceRequested = false;
            if (_dialogueBoxImage != null)
            {
                var sprite = showPortrait && _dialogueBoxPortraitSprite != null ? _dialogueBoxPortraitSprite : _dialogueBoxDefaultSprite;
                if (sprite != null)
                {
                    _dialogueBoxImage.sprite = sprite;
                    float aspect = (float)sprite.texture.width / sprite.texture.height;
                    var rt = (RectTransform)_dialoguePanel.transform;
                    rt.sizeDelta = new Vector2(rt.sizeDelta.y * aspect, rt.sizeDelta.y);
                    var textRt = _dialogueText.rectTransform;
                    textRt.offsetMin = new Vector2(rt.sizeDelta.x * (showPortrait ? 0.26f : 0.06f), 20f);
                }
            }
            if (_dialogueText != null) _dialogueText.text = text;
            if (!_dialoguePanel.activeSelf) _dialoguePanel.SetActive(true);
            _dialogueCanvasGroup.alpha = 1f;
        }

        // SeedDialogueTrigger가 호출: 대화창을 숨긴다.
        public void HideDialoguePanel()
        {
            if (_dialoguePanel == null) return;
            _dialogueCanvasGroup.alpha = 0f;
            _dialoguePanel.SetActive(false);
        }

        // ---------------------------------------------------------------
        // 일시정지 패널 (참고 이미지의 완성된 패널 아트 + 투명 버튼 3개)
        // ---------------------------------------------------------------
        private void BuildPausePanel()
        {
            var scrim = MakeScrim("PausePanel", _canvas.transform, scrimColor);
            _pausePanel = scrim.gameObject;

            var card = MakeArtCard("PauseCard", scrim, _pauseBgSprite, 560f);

            MakeInvisibleButton("ResumeHitZone", card, 0.5f, 0.6145f, card.sizeDelta.x * 0.836f, card.sizeDelta.y * 0.193f, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.TogglePause();
            });

            MakeInvisibleButton("SettingsHitZone", card, 0.5f, 0.3946f, card.sizeDelta.x * 0.836f, card.sizeDelta.y * 0.187f, OpenSettings);

            MakeInvisibleButton("TitleHitZone", card, 0.5f, 0.1807f, card.sizeDelta.x * 0.836f, card.sizeDelta.y * 0.193f, () =>
            {
                if (GameManager.Instance != null) GameManager.Instance.GoToTitle();
            });
        }

        // ---------------------------------------------------------------
        // 설정 패널 (참고 이미지에 해당 화면이 없어 기존 팔레트로 제작, 타이틀/일시정지 양쪽에서 접근)
        // ---------------------------------------------------------------
        private void BuildSettingsPanel()
        {
            var scrim = MakeScrim("SettingsPanel", _canvas.transform, scrimColor);
            _settingsPanel = scrim.gameObject;
            _settingsPanel.SetActive(false);

            var card = MakeCard("SettingsCard", scrim, new Vector2(640, 420), cardColor);

            var title = MakeText("SettingsTitle", card, "설정", 38, titleColor, TextAnchor.MiddleCenter, true);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = new Vector2(0.5f, 0.5f);
            titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(400, 70);
            titleRt.anchoredPosition = new Vector2(0, 150);
            AddShadow(title);

            var volLabel = MakeText("VolumeLabel", card, "볼륨", 24, textColor, TextAnchor.MiddleCenter, true);
            var volLabelRt = volLabel.rectTransform;
            volLabelRt.anchorMin = new Vector2(0.5f, 0.5f);
            volLabelRt.anchorMax = new Vector2(0.5f, 0.5f);
            volLabelRt.anchoredPosition = new Vector2(0, 50);
            volLabelRt.sizeDelta = new Vector2(360, 40);

            var sliderGO = new GameObject("VolumeSlider", typeof(RectTransform));
            sliderGO.transform.SetParent(card, false);
            var sliderRt = sliderGO.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0.5f, 0.5f);
            sliderRt.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRt.anchoredPosition = new Vector2(0, -10);
            sliderRt.sizeDelta = new Vector2(440, 30);
            _volumeSlider = sliderGO.AddComponent<Slider>();

            var sliderBgGO = new GameObject("Background", typeof(RectTransform));
            sliderBgGO.transform.SetParent(sliderGO.transform, false);
            var sliderBgRt = sliderBgGO.GetComponent<RectTransform>();
            sliderBgRt.anchorMin = Vector2.zero;
            sliderBgRt.anchorMax = Vector2.one;
            sliderBgRt.offsetMin = Vector2.zero;
            sliderBgRt.offsetMax = Vector2.zero;
            var sliderBgImg = sliderBgGO.AddComponent<Image>();
            sliderBgImg.sprite = UIStyleKit.RoundedRect;
            sliderBgImg.type = Image.Type.Sliced;
            sliderBgImg.color = new Color(0f, 0f, 0f, 0.12f);

            var fillAreaGO = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            var fillAreaRt = fillAreaGO.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0f);
            fillAreaRt.anchorMax = new Vector2(1f, 1f);
            fillAreaRt.offsetMin = new Vector2(4, 4);
            fillAreaRt.offsetMax = new Vector2(-4, -4);

            var fillGO = new GameObject("Fill", typeof(RectTransform));
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRt = fillGO.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.sprite = UIStyleKit.RoundedRect;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = accentGold;

            var handleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGO.transform.SetParent(sliderGO.transform, false);
            var handleAreaRt = handleAreaGO.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = Vector2.zero;
            handleAreaRt.anchorMax = Vector2.one;
            handleAreaRt.offsetMin = Vector2.zero;
            handleAreaRt.offsetMax = Vector2.zero;

            var handleGO = new GameObject("Handle", typeof(RectTransform));
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            var handleRt = handleGO.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(30, 30);
            var handleImg = handleGO.AddComponent<Image>();
            handleImg.sprite = UIStyleKit.Circle;
            handleImg.color = Color.white;

            _volumeSlider.fillRect = fillRt;
            _volumeSlider.handleRect = handleRt;
            _volumeSlider.targetGraphic = handleImg;
            _volumeSlider.direction = Slider.Direction.LeftToRight;
            _volumeSlider.minValue = 0f;
            _volumeSlider.maxValue = 1f;
            _volumeSlider.value = 1f;
            _volumeSlider.onValueChanged.AddListener(v =>
            {
                AudioListener.volume = v;
                PlayerPrefs.SetFloat(VolumePrefKey, v);
            });

            MakeButton("SettingsCloseButton", card, "닫기", new Vector2(0, -140), new Vector2(300, 72), secondaryButtonColor, CloseSettings);
        }

        private void OpenSettings()
        {
            if (_settingsPanel != null) _settingsPanel.SetActive(true);
        }

        private void CloseSettings()
        {
            if (_settingsPanel != null) _settingsPanel.SetActive(false);
        }

        private void HandleStateChanged(GameManager.GameState state)
        {
            if (_titlePanel != null) _titlePanel.SetActive(state == GameManager.GameState.Title);
            if (_stageSelectPanel != null) _stageSelectPanel.SetActive(state == GameManager.GameState.StageSelect);
            if (_hudPanel != null) _hudPanel.SetActive(state == GameManager.GameState.Playing || state == GameManager.GameState.Paused);
            if (_pausePanel != null) _pausePanel.SetActive(state == GameManager.GameState.Paused);
            if (_settingsPanel != null) _settingsPanel.SetActive(false);

            if (state == GameManager.GameState.StageSelect) RefreshStageButtons();
            if (state == GameManager.GameState.Title) RefreshTitleButtons();

            if (state == GameManager.GameState.Title || state == GameManager.GameState.StageSelect)
            {
                HideDialoguePanel();
            }

            if (state == GameManager.GameState.Playing)
            {
                if (GameManager.Instance != null && GameManager.Instance.CurrentStage == 1)
                {
                    ShowControlHintsForStage1();
                }
            }
            else if (state != GameManager.GameState.Paused)
            {
                // 새 스테이지로 다시 들어올 때(스테이지 선택 -> Playing) 힌트를 다시 보여줄 수 있도록 리셋.
                _controlHintsShownThisStage = false;
            }
        }

        private void HandleSeedCountChanged(int collected, int total)
        {
            if (_seedText != null) _seedText.text = string.Format("씨앗 {0} / {1}", collected, total);
        }
    }
}
