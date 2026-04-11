using UnityEngine;

namespace MiniGameTemplate.Example.VFXDemo
{
    /// <summary>
    /// VFXDemo 场景内的额外播放热键：重播、单发补播、类型模式切换。
    /// 通用返回与说明展示交给 ExampleSceneHotkeys。
    /// </summary>
    public class VFXDemoInputHint : MonoBehaviour
    {
        [SerializeField] private VFXDemoSpawner _spawner;
        [SerializeField] private KeyCode _replayKey = KeyCode.R;
        [SerializeField] private KeyCode _spawnOnceKey = KeyCode.Space;
        [SerializeField] private KeyCode _sequentialKey = KeyCode.Alpha1;
        [SerializeField] private KeyCode _randomKey = KeyCode.Alpha2;
        [SerializeField] private KeyCode _fixedFirstKey = KeyCode.Alpha3;
        [SerializeField] private KeyCode _fixedSecondKey = KeyCode.Alpha4;
        [SerializeField] private KeyCode _fixedThirdKey = KeyCode.Alpha5;
        [SerializeField] private KeyCode _fixedFourthKey = KeyCode.Alpha6;
        [SerializeField] private KeyCode _fixedFifthKey = KeyCode.Alpha7;

        private void Update()
        {
            if (Input.GetKeyDown(_replayKey))
                _spawner?.RestartLoop();

            if (Input.GetKeyDown(_spawnOnceKey))
                _spawner?.SpawnOneManual();

            if (Input.GetKeyDown(_sequentialKey))
                _spawner?.SetSelectionModeSequential();

            if (Input.GetKeyDown(_randomKey))
                _spawner?.SetSelectionModeRandom();

            if (Input.GetKeyDown(_fixedFirstKey))
                _spawner?.SetFixedTypeIndex(0);

            if (Input.GetKeyDown(_fixedSecondKey))
                _spawner?.SetFixedTypeIndex(1);

            if (Input.GetKeyDown(_fixedThirdKey))
                _spawner?.SetFixedTypeIndex(2);

            if (Input.GetKeyDown(_fixedFourthKey))
                _spawner?.SetFixedTypeIndex(3);

            if (Input.GetKeyDown(_fixedFifthKey))
                _spawner?.SetFixedTypeIndex(4);
        }
    }
}
