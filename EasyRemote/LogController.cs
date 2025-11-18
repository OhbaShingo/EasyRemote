using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyRemote
{
    /// <summary>
    /// ログ管理クラス（Singletonクラス　　getInstanceでインスタンスを取得して使用する
    /// </summary>
    internal class LogController
    {
        private const string DEFAULT_LOG_PATH = "Log";    // デフォルトログパス
        public const string LOG_EXTENSION = ".log";        // ログファイル　拡張子


        /// <summary>
        /// ログ出力レベル
        /// </summary>
        private enum LogLevel
        {
            DEBUG,      // デバッグログ
            INFO,       // 通常ログ
            WARNING,    // 連続動作可能なレベルの警告
            ERROR,      // 停止する必要性があるレベルでのエラー
            ASSERT      // バグ等予期しない異常(アプリさせるべき不具合）
        };

        private struct LogInfo
        {
            public LogLevel lebel;   // ログレベル
            public DateTime time;    // 発生時間
            public string log;     // ログ内容
        };

        /// <summary>
        /// ログ追加通知
        /// ※　logWindowに通知する為のデリゲータ　
        /// </summary>
        /// <param name="log">追加ログ</param>
        private delegate void NotifyAddLog(string log);


        private static LogController m_logController;   // ログコントローラ
        private LogInfo[] m_logInfo;                    // ログバッファ
        private Dialog.LogWindow m_logWindow;           // ログWindow

        private bool m_logLoop;                         // ログ 最大件数を超えたか否か
        private int m_nextLogIdx;                       // 次のログ保存位置

 //       private string m_saveFolder;                    // ログ出力先フォルダ
        private System.IO.StreamWriter m_realLogSW;     // リアルログ出力ストリーマー

        /// <summary>
        /// ログ管理オブジェクト取得
        /// </summary>
        /// <returns></returns>
        public static LogController getInstance()
        {
            if( m_logController == null )
            {
                m_logController = new LogController(2000);
            }
            return m_logController;
        }


        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="size">ログ保存件数</param>
        private LogController(int size)
        {
            //xsize = 5;
            clearLog(size);
            m_realLogSW = null;
        }

        /// <summary>
        /// ログDialog設定
        /// </summary>
        /// <param name="window">ログDialog</param>
        public void setLogWindow( Dialog.LogWindow window )
        {
            Debug.WriteLine("閉じる開始");
            lock (this) {
                Debug.WriteLine("閉じる開始1");
                m_logWindow = window;
            }
            Debug.WriteLine("閉じる終了");
        }

        /// <summary>
        /// ログクリア
        /// </summary>
        /// <param name="size">ログのサイズ  0の時は、サイズ変更なし</param>
        public void clearLog(int size = 0)
        {
            if (size != 0)
            {
                m_logInfo = new LogInfo[size];
            }

            m_logLoop = false;
            m_nextLogIdx = 0;
        }

        /// <summary>
        /// 現在の日付・時刻文字列取得（ファイル名用)
        /// </summary>
        /// <returns></returns>
        static public string getNowTimeText()
        {
            DateTime dt = DateTime.Now;
            string timeText = $"{dt.Year,4}{dt.Month,2}{dt.Day,2}{dt.Hour,2}{dt.Minute,2}{dt.Second,2}";
            return timeText;
        }


        /// <summary>
        /// 全ログ取得
        /// </summary>
        /// <returns></returns>
        public string[] getAllLog( )
        {
            string[] allLog;
            lock (this)
            {
                if (m_logLoop == true)
                {
                    allLog = new string[m_logInfo.Length];
                }
                else
                { 
                    allLog = new string[m_nextLogIdx];
                }

                int i = 0;
                int j;
                int lastIdx = m_nextLogIdx - 1;
                if (m_nextLogIdx >= 0)
                {
                    for (j = lastIdx; j >= 0; j--)
                    {   // 最後に出力したログから、さかのぼって出力
                        allLog[i] = getOutLog(j);
                        i++;
                    }
                }
                if (m_logLoop == true)
                {   // バッファサイズ以上に出力している場合
                    for (j = m_logInfo.Length - 1; j >= m_nextLogIdx; j--)
                    {
                        allLog[i] = getOutLog(j);
                        i++;
                    }
                }
            }
            return allLog;
        }



        /// <summary>
        /// ログ出力
        /// </summary>
        /// <param name="lebel"></param>
        /// <param name="msg"></param>
        private void logOut(LogLevel lebel, string msg)
        {
            lock (this)
            {
                int logIdx = m_nextLogIdx;
                m_logInfo[m_nextLogIdx].lebel = lebel;
                m_logInfo[m_nextLogIdx].time = DateTime.Now;
                m_logInfo[m_nextLogIdx].log = msg;

                if (m_realLogSW != null)
                {   // リアルログ出力時は、、ログを保存
                    string outlog = getOutLog(m_nextLogIdx);
                    m_realLogSW.WriteLine(outlog);
                    m_realLogSW.Flush();
                }

                m_nextLogIdx++;
                if (m_logInfo.Length <= m_nextLogIdx)
                {   // 次のログ保存位置が、ログ保存最大件数を超えたら、 0から保存
                    m_nextLogIdx = 0;
                    m_logLoop = true;
                }

                if ( m_logWindow != null)
                {   // ログWindowが存在していたら、通知する(
                    try
                    {
                        NotifyAddLog notifyAddLog = m_logWindow.notifyAddLog;
                        string log = getOutLog(logIdx);
                        m_logWindow.Dispatcher.BeginInvoke(notifyAddLog, log );
                    }
                    catch (Exception)
                    {
                    }
                }

            }
        }



        /// <summary>
        /// ログ出力文字列 作成
        /// </summary>
        /// <param name="idx">ログ位置</param>
        /// <returns></returns>
        private string getOutLog(int idx)
        {
            string log;
            string lebelTxt = null;
            switch (m_logInfo[idx].lebel)
            {
                case LogLevel.DEBUG:
                    lebelTxt = "DEBUG   ";
                    break;
                case LogLevel.INFO:
                    lebelTxt = "INFO    ";
                    break;
                case LogLevel.WARNING:
                    lebelTxt = "WARNING ";
                    break;
                case LogLevel.ERROR:
                    lebelTxt = "ERROR   ";
                    break;
                case LogLevel.ASSERT:
                    lebelTxt = "ASSERT  ";
                    break;
            }

            log = "[" + $"{m_logInfo[idx].time.Hour:D2}:{m_logInfo[idx].time.Minute:D2}:{m_logInfo[idx].time.Second:D2}.{m_logInfo[idx].time.Millisecond:D3} " + lebelTxt + "]" + m_logInfo[idx].log;
            return log;
        }

        /// <summary>
        ///     デバッグログ出力
        /// </summary>
        /// <param name="msg">ログ内容</param>
        public void debugLog(string msg)
        {
        }

        /// <summary>
        ///     通常ログ出力
        /// </summary>
        /// <param name="msg">ログ内容</param>
        public void infoLog(string msg)
        {
            logOut(LogLevel.INFO, msg);
        }

        /// <summary>
        ///     WARNINGログ出力
        /// </summary>
        /// <param name="msg">ログ内容</param>
        public void warningLog(string msg)
        {
            logOut(LogLevel.WARNING, msg);
        }

        /// <summary>
        ///     ERRORログ出力
        /// </summary>
        /// <param name="msg">ログ内容</param>
        public void errorLog(string msg)
        {
            logOut(LogLevel.ERROR, msg);
        }

        /// <summary>
        ///     ASSERTログ出力
        /// </summary>
        /// <param name="msg">ログ内容</param>
        public void assertLog(string msg)
        {
            logOut(LogLevel.ASSERT, msg);
            System.Diagnostics.Debug.Assert(false, msg); // アサート
        }
    }
}

