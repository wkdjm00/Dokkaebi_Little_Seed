using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using DokkaebiStoneBridge;

namespace DokkaebiStoneBridge.EditorTools
{
    // 새 스테이지를 코드로 조립할 때 쓰는 공용 헬퍼 모음 (에디터 전용).
    // 1스테이지~5스테이지에서 이미 검증된 "4x4 타일 + Connector/FlipBridge + 압력판/장승/리프트 +
    // 감시자" 조합 방식을 그대로 재사용해서 6~10스테이지를 조립하는 데 쓴다.
    public static class StageKit
    {
        public static Transform[] AllTransforms()
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        public static Transform FindRoot(Transform[] all, string name)
        {
            foreach (var t in all)
            {
                if (t == null) continue;
                if (t.parent == null && t.name == name) return t;
            }
            return null;
        }

        public static Transform FindByName(Transform[] all, string name)
        {
            foreach (var t in all)
            {
                if (t == null) continue;
                if (t.name == name) return t;
            }
            return null;
        }

        // 템플릿 오브젝트(기존 스테이지의 Platform/Connector/FlipBridge/Watcher/Seed 등)를 통째로 복제해서
        // 새 부모 아래, 로컬 좌표/Y회전으로 배치한다. 내부 계층(FloorArt/TileGroup 등)은 그대로 유지됨.
        public static GameObject CloneUnder(GameObject template, Transform newParent, string newName, Vector3 localPos, float rotY)
        {
            var clone = Object.Instantiate(template);
            clone.name = newName;
            clone.transform.SetParent(newParent, false);
            clone.transform.localPosition = localPos;
            clone.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            return clone;
        }

        public static GameObject CreateStageRoot(string name, float worldX)
        {
            var go = new GameObject(name);
            go.transform.position = new Vector3(worldX, 0f, 0f);
            return go;
        }

        public static GameObject CreateSpawnPoint(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("SpawnPoint");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        public static Material WallMaterial()
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.22f, 0.2f, 0.17f);
            return mat;
        }

        // 시야/인식을 가로막는 벽(안전지대용). 콜라이더는 평범한(비트리거) 박스라서 물리적으로도 막는다.
        public static GameObject BuildWall(Transform parent, string name, Vector3 localCenter, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localCenter;
            go.transform.localScale = size;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            go.AddComponent<VisionBlocker>();
            return go;
        }

        public static void TuneWatcher(GameObject watcherGO, float? zoneRadius = null, float? awarenessRadius = null,
            Vector3? zoneOffset = null, bool? patrol = null, float? patrolDistance = null, float? patrolSpeed = null,
            float? loseInterestDelay = null)
        {
            var w = watcherGO.GetComponent<WatcherVision>();
            if (w == null) return;
            var so = new SerializedObject(w);
            if (zoneRadius.HasValue) so.FindProperty("zoneRadius").floatValue = zoneRadius.Value;
            if (awarenessRadius.HasValue) so.FindProperty("awarenessRadius").floatValue = awarenessRadius.Value;
            if (zoneOffset.HasValue) so.FindProperty("zoneOffset").vector3Value = zoneOffset.Value;
            if (patrol.HasValue) so.FindProperty("patrol").boolValue = patrol.Value;
            if (patrolDistance.HasValue) so.FindProperty("patrolDistance").floatValue = patrolDistance.Value;
            if (patrolSpeed.HasValue) so.FindProperty("patrolSpeed").floatValue = patrolSpeed.Value;
            if (loseInterestDelay.HasValue) so.FindProperty("loseInterestDelay").floatValue = loseInterestDelay.Value;
            so.ApplyModifiedProperties();
        }

        // FloorLift를 복제하면 trigger 참조가 원본 스테이지의 PressurePlate를 계속 가리키므로,
        // 복제한 뒤 반드시 이 함수로 새로 복제한 PressurePlate를 다시 연결해줘야 한다.
        public static void WireFloorLift(GameObject floorLiftGO, GameObject pressurePlateGO)
        {
            var so = new SerializedObject(floorLiftGO.GetComponent<FloorLift>());
            so.FindProperty("trigger").objectReferenceValue = pressurePlateGO.GetComponent<PressurePlate>();
            so.ApplyModifiedProperties();
        }

        public static void SaveScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
