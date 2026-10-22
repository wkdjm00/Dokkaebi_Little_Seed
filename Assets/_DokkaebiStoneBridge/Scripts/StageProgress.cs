using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 스테이지 잠금/클리어 진행 상태를 PlayerPrefs에 저장해서 관리한다.
    // 총 5스테이지, 1스테이지는 항상 열려있고 N스테이지를 깨면 N+1이 풀린다.
    // (지금은 실제 스테이지 레벨이 하나뿐이라 전부 같은 씬을 재시작하는 식으로 동작하지만,
    // 나중에 스테이지별로 씬이 생기면 GameManager.SelectStage()에서 해당 씬을 로드하도록 바꾸면 된다.)
    public static class StageProgress
    {
        public const int StageCount = 10;
        private const string ClearedKeyPrefix = "DSB_Stage_Cleared_";

        public static bool IsCleared(int stage)
        {
            return PlayerPrefs.GetInt(ClearedKeyPrefix + stage, 0) == 1;
        }

        public static bool IsUnlocked(int stage)
        {
            if (stage <= 1) return true;
            return IsCleared(stage - 1);
        }

        public static void MarkCleared(int stage)
        {
            PlayerPrefs.SetInt(ClearedKeyPrefix + stage, 1);
            PlayerPrefs.Save();
        }

        // 한 번이라도 깬 스테이지가 있는지 (처음 실행인지 / 돌아온 플레이어인지 판단용)
        public static bool HasAnyProgress()
        {
            for (int i = 1; i <= StageCount; i++)
            {
                if (IsCleared(i)) return true;
            }
            return false;
        }

        // 클리어 기록 초기화 규칙
        //  - 에디터: Unity를 끄면 초기화 (다음에 Unity를 열 때 한 번). Play를 껐다 켜는 것만으로는 초기화되지 않는다.
        //  - 빌드  : 게임을 완전히 끄고 다시 실행하면 초기화.
        //  - 스테이지 재시작/선택으로 씬을 다시 불러올 때는 항상 유지된다.
        // (볼륨 같은 설정값은 건드리지 않는다.)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ResetOnAppLaunch()
        {
#if !UNITY_EDITOR
            ResetForNewRun();
#endif
        }

#if UNITY_EDITOR
        // 에디터 세션당 한 번만 실행된다. (SessionState는 Unity를 끄면 사라지고, Play/스크립트 재컴파일로는 유지된다.)
        [UnityEditor.InitializeOnLoadMethod]
        private static void ResetOnEditorSessionStart()
        {
            if (UnityEditor.SessionState.GetBool("DSB_ProgressResetDone", false)) return;
            UnityEditor.SessionState.SetBool("DSB_ProgressResetDone", true);
            ResetForNewRun();
        }
#endif

        private static void ResetForNewRun()
        {
            ResetAll();
            PlayerPrefs.DeleteKey("DSB_PendingStage");
            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            for (int i = 1; i <= StageCount; i++)
            {
                PlayerPrefs.DeleteKey(ClearedKeyPrefix + i);
            }
            PlayerPrefs.Save();
        }
    }
}
