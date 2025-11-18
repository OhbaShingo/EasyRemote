using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace EasyRemote.WebService
{
    /// <summary>
    /// Webサービスクラス
    /// </summary>
    public class WebService
    {
        /// <summary>
        /// ブラウザ側からのマウスイベントデータ
        /// </summary>


        private MainWindow m_window = null;                 // メイン画面
        private System.Net.HttpListener m_listener = null;  // HTTP受信リスナー
        private ushort m_port;                              // ポート                    
        private ushort m_passCode;                          // パスコード
        private bool m_isRunning;                           // サービス動作中
        private bool m_isCaptureRunning;                    // キャプチャ動作中
        private string m_accessIP;                          // 現在アクセス中のクライアントIPアドレス
        private Task m_captureTask = null;                  // キャプチャタスク
        private EventControl m_event;                       // イベント制御
        private byte[] m_captureJpeg = null;                // キャプチャしたJPEG画像
        private bool m_captureOnlyFlg = false;              // キャプチャのみ有効（ブラウザからの操作は禁止)

        private CaptureControl m_captureControl = new CaptureControl(); // キャプチャ処理

        /// <summary>
        /// Webサービスポート
        /// </summary>
        public ushort Port { get {return m_port; } }
        
        /// <summary>
        /// パスコード
        /// </summary>
        public ushort PassCode { get { return m_passCode; } }

        /// <summary>
        /// 表示レート
        /// </summary>
        public ushort ShowRate { get; set; }
        private int ShowInterval
        {
            get { return 1000 / ShowRate; }
        }

        /// <summary>
        /// パスコード有効/無効
        /// </summary>
        public bool IsPasscodeEnable { get; set; }

        
        /// <summary>
        /// 動作中
        /// </summary>
        public bool IsRunnning { get { return m_isRunning; } }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public WebService(MainWindow window)
        {
            m_window = window;
            m_event = new EventControl();
           createPassCode();
        }

        /// <summary>
        /// パスコード生成
        /// </summary>
        private void createPassCode()
        {
            Random random = new Random();
            m_passCode = (ushort)random.Next( 100, 999 );
        }

        
        /// <summary>
        /// リクエスト可能なURLの取得
        /// </summary>
        /// <returns></returns>
        public String getRequestURL(int port)
        {
            // リクエスト可能なURL取得できる場合
            string hostName = System.Net.Dns.GetHostName();
            string ipAddr = getLocalIPv4();
            string url = $"http://{hostName}:{port}";
            if (ipAddr != null)
            {
                url += $" or http://{ipAddr}:{port}";
            }

            return url;
        }

        /// <summary>
        /// IPアドレス取得
        /// </summary>
        /// <returns></returns>
        private string getLocalIPv4()
        {
            string localIP = null;
            foreach (var ip in System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName()).AddressList)
            {
                // IPv4 かつ ループバックアドレス以外
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(ip))
                {
                    localIP = ip.ToString();
                    return localIP;
                    //                    break;
                }
            }
            return null;
        }

        /// <summary>
        /// サービス開始
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public async void start( ushort port, bool showOnlyFlg )
        {
            String url = $"http://+:{port}/";
//            String url = $"http://localhost:{port}/";
            if (m_listener != null) { return; }    // 既に接続済みならtrueを返す

            try
            {
                m_captureOnlyFlg = showOnlyFlg;
                m_listener = new System.Net.HttpListener();
                m_listener.Prefixes.Add(url);
                m_listener.Start();
                m_port = port;  // 接続出来たら現在のポートを保持
                m_isRunning = true;
                LogController.getInstance().errorLog($"Webサーバ起動 URL={url} ");

                while (m_isRunning)
                {                    
                    System.Net.HttpListenerContext context = await m_listener.GetContextAsync();
                    _ = Task.Run(() => ProcessRequest(context));  // 別スレッドで処理
                }
            }
            catch (System.Net.HttpListenerException ex)
            {
                m_listener = null;
                LogController.getInstance().errorLog($"Webサーバ起動失敗 URL={url} Webサービス起動しません");
            }
            catch (System.ObjectDisposedException ex)
            {
                m_listener = null;
                // 破棄オブジェクトにアクセスする例外は無視（アプリ終了時に発生するだけ）
                LogController.getInstance().errorLog($"破棄オブジェクトにアクセスあり 無視します");
            } catch( Exception )
            {
                m_listener = null;
            }
            LogController.getInstance().errorLog( "Webサーバ起動停止 ");

        }


        /// <summary>
        /// 停止
        /// </summary>
        public void stop()
        {   // 処理スレッドを停止する。
            m_isRunning = false;
            m_isCaptureRunning = false;
            m_listener.Stop();
        }

        /// <summary>
        /// リクエスト
        /// </summary>
        /// <param name="context"></param>
        private async void ProcessRequest(System.Net.HttpListenerContext context)
        {
            System.Net.HttpListenerRequest request = context.Request;
            System.Net.HttpListenerResponse response = context.Response;
            string clientIp = request.RemoteEndPoint.Address.ToString();
            bool responseCloseFlg = true;

            if (request.HttpMethod == "GET")
            {
                if( m_accessIP == null)
                {   // 現時点でアクセスいているクライアントがいない場合
                    requestForNotAccess( request, response );
                } else
                {   // 現時点でアクセスしているクライアントがいる場合は、そのクライアントからの要求か確認
                    if( clientIp.Equals( m_accessIP) == true )
                    {
                        // 現時点でアクセスしている場合
                        responseCloseFlg = requestForAccess(request, response);
                    } else
                    {   // 他のPCがアクセスしている場合は アクセス禁止

                        response.StatusCode = 404; // Bad Request
                        using (var writer = new StreamWriter(response.OutputStream))
                        {
                            writer.Write("Access Other PC.......");
                            LogController.getInstance().errorLog($"既に別PCがアクセスしているので 他PC({clientIp}からのアクセス 受け付けません");

                        }
                    }
                }
            }
            else if( request.HttpMethod == "POST")
            {   // POSTメソッド（ブラウザ側のイベント処理)
                if(clientIp.Equals( m_accessIP ) == true)
                {   // アクセスIP
                    requestEvent( request, response );
                } else {
                    LogController.getInstance().errorLog( $"許可していないPC({clientIp}からのイベント受付！！ 受け付けません");
                }
            }


            if (responseCloseFlg == true )
            {   // ここでレスポンスを閉じる場合は閉じる
                response.OutputStream.Close();
            }

        }

        /// <summary>
        /// リクエスト（未アクセス時処理)
        /// </summary>
        /// <param name="request"></param>
        private void requestForNotAccess(System.Net.HttpListenerRequest request, System.Net.HttpListenerResponse  response )
        {
            string clientIp = request.RemoteEndPoint.Address.ToString();
            if (request.Url.AbsolutePath == "/")
            {   // 開始用ﾍﾟｰｼﾞを返す
                LogController.getInstance().infoLog($" 開始ページ表示 IP={clientIp} ");
                if( IsPasscodeEnable == false ) {
                    // パスコード有効の時は パスコード入力画面(start.html)を表示
                    responseHTML( "start.html", response );
                } else
                {   // パスコード無効の場合は即リモート画面表示
                    loginRemoteStart(clientIp, request, response);
                }
            } else if( request.Url.AbsolutePath == "/connect") {

                string? passcode = request.QueryString["passcode"];  // パスコード取得
                if(string.IsNullOrEmpty(passcode) == true || m_passCode.ToString().Equals( passcode ) == false )
                {   // パスコード未指定若しくは一致していない
                    LogController.getInstance().errorLog($" パスコード NG IP={clientIp} PassCode={PassCode}");

                    response.StatusCode = 404; // Bad Request
                    using (var writer = new StreamWriter(response.OutputStream))
                    {
                        writer.Write("Passcode NG.......");
                    }
                } else
                {
                    loginRemoteStart( clientIp, request, response );
                }
            }
        }

        /// <summary>
        /// リモートアクセスログイン成功  開始
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        private void loginRemoteStart(string clientIp, System.Net.HttpListenerRequest request, System.Net.HttpListenerResponse response)
        {
            LogController.getInstance().infoLog($" パスコード OK IP={clientIp} PassCode={PassCode}");

            m_accessIP = request.RemoteEndPoint.Address.ToString(); // パスコードOKのPCのIPアドレスを保持
            m_isCaptureRunning = true;
            m_captureTask = Task.Run(() => { captureTask(); });     // ここでキャプチャタスクを起動
            m_window.Dispatcher.Invoke(() =>
            {
                m_window.showAccessIP(m_accessIP);
            });

            responseHTML("relay.html", response);                   // 一旦中継用のHTMLを渡す（すぐ /でアクセスする)

        }

        /// <summary>
        /// リクエスト（現在アクセス中
        /// </summary>
        /// <param name="request"></param>
        private bool requestForAccess(System.Net.HttpListenerRequest request, System.Net.HttpListenerResponse response)
        {
            bool requestCloseFlg = true;
            if ( request.Url.AbsolutePath == "/")
            {   // リモートアクセス用ページを渡す
                LogController.getInstance().infoLog($"リモートアクセスページ 応答");
                responseHTML("main.html", response);
            } else if(request.Url.AbsolutePath == "/capturejpeg")
            {
                LogController.getInstance().errorLog($"画像応答 開始");
                deliveryCapture( response );
                requestCloseFlg = false;
            } else if(request.Url.AbsolutePath == "/status")
            {   // 状態通知
                string json = $"{{\"ShowOnly\":{(m_captureOnlyFlg ? "true" : "false")}}}";
                byte[] data = System.Text.Encoding.UTF8.GetBytes(json);
                response.ContentType = "application/json";
                response.OutputStream.Write(data, 0, data.Length);
            }
            return requestCloseFlg;
        }
        
        /// <summary>
        /// リクエスト（イベント関連 POSTで請けたものをここで処理)
        /// </summary>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        private async void requestEvent(System.Net.HttpListenerRequest request, System.Net.HttpListenerResponse response)
        {
            if( m_captureOnlyFlg == true )
            {   // キャプチャのみの場合はイベント処理しない。
                return;
            }

            if (request.HasEntityBody) // <== まずボディがあるかチェック
            {
                // Content-Lengthがない場合に備えて、読み取りはtry-catchで囲む
                try
                {
                    StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    var body = await reader.ReadToEndAsync();

                    // JSON処理...
                    EventControl.InputMouseData data = JsonSerializer.Deserialize<EventControl.InputMouseData>(body);
                    m_event.sendMouseEvent(data);

                }
                catch (Exception ex)
                {
                    // iOS/Chromeで発生した例外をここで捕捉し、ログに記録する
                    Console.WriteLine($"Error reading request body: {ex.Message}");
                    response.StatusCode = 400; // Bad Request (または 500 Internal Error)
                    return;
                }
            }
        }

        /// <summary>
        /// HTMLファイルの応答
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="response"></param>
        private async void responseHTML(string fileName, System.Net.HttpListenerResponse response )
        {
            string htmlContent = System.IO.File.ReadAllText("html/" + fileName);
            byte[] htmlByte = System.Text.Encoding.UTF8.GetBytes(htmlContent);

            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = htmlByte.Length;
            await response.OutputStream.WriteAsync(htmlByte, 0, htmlByte.Length);
        }
        
        static int WAIT_TIME = 200;

        /// <summary>
        /// キャプチャタスク
        /// </summary>
        private void captureTask()
        {
            MemoryStream[] streams = new MemoryStream[2];   // JPEG画像メモリキャッシュ用
            int index = 0;
            long delayTime = 0;

            System.Diagnostics.Stopwatch procSW = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch captureSW = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch jpegSW = new System.Diagnostics.Stopwatch();

            System.Diagnostics.Stopwatch notifySW = new System.Diagnostics.Stopwatch();
            notifySW.Start();
            long notifyTime = -1;

            while (m_isCaptureRunning)
            {

                procSW.Start();
                captureSW.Start();

                Bitmap captureImg = m_captureControl.capture(); // キャプチャ処理
                captureSW.Stop();

                if (streams[index] != null )
                {   // 前回ストリーム破棄
                    streams[index].Dispose();
                }
                streams[index] = new MemoryStream();

                jpegSW.Start();
                captureImg.Save(streams[index], ImageFormat.Jpeg);    // JPEG生成
                lock( m_captureControl )
                {
                    m_captureJpeg = streams[index].ToArray();
                }
                jpegSW.Stop();
                procSW.Stop();

                index++;
                if( index > 1 ) { index = 0; }

                long waitTime = (long)ShowInterval - procSW.ElapsedMilliseconds;
                if( waitTime < 10 ) { waitTime = 10; }
               // LogController.getInstance().infoLog( $"キャプチャ処理時間{procSW.ElapsedMilliseconds} キャプチャ{captureSW.ElapsedMilliseconds} JPEG{jpegSW.ElapsedMilliseconds} 待機時間={waitTime}");
                long procTime = procSW.ElapsedMilliseconds;

                procSW.Reset();
                captureSW.Reset();
                jpegSW.Reset();

                long tempNotifyTime = notifySW.ElapsedMilliseconds / 1000;
                if(notifyTime != tempNotifyTime)
                {
                    notifyTime = tempNotifyTime;
                    m_window.Dispatcher.BeginInvoke(() =>
                    {
                        m_window.showNowRate((int)procTime, (int)( 1000 / (procTime + 10) ) );
                    });
                }

                System.Threading.Thread.Sleep((int)waitTime);

            }

            m_captureTask = null;
        }

        /// <summary>
        /// キャプチャ画像の配信
        /// </summary>
        /// <param name="response"></param>
        private void  deliveryCapture( System.Net.HttpListenerResponse response )
        {
            response.ContentType = "multipart/x-mixed-replace; boundary=--frame";
            response.SendChunked = true;

            _ = Task.Run(async () =>
            {
                try
                {
                    using ( var output = response.OutputStream )
                    {   // 
                        while (m_isCaptureRunning && m_isRunning )
                        {
                            byte[] jpeg = null;
                            lock ( m_captureControl )
                            {
                                jpeg = m_captureJpeg;
                            }
                            if (jpeg != null)
                            {
                                string header = "\r\n--frame\r\nContent-Type: image/jpeg\r\nContent-Length: " + jpeg.Length + "\r\n\r\n";
                                byte[] headerBytes = Encoding.ASCII.GetBytes(header);

                                await output.WriteAsync(headerBytes, 0, headerBytes.Length);
                                await output.WriteAsync(jpeg, 0, jpeg.Length);
                                await output.FlushAsync();
                            }
                            await Task.Delay(ShowInterval);
                        }
                    }
                } catch(Exception ex )
                {
                    // 例外が発生した場合、別ページに行ったとみなし リモートディスクトップを停止したとみなす
                    m_accessIP = null;
                    m_isCaptureRunning = false;
                    LogController.getInstance().errorLog($"画像応答終了");

                }
                response.Close();
            } );
        }
    }
}
