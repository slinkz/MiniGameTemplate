using System.Runtime.CompilerServices;

// 允许测试程序集访问 internal 成员（Entity.ConfigSO setter、Entity.InitAll 等）
[assembly: InternalsVisibleTo("MiniGameFramework.Tests.Editor")]
