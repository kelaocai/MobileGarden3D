using UnityEngine;

// 保留兼容接口，避免旧角色移动代码失去引用。
// 塔防关卡不再创建摇杆、开火按钮或运行时 Canvas。
public sealed class MobileMobaControls : MonoBehaviour
{
    public static Vector2 MoveInput => Vector2.zero;
}
