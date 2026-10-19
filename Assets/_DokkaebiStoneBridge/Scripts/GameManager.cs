using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace DokkaebiStoneBridge
{
    // 게임 전체 상태(타이틀 / 스테이지 선택 / 진행 중 / 일시정지 / 클리어)를 관리하는 싱글턴.
    // 발각/몬스터 접촉은 '게임 오버'로 멈추지 않고, 시작 지점으로 즉시
    // 리스폰시키는 패널티로 처리한다 (클리어 상태만 완전히 멈춘다).
    public class GameManager : MonoBehaviour
    {
        public enum GameState { Title, StageSelect, Playing, Paused, Cleared }

        public static GameManager Instance { get; private set; }

        public bool IsGameOver { get; private set; }
        public bool Cleared { get; private set; }
        public GameState State { get; private set; } = GameState.Title;

        public event System.Action<GameState> OnStateChanged;
        public event System.Action<int, int> OnSeedCountChanged;

        [Tooltip("발각/접촉 시 잠깐 깜빡이는 카메라 색")]
        public Color gameOverColor = new Color(0.45f, 0.05f, 0.05f);
        [Tooltip("클리어했을 때 카메라 배경색")]
        public Color clearColor = new Color(0.95f, 0.85f, 0.4f);
        public float detectFlashDuration = 0.3f;

        [Tooltip("true면 시작하자마자 바로 플레이 상태(타이틀/스테이지 선택 생략, 테스트용)")]
        public bool skipTitleForTesting = false;

        public int totalFragments;
        private int _collected;
        private Camera _cam;

        private const string PendingStageKey = "DSB_PendingStage";

        public Vector3 LastSafePosition { get; private set; }
        public Vector3 StartPosition { get; private set; }

        public int CollectedFragments => _collected;

        // 현재 플레이 중인 스테이지 번호(1~5). 지금은 스테이지가 하나뿐이라 전부 같은 씬을
        // 재시작하는 식으로 동작하지만, 나중에 스테이지별 씬이 생기면 SelectStage()에서
        // SceneManager.LoadScene(스테이지별 씬 이름)으로 바꾸면 된다.
        public int CurrentStage { get; private set; } = 1;

        private void Awake()
        {
            Instance = this;
            _cam = Camera.main;
        }

        private void Start()
        {
            totalFragments = FindObjectsByType<SeedFragment>(FindObjectsInactive.Exclude).Length;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                LastSafePosition = player.transform.position;
                StartPosition = player.transform.position;
            }

            int pendingStage = PlayerPrefs.GetInt(PendingStageKey, -1);
            if (pendingStage >= 1)
            {
                PlayerPrefs.DeleteKey(PendingStageKey);
                CurrentStage = pendingStage;
                State = GameState.Playing;
                Time.timeScale = 1f;
            }
            else
            {
                State = skipTitleForTesting ? GameState.Playing : GameState.Title;
                Time.timeScale = State == GameState.Title ? 0f : 1f;
            }

            OnStateChanged?.Invoke(State);
            OnSeedCountChanged?.Invoke(_collected, totalFragments);
        }

        private void Update()
        {
            if (Cleared && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                RestartLevel();
                return;
            }

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (State == GameState.Playing || State == GameState.Paused)
                {
                    TogglePause();
                }
            }
        }

        // 바닥(StandSurface)이 리프트처럼 아래로 내려가는 도중에도 매 프레임 '안전 위치'로
        // 계속 갱신되는데, 리프트가 추락 판정 높이(PlayerRespawn.fallY)보다 더 아래까지
        // 내려갔다가 플레이어가 함께 그 높이까지 실려 내려가면, 안전 위치 자체가 이미
        // '추락한 위치'로 기록되어 버린다. 그러면 추락 판정 -> 안전 위치로 리스폰 -> 그 위치도
        // 추락 판정 -> 다시 리스폰 ... 이 무한 반복되면서 플레이어가 그대로 멈춰버린 것처럼
        // 보이는 버그가 생긴다 (실제로는 똑같은 위치로 계속 순간이동하고 있는 것).
        // 이를 막기 위해, 추락 판정 높이 근처거나 그보다 낮은 위치는 애초에 '안전 위치'로
        // 기록하지 않는다.
        [Tooltip("이 높이보다 낮은 위치는 '안전 위치'로 기록하지 않는다 (PlayerRespawn.fallY보다 넉넉하게 높아야 함)")]
        public float minSafeY = -2f;

        public void RecordSafePosition(Vector3 pos)
        {
            if (pos.y < minSafeY) return;
            LastSafePosition = pos;
        }

        public void StartGame()
        {
            if (State != GameState.Title) return;
            State = GameState.Playing;
            Time.timeScale = 1f;
            OnStateChanged?.Invoke(State);
        }

        // 리로드 없이 바로 특정 스테이지 플레이를 시작한다 (처음 실행 시 스테이지 선택을
        // 건너뛰고 바로 1스테이지로 들어갈 때 사용).
        public void BeginStage(int stage)
        {
            CurrentStage = Mathf.Clamp(stage, 1, StageProgress.StageCount);
            State = GameState.Playing;
            Time.timeScale = 1f;
            OnStateChanged?.Invoke(State);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing)
            {
                State = GameState.Paused;
                Time.timeScale = 0f;
                OnStateChanged?.Invoke(State);
            }
            else if (State == GameState.Paused)
            {
                State = GameState.Playing;
                Time.timeScale = 1f;
                OnStateChanged?.Invoke(State);
            }
        }

        // 타이틀 화면의 '시작하기', 일시정지 메뉴의 '나가기' 둘 다 여기로 온다.
        public void OpenStageSelect()
        {
            if (State == GameState.Cleared) return;
            State = GameState.StageSelect;
            Time.timeScale = 0f;
            OnStateChanged?.Invoke(State);
        }

        public void BackToTitleFromStageSelect()
        {
            if (State != GameState.StageSelect) return;
            State = GameState.Title;
            Time.timeScale = 0f;
            OnStateChanged?.Invoke(State);
        }

        // 일시정지 메뉴에서 바로 타이틀 화면으로 돌아갈 때 사용 (스테이지 선택을 거치지 않음).
        public void GoToTitle()
        {
            State = GameState.Title;
            Time.timeScale = 0f;
            OnStateChanged?.Invoke(State);
        }

        // 스테이지 선택 화면에서 스테이지를 고르면 호출됨. 잠긴 스테이지는 무시한다.
        public void SelectStage(int stage)
        {
            if (!StageProgress.IsUnlocked(stage)) return;

            PlayerPrefs.SetInt(PendingStageKey, stage);
            PlayerPrefs.Save();

            Time.timeScale = 1f;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
        }

        public void QuitToTitle()
        {
            Time.timeScale = 1f;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // 몬스터 시야에 들어오거나(WatcherVision) 몬스터 몸에 직접 닿으면(WatcherTouch) 호출됨.
        public void OnDetected()
        {
            if (Cleared) return;
            Debug.Log("[GameManager] 발각/몬스터 접촉! 시작 지점으로 리스폰합니다.");

            if (_cam != null) StartCoroutine(FlashBackground(gameOverColor, detectFlashDuration));

            var player = GameObject.FindGameObjectWithTag("Player");
            var respawn = player != null ? player.GetComponent<PlayerRespawn>() : null;
            if (respawn != null) respawn.RespawnTo(StartPosition);
        }

        private IEnumerator FlashBackground(Color flashColor, float duration)
        {
            if (_cam == null) yield break;
            Color original = _cam.backgroundColor;
            _cam.backgroundColor = flashColor;
            yield return new WaitForSecondsRealtime(duration);
            if (_cam != null && !Cleared) _cam.backgroundColor = original;
        }

        public void OnGoalReached()
        {
            if (Cleared) return;
            Cleared = true;
            State = GameState.Cleared;
            if (_cam != null) _cam.backgroundColor = clearColor;
            OnStateChanged?.Invoke(State);
            Debug.Log("[GameManager] 클리어! 세계수가 되살아났습니다. R키로 다시 시작할 수 있습니다.");
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene.name);
        }

        public void NotifyFragmentCollected()
        {
            _collected++;
            OnSeedCountChanged?.Invoke(_collected, totalFragments);
            Debug.Log(string.Format("[GameManager] 씨앗 조각 수집: {0}/{1}", _collected, totalFragments));
        }

        public bool AllFragmentsCollected => totalFragments > 0 && _collected >= totalFragments;

        public bool WorldTreeRevived { get; private set; }

        // 세계수(EndingAltar)에 닿았을 때 호출됨. 게임을 끝내는 대신 하늘을 밤->아침으로 전환시키고,
        // 현재 스테이지를 클리어 처리해서 다음 스테이지 잠금을 풀어준다.
        public void OnWorldTreeTouched()
        {
            if (WorldTreeRevived) return;
            WorldTreeRevived = true;

            Debug.Log("[GameManager] 세계수에 닿았다! 하늘이 밝아지기 시작합니다.");
            StageProgress.MarkCleared(CurrentStage);

            var sky = FindAnyObjectByType<SkyController>();
            float dawnDelay = 2f;
            if (sky != null)
            {
                dawnDelay = sky.transitionDuration;
                sky.TriggerDawn();
            }
            else
            {
                Debug.LogWarning("[GameManager] SkyController를 찾을 수 없어 하늘 전환을 실행하지 못했습니다.");
            }

            // 하늘이 다 밝아진 다음 스테이지 선택 화면을 띄워서 바로 다음 스테이지로 넘어갈 수 있게 한다.
            StartCoroutine(OpenStageSelectAfterDelay(dawnDelay));
        }

        private IEnumerator OpenStageSelectAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            OpenStageSelect();
        }
    }
}
