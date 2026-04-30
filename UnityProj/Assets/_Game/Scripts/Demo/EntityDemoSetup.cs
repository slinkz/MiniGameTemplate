using UnityEngine;
using MiniGameTemplate.Entity;

namespace MiniGameTemplate.Game.Demo
{
    /// <summary>
    /// P1.11 Demo 场景辅助——在 Play Mode 中显示操作说明 HUD。
    /// 挂在场景中任意 GO 上即可。
    /// </summary>
    public class EntityDemoSetup : MonoBehaviour
    {
        [Header("HUD 配置")]
        [Tooltip("是否显示操作说明")]
        public bool ShowControlsHUD = true;

        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;

        private void OnGUI()
        {
            if (!ShowControlsHUD) return;

            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box);
                _boxStyle.fontSize = 14;

                _labelStyle = new GUIStyle(GUI.skin.label);
                _labelStyle.fontSize = 13;
                _labelStyle.richText = true;
            }

            float w = 260f;
            float h = 160f;
            Rect rect = new Rect(10f, 10f, w, h);

            GUI.Box(rect, "Entity Demo (P1.11)", _boxStyle);

            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 25f, w - 20f, h - 30f));
            GUILayout.Label("<b>WASD</b> / Arrow Keys — Move", _labelStyle);
            GUILayout.Label("<b>Space</b> / J — Shoot", _labelStyle);
            GUILayout.Space(8f);

            var mgr = EntityManagerAccessor.Instance;
            if (mgr != null)
            {
                GUILayout.Label($"Active Entities: <color=yellow>{mgr.ActiveCount}</color>", _labelStyle);
            }

            var spawner = EntityManagerAccessor.Spawner;
            if (spawner != null)
            {
                string status = spawner.IsAllWavesCleared ? "<color=green>All Cleared</color>" : "<color=orange>In Progress</color>";
                GUILayout.Label($"Waves: {status}", _labelStyle);
            }

            GUILayout.EndArea();
        }
    }
}
