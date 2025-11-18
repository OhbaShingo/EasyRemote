using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
//using System.Windows.Forms;
using System.Windows.Media;
using Size = System.Drawing.Size;

namespace EasyRemote.WebService
{
    /// <summary>
    /// キャプチャ処理
    /// </summary>
    internal class CaptureControl
    {
        /*
         *  キャプチャ関連の呼び出しAPI定義
         */
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(
            IntPtr hdcDest,
            int nXDest,
            int nYDest,
            int nWidth,
            int nHeight,
            IntPtr hdcSrc,
            int nXSrc,
            int nYSrc,
            CopyPixelOperation dwRop);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDc);

        /*
         * カーソル描画関連の呼び出しAPI定義
         */
        [DllImport("user32.dll")]
        static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        static extern bool DrawIconEx(IntPtr hdc, int xLeft, int yTop, IntPtr hIcon,
            int cxWidth, int cyWidth, int istepIfAniCur, IntPtr hbrFlickerFreeDraw, int diFlags);

        [DllImport("user32.dll")]
        static extern bool GetCaretPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        // 標準カーソルID
        const int IDC_ARROW = 32512;
        const int IDC_IBEAM = 32513;
        const int IDC_WAIT = 32514;
        const int IDC_CROSS = 32515;
        const int IDC_HAND = 32649;


        const int CURSOR_SHOWING = 0x00000001;
        const int DI_NORMAL = 0x0003;

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }


        private Bitmap m_captureImg;
        private Size m_captureSize;

        private IntPtr m_desktopWnd;
        private IntPtr m_hdcSrc;
        private readonly IntPtr m_hIBeam;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CaptureControl()
        {
            var source = PresentationSource.FromVisual(Application.Current.MainWindow);
            double dpiX = 1.0, dpiY = 1.0;
            if (source != null)
            {
                dpiX = source.CompositionTarget.TransformToDevice.M11;
                dpiY = source.CompositionTarget.TransformToDevice.M22;
            }
            int width = (int)(SystemParameters.PrimaryScreenWidth * dpiX );
            int height = (int)(SystemParameters.PrimaryScreenHeight * dpiY );
            m_captureImg = new Bitmap( width, height );
            m_captureSize = new System.Drawing.Size( width, height );

            m_desktopWnd = GetDesktopWindow();
            m_hdcSrc = GetWindowDC(m_desktopWnd);
            m_hIBeam = LoadCursor(IntPtr.Zero, IDC_IBEAM);

            //            Rectangle screenSize = Screen.PrimaryScreen.Bounds;
        }

        /// <summary>
        /// ファイナライザ
        /// </summary>
        ~CaptureControl()
        {   // 念のため破棄
            ReleaseDC( m_desktopWnd, m_hdcSrc);
        }

        public Bitmap capture()
        {
            using (Graphics g = Graphics.FromImage(m_captureImg))
            {
                IntPtr hdcDest = g.GetHdc();

                // デスクトップから画面コピー
                BitBlt(hdcDest, 0, 0, m_captureSize.Width, m_captureSize.Height,
                       m_hdcSrc, 0, 0,
                       CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

                // カーソルを描画（Iビーム含む）
                CURSORINFO ci = new CURSORINFO();
                ci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                if (GetCursorInfo(out ci) && ci.flags == CURSOR_SHOWING)
                {
                    if (ci.hCursor == m_hIBeam)
                    {
                        g.ReleaseHdc(hdcDest);
                        // Iビームカーソルを自前で描く（黒の縦棒）
                        int lineHeight = 20; // 適宜調整
                        g.DrawLine(Pens.Black,
                                   ci.ptScreenPos.x,
                                   ci.ptScreenPos.y - lineHeight / 2,
                                   ci.ptScreenPos.x,
                                   ci.ptScreenPos.y + lineHeight / 2);
                    }
                    else
                    {
                        // 通常カーソルを描画
                        DrawIconEx(hdcDest,
                            ci.ptScreenPos.x,
                            ci.ptScreenPos.y,
                            ci.hCursor,
                            0, 0, 0,
                            IntPtr.Zero,
                            DI_NORMAL);
                        g.ReleaseHdc(hdcDest);
                    }
                } else {
                    g.ReleaseHdc(hdcDest);
                }
            }

            return m_captureImg;
        }


            /// <summary>
            /// キャプチャ処理
            /// </summary>
            /// <returns></returns>
            /*
            public Bitmap capture()
            {
                using (Graphics g = Graphics.FromImage(m_captureImg))
                {
                    IntPtr hdcDest = g.GetHdc();

                    // デスクトップから画面コピー
                    BitBlt(hdcDest, 0, 0, m_captureSize.Width, m_captureSize.Height,
                           m_hdcSrc, 0, 0,
                           CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

                    g.ReleaseHdc(hdcDest);
                }


                // カーソルを描画（Iビーム含む）
                CURSORINFO ci = new CURSORINFO();
                ci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
                if (GetCursorInfo(out ci) && ci.flags == CURSOR_SHOWING)
                {
                    using (Graphics g = Graphics.FromImage(m_captureImg))
                    {
                        IntPtr hdc = g.GetHdc();

                        if (ci.hCursor == m_hIBeam)
                        {
                            // Iビームカーソルを自前で描く（黒の縦棒）
                            g.ReleaseHdc(hdc);

                            int lineHeight = 20; // 適宜調整
                            g.DrawLine(Pens.Black,
                                       ci.ptScreenPos.x,
                                       ci.ptScreenPos.y - lineHeight / 2,
                                       ci.ptScreenPos.x,
                                       ci.ptScreenPos.y + lineHeight / 2);
                        }
                        else
                        {
                            // 通常カーソルを描画
                            DrawIconEx(hdc,
                                ci.ptScreenPos.x,
                                ci.ptScreenPos.y,
                                ci.hCursor,
                                0, 0, 0,
                                IntPtr.Zero,
                                DI_NORMAL);
                            g.ReleaseHdc(hdc);
                        }
                    }
                }

                return m_captureImg;
            }
            */


        }
    }
