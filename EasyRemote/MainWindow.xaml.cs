using System.Printing;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace EasyRemote
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dialog.LogWindow m_logWindow = null;
        private WebService.WebService m_webService;
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// フォームロード完了
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onLoaded(object sender, RoutedEventArgs e)
        {
            m_webService = new WebService.WebService(this);
            m_webService.ShowRate = Properties.Settings.Default.ShowRate;
            m_nowAccessPanel.Visibility = Visibility.Hidden;
            // ポートを設定
            ushort webPort = Properties.Settings.Default.WebPort;
            m_accessPortTxt.Text = webPort.ToString();
            m_showRateTxt.Text = Properties.Settings.Default.ShowRate.ToString();

            showAccessURL();
            showPassCode();
        }

        /// <summary>
        /// 終了前処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        // 終了時処理
        private void onClosed(object sender, EventArgs e)
        {
            if(m_webService.Port != 0) {
                Properties.Settings.Default.WebPort = m_webService.Port;
            }

            Properties.Settings.Default.Save(); // プロパティの保存
            if(m_logWindow != null)
            {
                m_logWindow.Close();
            }
        }

        /// <summary>
        /// アクセスポートテキストフォーカス喪失
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onAccessPortLostFocus(object sender, RoutedEventArgs e)
        {
            string txt = m_accessPortTxt.Text;
            ushort port = 0;
            if (ushort.TryParse( txt, out port ) == false )
            {   // 数字以外のモノを入力した場合は現在保存されているポート番号に戻す
                m_accessPortTxt.Text = Properties.Settings.Default.WebPort.ToString();
            }
            showAccessURL();
        }
    
        /// <summary>
        /// 表示レートテキスト フォーカス喪失
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onShowRateTxt(object sender, RoutedEventArgs e)
        {
            string txt = m_showRateTxt.Text;
            byte rate = 0;
            if (byte.TryParse(txt, out rate) == false)
            {   // 数字以外のモノを入力した場合は現在保存されているポート番号に戻す
                m_showRateTxt.Text = Properties.Settings.Default.ShowRate.ToString();
            }
            m_webService.ShowRate = rate;
        }


        /// <summary>
        /// アクセスURLの表示
        /// </summary>
        private void showAccessURL()
        {
            string txt = m_accessPortTxt.Text;
            ushort port;
            if (ushort.TryParse(txt, out port) == false)
            {
                port = Properties.Settings.Default.WebPort;
            }
            string url = m_webService.getRequestURL( port );
            m_accessURLText.Text = url;
        }

        /// <summary>
        /// パスコードの表示
        /// </summary>
        private void showPassCode()
        {
            m_passcodeText.Text = m_webService.PassCode.ToString();
        }

        /// <summary>
        /// Webサービス開始/終了ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onStartEndButton(object sender, RoutedEventArgs e)
        {
            if( m_webService.IsRunnning == false )
            {   // 現在Webサービス未動作中の場合
                ushort port = ushort.Parse( m_accessPortTxt.Text.ToString() );
                m_webService.start( port, ( m_showOnlyCheck.IsChecked == true ) );
                m_showOnlyCheck.IsEnabled = false;
                if(m_webService.IsRunnning == true )
                {
                    m_startEndButton.Content = "終了";
                }

            } else
            {
                m_webService.stop();
                m_showOnlyCheck.IsEnabled = true;
                m_startEndButton.Content = "開始";
                m_nowAccessPanel.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// アクセス中IPアドレス
        /// </summary>
        /// <param name="accessIP"></param>
        public void showAccessIP( string accessIP )
        {
            m_nowAccessPanel.Visibility = Visibility.Visible;
            m_nowAccessIpLabel.Content = accessIP;
        }

        /// <summary>
        /// 現在の処理時間
        /// </summary>
        /// <param name="procTime"></param>
        public void showNowRate( int procTime, int fps )
        {
            string text = procTime.ToString() + "ms(予想最高FPS=" + fps + ")";
            m_nowFPSLabel.Content = text;
        }


        /// <summary>
        /// ログ画面表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onLogButton(object sender, RoutedEventArgs e)
        {
            if(m_logWindow == null )
            {
                m_logWindow = new Dialog.LogWindow( this );
            }
            m_logWindow.Show();
        }
        
        /// <summary>
        /// ログ画面閉じる通知
        /// </summary>
        public void notifyLogWindowClose()
        {
            m_logWindow = null;
        }

        /// <summary>
        /// パスコード有効/無効チェック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onPasscodeEnableChecked(object sender, RoutedEventArgs e)
        {
            if( m_passcdeEnableCheck.IsChecked == true )
            {
                m_webService.IsPasscodeEnable = true;
                m_passcodeText.Visibility = Visibility.Hidden;
            } else
            {
                m_webService.IsPasscodeEnable = false;
                m_passcodeText.Visibility = Visibility.Visible;

            }
        }


    }
}