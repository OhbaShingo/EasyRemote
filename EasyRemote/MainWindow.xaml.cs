using EasyRemote.WebService;
using System.Printing;
using System.Security.Policy;
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

            // パスチェック有効/無効
            bool isPasscheck = Properties.Settings.Default.IsPasscheck;
            m_passcdeEnableCheck.IsChecked = isPasscheck;

#if !DEBUG
            m_showOnlyLabel.Visibility = Visibility.Hidden;
            m_showOnlyCheck.Visibility = Visibility.Hidden;
#endif

            checkPortEnable( webPort );
            showNetworkCardList();            
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
            Properties.Settings.Default.IsPasscheck = ( m_passcdeEnableCheck.IsChecked == true );

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
                port = Properties.Settings.Default.WebPort;
                m_accessPortTxt.Text = port.ToString();
            }

            checkPortEnable( port );
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
        /// ネットワークカード一覧表示
        /// </summary>
        private void showNetworkCardList()
        {
            List<AccessInfo> cardList = m_webService.getNetworkCardList();
            foreach(AccessInfo card in cardList )
            {
                m_cardCombo.Items.Add( card );
            }
            m_cardCombo.SelectedIndex = 0;
        }

        /// <summary>
        /// 指定ポートの有効/無効チェック
        /// </summary>
        /// <param name="port"></param>
        private void checkPortEnable( int port )
        {
            if( m_webService.isEnableWebPort( port ) == true)
            {
                m_portEnableButton.Content = "ポート有効済";
            }
            else
            {
                m_portEnableButton.Content = "ポート有効化";
            }
        }


        /// <summary>
        /// アクセスURLの表示
        /// </summary>
        private void showAccessURL(  )
        {
            string txt = m_accessPortTxt.Text;
            ushort port;
            if (ushort.TryParse(txt, out port) == false)
            {
                port = Properties.Settings.Default.WebPort;
            }

            AccessInfo info = (AccessInfo)m_cardCombo.SelectedItem;
            m_accessURLText.Text = info.getURLForIp( port.ToString() );
            if (m_webService.IsRunnning == true )
            {   // アクセス可能の時はQRコードを表示
                m_accessQrImage.Visibility = Visibility.Visible;
                m_accessQrImage.Source = info.createURLCode( port.ToString() );
            } else
            {   // アクセス不可能の時はQRコードを非表示
                m_accessQrImage.Visibility = Visibility.Collapsed;
            }
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
                    m_startEndButton.Content = "モニタ終了";

                    showAccessURL(  );

                }

            } else
            {
                m_webService.stop();
                m_showOnlyCheck.IsEnabled = true;
                m_startEndButton.Content = "モニタ開始";
                m_nowAccessPanel.Visibility = Visibility.Hidden;
                showAccessURL();
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

        /// <summary>
        /// 選択中のカード変更イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onCardComboSelectChange(object sender, SelectionChangedEventArgs e)
        {
            showAccessURL();
        }

        /// <summary>
        /// ポート有効ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void onPortEnableButton(object sender, RoutedEventArgs e)
        {
            int port = int.Parse(m_accessPortTxt.Text);
            m_webService.enableWebPort( port );
            checkPortEnable( port );
        }
    }
}