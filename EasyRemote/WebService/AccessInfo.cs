using QRCoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using static System.Net.Mime.MediaTypeNames;

namespace EasyRemote.WebService
{
    /// <summary>
    /// アクセス情報取得
    /// </summary>
    public class AccessInfo
    {
        private static List<AccessInfo> m_accessInfoList = null;          // ネットワーク情報一覧
        private System.Net.NetworkInformation.NetworkInterface    m_info; // ネットワーク情報

        
        /// <summary>
        /// アクセス情報一覧取得
        /// </summary>
        /// <returns></returns>
        public static List<AccessInfo> getAccessInfos()
        {
            if(m_accessInfoList != null) { return m_accessInfoList;}

            m_accessInfoList = new List<AccessInfo>();

            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (System.Net.NetworkInformation.NetworkInterface nic in nics)
            {
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {   // まずはWifiカード検索
                    m_accessInfoList.Add( new AccessInfo( nic ) );
                }
            }
            foreach (System.Net.NetworkInformation.NetworkInterface nic in nics)
            {
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    nic.Description.ToLower().Contains("bluetooth"))
                {
                    m_accessInfoList.Add(new AccessInfo(nic));
                }
            }
            foreach (System.Net.NetworkInformation.NetworkInterface nic in nics)
            {
                if (nic.OperationalStatus == OperationalStatus.Up &&
                     nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {   // 次に有線LAN検索
                    m_accessInfoList.Add(new AccessInfo(nic));
                }
            }
            return m_accessInfoList;
        }



        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="info"></param>
        private AccessInfo(System.Net.NetworkInformation.NetworkInterface info)
        {
            m_info = info;
        }


        /// <summary>
        /// IPアドレス取得
        /// </summary>
        /// <returns></returns>
        private string getIpAddress()
        {
            IPInterfaceProperties ipProps = m_info.GetIPProperties();
            UnicastIPAddressInformationCollection addrs = ipProps.UnicastAddresses;

            foreach (UnicastIPAddressInformation addr in addrs)
            {
                // IPv4 のみ
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    // ループバック 127.0.0.1 は除外
                    if (!IPAddress.IsLoopback(addr.Address))
                    {
                        return addr.Address.ToString();
                    }
                }
            }
            return "";
        }


        /// <summary>
        /// IPアドレスでのアクセスURL取得
        /// </summary>
        /// <returns></returns>
        public string getURLForIp( string port )        {
            return $"http://{getIpAddress()}:{port}";
        }

        /// <summary>
        /// アクセスQRコード作成
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public BitmapImage createURLCode( string port )
        {
            string url = getURLForIp( port );

            QRCodeGenerator generator = new QRCodeGenerator();
            QRCodeData data = generator.CreateQrCode( url, QRCodeGenerator.ECCLevel.Q);
            QRCode qr = new QRCode(data);
            Bitmap bmp = qr.GetGraphic(20);  // ← 20 はドットの大きさ（適宜変更）

            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();   // ← これ重要（UIスレッド外でも安全化）

                return image;
            }
        }

        /// <summary>
        /// Nameの取得
        /// </summary>
        public string Name { get { return m_info.Name; } }

        /// <summary>
        /// ネットワークカード文字列取得
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return m_info.Name + " " + m_info.Description;
        }
    }
}
