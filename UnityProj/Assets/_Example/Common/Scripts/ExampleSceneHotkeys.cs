using UnityEngine;

namespace MiniGameTemplate.Example
{
    /// <summary>
    /// 示例场景通用热键入口：统一处理返回主菜单与左上角说明文字。
    /// 只负责输入与展示，不负责具体玩法逻辑。
    /// </summary>
    public class ExampleSceneHotkeys : MonoBehaviour
    {
        [SerializeField] private KeyCode _backKey = KeyCode.Escape;
        [SerializeField] private string _title = "Example Demo";
        [SerializeField, TextArea(2, 8)] private string _instructions = "Esc = 返回主菜单";
        [SerializeField] private Rect _area = new Rect(24f, 24f, 360f, 220f);
        [SerializeField] private int _titleFontSize = 24;
        [SerializeField] private int _bodyFontSize = 16;

        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;

        private void Update()
        {
            if (Input.GetKeyDown(_backKey))
                ExampleSceneNavigator.ReturnToMainMenu();
        }

        private void OnGUI()
        {
            EnsureStyles();

            GUILayout.BeginArea(_area);
            GUILayout.Label(_title, _titleStyle);
            GUILayout.Space(8f);
            GUILayout.Label(_instructions, _bodyStyle);
            GUILayout.EndArea();
        }

        public void SetText(string title, string instructions)
        {
            _title = title;
            _instructions = instructions;
        }

        private void EnsureStyles()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = _titleFontSize,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }

            if (_bodyStyle == null)
            {
                _bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = _bodyFontSize,
                    wordWrap = true,
                    normal = { textColor = new Color(0.92f, 0.96f, 1f, 1f) }
                };
            }
        }
    }
}
