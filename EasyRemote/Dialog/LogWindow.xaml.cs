using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EasyRemote.Dialog
{
        /// <summary>
        /// LogWindow.xaml の相互作用ロジック
        /// </summary>
        public partial class LogWindow : Window
        {
            /**********************************
             * メンバ変数定義
             **********************************/
            private MainWindow m_parentWindow;          // 親Window
            private LogController m_logController;      // ログ管理

            /**********************************
             * メソッド定義
             **********************************/
            public LogWindow(MainWindow parent)
            {
                m_parentWindow = parent;
                m_logController = LogController.getInstance();
                InitializeComponent();
            }

            /// <summary>
            /// ダイアログ　Load完了
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void onLoaded(object sender, RoutedEventArgs e)
            {
               // DataClass.AppSettingData.getInstance().loadLogWindowSize(this);

                string[] allLog = m_logController.getAllLog();
                for (int i = 0; i < allLog.Length; i++)
                {
                    m_LogListBox.Items.Add(allLog[i]);
                }
                LogController.getInstance().setLogWindow(this);
            }

            /// <summary>
            /// Dialog閉じるイベント
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void onClosed(object sender, EventArgs e)
            {
                LogController.getInstance().setLogWindow(null);
                m_parentWindow.notifyLogWindowClose();
            }

            /// <summary>
            /// ログクリアボタン
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void onClearButton(object sender, EventArgs e)
            {
                LogController.getInstance().clearLog(); // ログクリア
                m_LogListBox.Items.Clear();             // 画面上からもクリア
            }

            /// <summary>
            /// ログ追加通知
            /// </summary>
            /// <param name="log"></param>
            public void notifyAddLog(string log)
            {
                if (IsLoaded == true && IsVisible == true)
                {   // 別スレッドから通知されるため、念の為Dialogがクローズされていないか確認する
                    m_LogListBox.Items.Insert(0, log);
                }
            }

            static private int debugCnt = 0;
            /// <summary>
            /// デバッグ用ボタン　クリック
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void onDebugClick(object sender, RoutedEventArgs e)
            {
                m_logController.infoLog($"AAA{debugCnt}");
                debugCnt++;


            }

            /// <summary>
            /// 閉じるボタン　クリック
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void onOK(object sender, RoutedEventArgs e)
            {
                Close();
            }


        }
}
