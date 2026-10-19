using UnityEngine;

namespace DokkaebiStoneBridge
{
    // 씬 하나 안에 5개 스테이지의 레이아웃(StageRoot_01~05)을 전부 넣어두고,
    // 현재 스테이지에 해당하는 것만 켜고 나머지는 꺼서, 하나의 씬/하나의 플레이어로
    // 여러 맵을 운영한다. 플레이어도 해당 스테이지의 스폰 지점으로 옮긴다.
    //
    // GameManager.Start()가 씨앗 개수를 셀 때(FindObjectsByType<SeedFragment>)
    // 꺼져있는 다른 스테이지의 씨앗이 섞여 들어가면 안 되므로, 반드시 GameManager보다
    // 먼저 꺼져 있어야 한다. Unity는 "씬의 모든 오브젝트의 Awake가 끝난 뒤에만" Start를
    // 호출하므로, 이 스크립트는 Awake()에서 처리하고(생성 순서와 무관하게 항상 먼저 끝남),
    // GameManager와 직접 참조를 주고받지 않고 PlayerPrefs를 각자 읽어서 독립적으로 동작한다.
    public class StageLayoutManager : MonoBehaviour
    {
        [System.Serializable]
        public class StageDefinition
        {
            public int stageNumber;
            public GameObject root;
            public Transform spawnPoint;
        }

        public StageDefinition[] stages;
        public Transform player;
        public IsoCameraRig cameraRig;

        private const string PendingStageKey = "DSB_PendingStage";

        private void Awake()
        {
            int pending = PlayerPrefs.GetInt(PendingStageKey, -1);
            int stageToActivate = pending >= 1 ? pending : 1;
            ActivateStage(stageToActivate);
        }

        public void ActivateStage(int stageNumber)
        {
            StageDefinition target = null;

            if (stages != null)
            {
                foreach (var def in stages)
                {
                    if (def == null) continue;
                    bool isTarget = def.stageNumber == stageNumber;
                    if (def.root != null) def.root.SetActive(isTarget);
                    if (isTarget) target = def;
                }
            }

            if (target == null)
            {
                Debug.LogWarning("[StageLayoutManager] 스테이지 " + stageNumber + "에 해당하는 StageDefinition을 찾지 못함.");
                return;
            }

            if (player != null && target.spawnPoint != null)
            {
                player.SetParent(null, true);
                player.position = target.spawnPoint.position;
                player.rotation = target.spawnPoint.rotation;

                var rb = player.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            if (cameraRig != null && target.spawnPoint != null)
            {
                cameraRig.followTarget = player;
                cameraRig.lookAtPoint = target.spawnPoint.position;
                cameraRig.Apply();
            }
        }
    }
}
