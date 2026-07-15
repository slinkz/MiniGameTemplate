using System;
using System.Threading.Tasks;
using FairyGUI;

namespace Game.ShooterGame
{
    /// <summary>
    /// BattleHUD Controller 接口——Core 层通过此接口与 UI 层解耦。
    /// </summary>
    public interface IBattleHUDController
    {
        void Show();
        Task ShowAsync();
        void ForceRefresh();
        GComponent GetView();
    }

    /// <summary>暂停面板 Controller 接口</summary>
    public interface IPausePanelController
    {
        void Show();
        void Hide();
        void BindEvents(Action onResume, Action onQuit);
    }

    /// <summary>胜利面板 Controller 接口</summary>
    public interface IVictoryPanelController
    {
        void Show(BattleResultData result);
        void BindEvents(Action onReturnToSelect);
    }

    /// <summary>失败面板 Controller 接口（V2：传入结果数据用于波次/击杀展示）</summary>
    public interface IDefeatPanelController
    {
        void Show(BattleResultData result);
        void BindEvents(Action onRetry, Action onQuit);
    }

    /// <summary>虚拟摇杆 Controller 接口</summary>
    public interface IJoystickController
    {
        void Init(GComponent battleHUD);
        void SetEnabled(bool enabled);
    }
}
