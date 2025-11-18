using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace EasyRemote.WebService
{
    /// <summary>
    /// イベントコントロール
    ///   ブラウザ側から受け付けたイベントを Windows側に渡すクラス
    /// </summary>
    public class EventControl
    {
        [StructLayout(LayoutKind.Sequential)]
        struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }


        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);
        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        /// <summary>
        /// ブラウザから渡されたマウスイベント
        /// </summary>
        public class InputMouseData
        {
            public string type { get; set; }    // イベント種別
            public string button { get; set; }  // ボタン種別
            public double x { get; set; }       // X座標
            public double y { get; set; }       // Y座標
            public double delta { get; set; }   // ホイル量
        }


        const uint INPUT_MOUSE = 0;
        const uint MOUSEEVENTF_MOVE = 0x0001;
        const uint MOUSEEVENTF_LEFTDOWN = 0x02;
        const uint MOUSEEVENTF_LEFTUP = 0x04;
        const uint MOUSEEVENTF_RIGHTDOWN = 0x08;
        const uint MOUSEEVENTF_RIGHTUP = 0x10;
        const uint MOUSEEVENTF_MIDDLEDOWN = 0x20;
        const uint MOUSEEVENTF_MIDDLEUP = 0x40;
        const uint MOUSEEVENTF_WHEEL = 0x0800;

        private double m_dpiX = 1.0;
        private double m_dpiY = 1.0;

        private int m_lastX;
        private int m_lastY;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public EventControl()
        {
            var source = PresentationSource.FromVisual(Application.Current.MainWindow);
            if (source != null)
            {
                m_dpiX = source.CompositionTarget.TransformToDevice.M11;
                m_dpiY = source.CompositionTarget.TransformToDevice.M22;
            }
        }

        /// <summary>
        /// マウスイベントの送信
        /// </summary>
        /// <param name="data"></param>
        public void sendMouseEvent(InputMouseData data)
        {
            int x = (int)data.x;
            int y = (int)data.y;
            int diffX, diffY;

            uint flag = 0;
            switch (data.type)
            {
                case "pointerdown":
                    switch (data.button)
                    {
                        case "left": flag = MOUSEEVENTF_LEFTDOWN; break;
                        case "right": flag = MOUSEEVENTF_RIGHTDOWN; break;
                        case "middle": flag = MOUSEEVENTF_MIDDLEDOWN; break;
                    }
                    SetCursorPos(x, y);  // カーソル位置移動（シングルモニタならOK）
                    m_lastX = x;
                    m_lastY = y;
                    LogController.getInstance().infoLog( $"DOWN {x}, {y}");
                    break;

                case "pointerup":
                    switch (data.button)
                    {
                        case "left": flag = MOUSEEVENTF_LEFTUP; break;
                        case "right": flag = MOUSEEVENTF_RIGHTUP; break;
                        case "middle": flag = MOUSEEVENTF_MIDDLEUP; break;
                    }
                    //SetCursorPos(x, y);  // カーソル位置移動（シングルモニタならOK）
                    LogController.getInstance().infoLog($"UP {x}, {y}");
                    break;
                case "pointermove":
                    LogController.getInstance().infoLog($"MOVE {x}, {y}");
                    diffX = x - m_lastX;
                    diffY = y - m_lastX;
                    m_lastX = x;
                    m_lastY = y;
                    x = diffX;
                    y = diffX;

                    flag = MOUSEEVENTF_MOVE;
                    break;
                case "wheel":
                    flag = MOUSEEVENTF_WHEEL;
                    break;

                default:
                    return;
            }

            // SendInput構造体を使用（mouse_eventより確実）
            INPUT input = new INPUT
            {
                type = INPUT_MOUSE,
                mi = new MOUSEINPUT
                {
                    dx = x,
                    dy = y,
                    mouseData = (data.type == "wheel") ? (uint)(-data.delta * 120) : 0, // ホイール対応
                    dwFlags = flag ,
                    time = 0,
                    dwExtraInfo = IntPtr.Zero
                }
            };

            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
