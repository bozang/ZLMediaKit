#define useRedis
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Net;
using Nancy.Hosting.Self;
using Nancy;
using System.Runtime.InteropServices;

namespace CSharpServer
{
    /// <summary>
    /// ��ý������
    /// </summary>
    public class MediaServerConfig
    {
        /// <summary>
        /// ��ý��ip
        /// </summary>
        public string Ipaddress = "127.0.0.1";
        /// <summary>
        /// ��ý��http�˿�
        /// </summary>
        public ushort mk_http_port = 8500;
        /// <summary>
        /// ��ý��rtsp�˿�
        /// </summary>
        public ushort mk_rtsp_port = 554;
        /// <summary>
        /// ��ý��rtmp�˿�
        /// </summary>
        public ushort mk_rtmp_port = 1935;
        /// <summary>
        /// ������Ȩ��
        /// </summary>
        public int weight;
    }
    class Program
    {
        private static string url = "http://localhost";
        private static int httpServerPort = 8502;
        private static NancyHost nancyHost;

        private static string redisAddress = "127.0.0.1";
        private static int redisPort = 6379;
        static MediaServerConfig mediaServerConfig;

        static on_mk_media_changed media_Changed;
        static on_mk_flow_report flow_Report;
        static on_mk_media_source_find_cb media_Source_Find_Cb;
        private static string smsMsg;
        /// <summary>
        /// ��ý�屣��ʱ��
        /// </summary>
        private static int mediaKeepLiveSecond;
        public static void del_on_mk_media_changed(int regist, ref IntPtr sender)
        {
            try
            {
                if (regist == 1)//ע��
                {
                    string schema = Marshal.PtrToStringAnsi(MediaServer.mk_media_source_get_schema(ref sender));
                    if (schema == "rtsp")//hls������������ע��ص���3�룬�������ֹͣ�ӿڹ��죬�����hlsע��ص�������ʱ�����½�ֹͣ�ӿ�ɾ����stream���뵽redis�У��˴�����rtsp�ж������������
                    {
                        string vhost = Marshal.PtrToStringAnsi(MediaServer.mk_media_source_get_vhost(ref sender));
                        string app = Marshal.PtrToStringAnsi(MediaServer.mk_media_source_get_app(ref sender));
                        string stream = Marshal.PtrToStringAnsi(MediaServer.mk_media_source_get_stream(ref sender));
                        int readCount = MediaServer.mk_media_source_get_total_reader_count(ref sender);
                        IntPtr trackinfos = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(TrackInfos)) * 2);
                        //IntPtr trackinfos = IntPtr.Zero;
                        int trackCount = MediaServer.mk_media_source_get_tracks(ref sender, trackinfos);
                        TrackInfos[] infos = new TrackInfos[trackCount];
                        for (int i = 0; i < trackCount; i++)
                        {
                            infos[i] = (TrackInfos)(Marshal.PtrToStructure(trackinfos + i * Marshal.SizeOf(typeof(TrackInfos)), typeof(TrackInfos)));
                        }


                        string redisValue = mediaServerConfig.Ipaddress + "_" + mediaServerConfig.mk_http_port + "_" + app + "_" + stream;


                        Task task = Task.Factory.StartNew
                            (
                            new Action(() =>
                            {
                                if (app != "rtp")
                                {
                                    stream = app + "_" + stream;
                                }
                                //else
                                {
                                    if (!RedisHelper.GetDatabase().HashExists("live", stream))
                                    {
                                        RedisHelper.GetDatabase().HashSet("live", new StackExchange.Redis.HashEntry[] { new StackExchange.Redis.HashEntry(stream, redisValue) });
                                    }
                                }
                            }
                            )
                            );

                        //Action action = new Action(() =>
                        //{
                        //    if (app != "rtp")
                        //    {
                        //        stream = app + "_" + stream;
                        //    }
                        //    //else
                        //    {
                        //        if (!RedisHelper.GetDatabase().HashExists("live", stream))
                        //        {
                        //            RedisHelper.GetDatabase().HashSet("live", new StackExchange.Redis.HashEntry[] { new StackExchange.Redis.HashEntry(stream, redisValue) });
                        //        }
                        //    }

                        //}
                        //    );
                        //action.BeginInvoke(null, null);

                        //Task.Run(() => media_source_for_each());
                    }

                }
                else//��ע��
                {
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
        public static string GetKey(string app, string stream)
        {
            return app + "_" + stream;
        }
        static Dictionary<string, int> dic_readerCount = new Dictionary<string, int>();
        public static void del_on_mk_media_source_find_cb(IntPtr userdata, ref IntPtr mk_media_source)
        {
            try
            {
                //IntPtr ptr = mk_media_source;
                string schema = Marshal.PtrToStringAnsi(MediaServer.mk_media_source_get_schema(ref mk_media_source));
                if (schema!="rtsp")
                {
                    return;
                }
                string vhost = Marshal.PtrToStringAnsi(MediaServer.mk_media_source_get_vhost(ref mk_media_source));
                string app = Marshal.PtrToStringAnsi(MediaServer.mk_media_source_get_app(ref mk_media_source));
                string stream = Marshal.PtrToStringAnsi(MediaServer.mk_media_source_get_stream(ref mk_media_source));
                int readCount = MediaServer.mk_media_source_get_total_reader_count(ref mk_media_source);
                string key = GetKey(app, stream);
                if (!dic_readerCount.ContainsKey(key))
                {
                    dic_readerCount.Add(key, readCount);
                }
                else
                {
                    if (dic_readerCount[key]==0 && readCount==0)
                    {
                        string ret = DoMedia.Stop(app,stream);
                    }
                    else
                    {
                        dic_readerCount[key] = readCount;
                    }
                }
                //Thread thread = new Thread(new ParameterizedThreadStart(GetTotalReaderCount));
                //thread.Start(mk_media_source);
                //Task.Run(() => GetTotalReaderCount(ref ptr));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
        public static void media_source_no_reader_check()
        {
            try
            {
                bool is_on = System.Configuration.ConfigurationManager.AppSettings["no_reader_close"] == "1" ? true : false;
                if (!is_on)
                {
                    return;
                }
                int sleep_millisecond = int.Parse(System.Configuration.ConfigurationManager.AppSettings["no_reader_check_second"]);
                while (true)
                {
                    MediaServer.mk_media_source_for_each(IntPtr.Zero, media_Source_Find_Cb);
                    Thread.Sleep(sleep_millisecond);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }
        //public static void GetTotalReaderCount(object mk_media_source)
        //{
        //    IntPtr ptr = (IntPtr)mk_media_source;
        //    Thread.Sleep(1000);
        //    int readCount = MediaServer.mk_media_source_get_total_reader_count(ref ptr);
        //}

        public static void del_on_mk_flow_report(ref IntPtr mk_media_info, UInt64 total_bytes, UInt64 total_seconds, int is_player, ref IntPtr mk_sock_info)
        {
            try
            {
                string vhost = Marshal.PtrToStringAnsi(MediaServer.mk_media_info_get_vhost(ref mk_media_info));
                string schema = Marshal.PtrToStringAnsi(MediaServer.mk_media_info_get_schema(ref mk_media_info));
                string app = Marshal.PtrToStringAnsi(MediaServer.mk_media_info_get_app(ref mk_media_info));
                string stream = Marshal.PtrToStringAnsi(MediaServer.mk_media_info_get_stream(ref mk_media_info));
                //MediaServer.mk_media_source_find(schema, vhost, app, stream, IntPtr.Zero, media_Source_Find_Cb);
                //int readCount = MediaServer.mk_media_info_get_total_reader_count(ref mk_media_info);
                //StringBuilder stringBuilder = new StringBuilder(64);
                //string peer_ip = Marshal.PtrToStringAnsi(MediaServer.mk_sock_info_peer_ip(ref mk_sock_info,ref stringBuilder));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        

        static void RegisterEvent()
        {
            Events events = new Events();
            int size = Marshal.SizeOf(typeof(Events));
            media_Changed = new on_mk_media_changed(del_on_mk_media_changed);
            flow_Report = new on_mk_flow_report(del_on_mk_flow_report);
            events.IntPtrOn_mk_media_changed = media_Changed;
            events.IntPtrOn_mk_flow_report = flow_Report;

            media_Source_Find_Cb = new on_mk_media_source_find_cb(del_on_mk_media_source_find_cb);

            MediaServer.mk_events_listen(ref events);
        }
        static void Main(string[] args)
        {
            //test
            //Test1();

            //rtspԴ
            //AddStreamProxyTest();
            //�ӿ�
            StartMain();
            //SDK
            //SDKTest();
        }
        static void Test1()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
            MediaServer.Config config = new MediaServer.Config();
            config.ini_is_path = 1;
            config.ini = configPath;
            MediaServer.mk_env_init(ref config);
            uint ret;
            ret = MediaServer.mk_http_server_start(8500, 0);
            ret = MediaServer.mk_rtsp_server_start(554, 0);
            ret = MediaServer.mk_rtmp_server_start(1935, 0);
            DoMedia.StartPlaySDK("fh", "sdklive1", "", "10.128.24.60", "admin", "0123456a?", "8000");
            Console.Read();
        }
        private static void StartMain()
        {
            try
            {
                mediaServerConfig = new MediaServerConfig();
                mediaServerConfig.Ipaddress = System.Configuration.ConfigurationManager.AppSettings["sms_ipaddress"];
                mediaServerConfig.weight = int.Parse(System.Configuration.ConfigurationManager.AppSettings["weight"]);
                redisAddress = System.Configuration.ConfigurationManager.AppSettings["cms_ipaddress"];
                mediaKeepLiveSecond = int.Parse(System.Configuration.ConfigurationManager.AppSettings["media_keeplive_second"]);
                smsMsg = "sms_" + mediaServerConfig.Ipaddress + "_" + mediaServerConfig.weight;
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
                DoMedia.MEDIA_PORT_START_STATIC = int.Parse(System.Configuration.ConfigurationManager.AppSettings["udp_start_port"]);
                DoMedia.MEDIA_PORT_START = int.Parse(System.Configuration.ConfigurationManager.AppSettings["udp_start_port"]);
                DoMedia.MEDIA_PORT_END = int.Parse(System.Configuration.ConfigurationManager.AppSettings["udp_end_port"]);
                ushort ret = MediaInit(configPath);
                RegisterEvent();
                Task.Run(() => media_source_no_reader_check());
                //AddStreamProxy();
#if useRedis
                InitRedis();
                try
                {
                    RedisHelper.GetDatabase().StringSet(smsMsg, httpServerPort);
                    Task.Factory.StartNew(new Action(KeepLive));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("δ��������redis����֧�ֲ��ֹ��ܲ���");
                }
                string str = url + ":" + httpServerPort + "/";
                nancyHost = new NancyHost(new Url(str));
                nancyHost.Start();
                Console.WriteLine("��ʼ��������˿ڣ�" + httpServerPort);
#else
                ret = MediaServer.mk_rtp_server_start(20000, "34020000001320000001");
#endif
                Console.Read();
                MediaServer.mk_stop_all_server();
#if useRedis
                nancyHost.Stop();
#endif
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                Console.Read();
            }
        }

        ///// <summary>
        ///// ��ȡ��ý�����õ�IP
        ///// </summary>
        ///// <param name="configPath"></param>
        ///// <returns></returns>
        //private static string GetLocalIp(string configPath)
        //{
        //    string ip = "";
        //    using (StreamReader sr = new StreamReader(configPath))
        //    {
        //        while (!sr.EndOfStream)
        //        {
        //            string str = sr.ReadLine();
        //            if (str.Contains("ipAddress"))
        //            {
        //                ip = str.Split('=')[1];
        //                break;
        //            }
        //        }
        //    }
        //    return ip;
        //}
        ///// <summary>
        ///// ��ȡ��ý�����õĶ˿ڣ�����flv��hls
        ///// </summary>
        ///// <param name="configPath"></param>
        ///// <returns></returns>
        //private static int GetMediaHttpPort(string configPath)
        //{
        //    int port=-1;
        //    using (StreamReader sr = new StreamReader(configPath))
        //    {
        //        while (!sr.EndOfStream)
        //        {
        //            string str = sr.ReadLine();
        //            if (str.Contains("httpPort"))
        //            {
        //                port = int.Parse(str.Split('=')[1]);
        //                break;
        //            }
        //        }
        //    }
        //    return port;
        //}
        static ushort MediaInit(string configPath)
        {
            MediaServer.Config config = new MediaServer.Config();
            config.ini_is_path = 1;
            config.ini = configPath;
            config.log_file_path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
            MediaServer.mk_env_init(ref config);
            ushort ret;
            ret = MediaServer.mk_http_server_start(mediaServerConfig.mk_http_port, 0);
            ret = MediaServer.mk_rtsp_server_start(mediaServerConfig.mk_rtsp_port, 0);
            ret = MediaServer.mk_rtmp_server_start(mediaServerConfig.mk_rtmp_port, 0);
            return ret;
        }
        static bool InitRedis()
        {
            try
            {
                string redisconf = $"{redisAddress}:{redisPort},password=123456,DefaultDatabase=0";
                RedisHelper.SetCon(redisconf);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("InitRedis:" + ex.StackTrace);
                return false;
            }
        }
        /// <summary>
        /// �߳�һֱ����key�Ĺ���ʱ�䣬���keyδ���£����ڣ���������sms������
        /// </summary>
        static void KeepLive()
        {
            while (true)
            {
                try
                {
                    DateTime dtNow = DateTime.Now;
                    DateTime dtExpire = dtNow.AddSeconds(mediaKeepLiveSecond);
                    TimeSpan ts = dtExpire - dtNow;
                    if (!RedisHelper.GetDatabase().KeyExists(smsMsg))
                    {
                        RedisHelper.GetDatabase().StringSet(smsMsg, httpServerPort.ToString());

                    }
                    bool ret = RedisHelper.GetDatabase().KeyExpire(smsMsg, ts);
                    if (!ret)
                    {
                        Console.WriteLine("sms_" + mediaServerConfig.Ipaddress + ":����ʧ��");
                    }
                    //Console.WriteLine("sms_" + mediaServerConfig.Ipaddress + ":����");
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("redis:�����쳣");
                    //Console.WriteLine(ex.Message);
                    //Console.WriteLine(ex.StackTrace);
                }
            }
        }

        static void AddStreamProxyTest()
        {
            try
            {
                mediaServerConfig = new MediaServerConfig();
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
                ushort ret = MediaInit(configPath);
                Console.WriteLine("��ʼ�����");
                RegisterEvent();
                Console.WriteLine("�¼�ע�����");
                int i = 0;
                foreach (var item in System.Configuration.ConfigurationManager.AppSettings.AllKeys)
                {
                    if (item.Contains("rtspurl"))
                    {
                        
                        i++;
                        string stream = "live" + i.ToString();
                        string url = System.Configuration.ConfigurationManager.AppSettings[item];
                        Console.WriteLine("��ʼ������"+url);
                        IntPtr playProxy = MediaServer.mk_proxy_player_create("__defaultVhost__", "fh", stream, 1, 0);
                        //rtsp://admin:123456a%3F@10.128.23.51:554/h264/ch33/main/av_stream
                        MediaServer.mk_proxy_player_play(playProxy, url);
                        Task.Run(() => media_source_no_reader_check());
                    }
                } 

                
                //IntPtr playProxy = MediaServer.mk_proxy_player_create("__defaultVhost__", "fh", "live1", 1, 0);
                ////rtsp://admin:123456a%3F@10.128.23.51:554/h264/ch33/main/av_stream
                //MediaServer.mk_proxy_player_play(playProxy, "rtsp://admin:123456a?@10.128.23.51:554/h264/ch33/main/av_stream");
                //IntPtr playProxy1 = MediaServer.mk_proxy_player_create("__defaultVhost__", "fh", "live2", 1, 0);
                //MediaServer.mk_proxy_player_play(playProxy1, "rtsp://admin:admin123@10.128.23.67:554/h264/ch33/main/av_stream");
                Console.Read();
                MediaServer.mk_stop_all_server();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                Console.Read();
            }
        }

        static CHCNetSDK.REALDATACALLBACK RealData = null;
        static IntPtr ctx;
        static void SDKTest()
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");
            MediaServer.Config config = new MediaServer.Config();
            config.ini_is_path = 1;
            config.ini = configPath;
            MediaServer.mk_env_init(ref config);
            uint ret;
            ret = MediaServer.mk_http_server_start(8500, 0);
            ret = MediaServer.mk_rtsp_server_start(554, 0);
            ret = MediaServer.mk_rtmp_server_start(1935, 0);


            ctx = MediaServer.mk_media_create("__defaultVhost__", "fh", "sdklive", 0, 1, 1, 1, 0);
            MediaServer.mk_media_init_video(ctx, 0, 1280, 720, 25);
            MediaServer.mk_media_init_complete(ctx);


            bool m_bInitSDK = CHCNetSDK.NET_DVR_Init();
            CHCNetSDK.NET_DVR_SetLogToFile(0, "", true);
            CHCNetSDK.NET_DVR_USER_LOGIN_INFO struLogInfo = new CHCNetSDK.NET_DVR_USER_LOGIN_INFO();
            //�豸IP��ַ��������
            byte[] byIP = System.Text.Encoding.Default.GetBytes("10.128.24.57");
            struLogInfo.sDeviceAddress = new byte[129];
            byIP.CopyTo(struLogInfo.sDeviceAddress, 0);

            //�豸�û���
            byte[] byUserName = System.Text.Encoding.Default.GetBytes("admin");
            struLogInfo.sUserName = new byte[64];
            byUserName.CopyTo(struLogInfo.sUserName, 0);

            //�豸����
            byte[] byPassword = System.Text.Encoding.Default.GetBytes("123456a?");
            struLogInfo.sPassword = new byte[64];
            byPassword.CopyTo(struLogInfo.sPassword, 0);

            struLogInfo.wPort = 8000;//�豸����˿ں�
            CHCNetSDK.NET_DVR_DEVICEINFO_V40 DeviceInfo = new CHCNetSDK.NET_DVR_DEVICEINFO_V40();
            int m_lUserID = CHCNetSDK.NET_DVR_Login_V40(ref struLogInfo, ref DeviceInfo);



            if (RealData == null)
            {
                RealData = new CHCNetSDK.REALDATACALLBACK(RealDataCallBack);//Ԥ��ʵʱ���ص�����

            }

            CHCNetSDK.NET_DVR_PREVIEWINFO lpPreviewInfo = new CHCNetSDK.NET_DVR_PREVIEWINFO();
            //lpPreviewInfo.hPlayWnd = RealPlayWnd.Handle;//Ԥ������
            lpPreviewInfo.lChannel = 1;//Ԥte�����豸ͨ��
            lpPreviewInfo.dwStreamType = 0;//�������ͣ�0-��������1-��������2-����3��3-����4���Դ�����
            lpPreviewInfo.dwLinkMode = 0;//���ӷ�ʽ��0- TCP��ʽ��1- UDP��ʽ��2- �ಥ��ʽ��3- RTP��ʽ��4-RTP/RTSP��5-RSTP/HTTP 
            lpPreviewInfo.bBlocked = true; //0- ������ȡ����1- ����ȡ��
            lpPreviewInfo.dwDisplayBufNum = 1; //���ſⲥ�Ż�������󻺳�֡��
            lpPreviewInfo.byProtoType = 0;
            lpPreviewInfo.byPreviewMode = 0;
            IntPtr pUser = new IntPtr();//�û�����
            int m_lRealHandle = CHCNetSDK.NET_DVR_RealPlay_V40(m_lUserID, ref lpPreviewInfo, /*null*/ RealData, pUser);
            if (m_lRealHandle >= 0)//���ųɹ�
            {
                //eSCallBack = new CHCNetSDK.PlayESCallBack(ESCallBack);
                //bool ret = CHCNetSDK.NET_DVR_SetESRealPlayCallBack(m_lRealHandle, eSCallBack, IntPtr.Zero);
                Task task = Task.Factory.StartNew(HIKDataInput);
            }
            Console.Read();
        }
        static List<byte> cache = new List<byte>();
        static void HIKDataInput()
        {
            while (true)
            {
                try
                {
                    if (queue.Count > 0)
                    {
                        byte[] psData = queue.Dequeue();
                        if (psData[0] == 0 && psData[1] == 0 && psData[2] == 1 && psData[3] == 186)
                        {
                            if (cache.Count != 0)
                            {
                                MediaServer.mk_media_input_ps(ctx, cache.ToArray(), cache.Count);
                                cache.Clear();
                            }
                            cache.AddRange(psData);
                        }
                        else if (psData[0] == 0 && psData[1] == 0 && psData[2] == 1 && psData[3] == 223)
                        {
                            if (cache.Count != 0)
                                cache.AddRange(psData);
                        }
                        else if (psData[0] == 0 && psData[1] == 0 && psData[2] == 1 && psData[3] == 224)
                        {
                            if (cache.Count != 0)
                                cache.AddRange(psData);
                        }

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
        static Queue<byte[]> queue = new Queue<byte[]>();
        public static void RealDataCallBack(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser)
        {
            if (dwBufSize > 0 && dwDataType != 1)
            {

                byte[] sData = new byte[dwBufSize];
                Marshal.Copy(pBuffer, sData, 0, (Int32)dwBufSize);
                queue.Enqueue(sData);
                string str = "ʵʱ������.ps";
                FileStream fs = new FileStream(str, FileMode.Append);
                int iLen = (int)dwBufSize;
                fs.Write(sData, 0, iLen);
                fs.Close();
            }
        }

    }
}
