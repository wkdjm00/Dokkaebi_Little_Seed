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
