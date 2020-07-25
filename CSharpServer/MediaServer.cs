using System;using System.Collections.Generic;using System.Linq;using System.Text;using System.Threading.Tasks;using System.Reflection;using System.Runtime.InteropServices;namespace CSharpServer{
    [StructLayout(LayoutKind.Sequential)]
    public struct Events
    {
        public on_mk_media_changed IntPtrOn_mk_media_changed;
        public IntPtr IntPtr2;
        public IntPtr IntPtr3;
        public IntPtr IntPtr4;
        public IntPtr IntPtr5;
        public IntPtr IntPtr6;
        public IntPtr IntPtr7;
        public IntPtr IntPtr8;
        public IntPtr IntPtr9;
        public IntPtr IntPtr10;
        public IntPtr IntPtr11;
        public IntPtr IntPtr12;
        public on_mk_flow_report IntPtrOn_mk_flow_report;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct TrackInfos
    {
        public CodecId track_type;
        public TrackType code_id;
    }
    public enum CodecId
    {
        CodecInvalid = -1,
        CodecH264 = 0,
        CodecH265,
        CodecAAC,
        CodecG711A,
        CodecG711U,
        CodecOpus,
        CodecMax = 0x7FFF
    }
    public enum TrackType
    {
        TrackInvalid = -1,
        TrackVideo = 0,
        TrackAudio,
        TrackTitle,
        TrackMax = 3
    }
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void on_mk_media_changed(int regist, ref IntPtr ctx);

    /**
    * 停止rtsp/rtmp/http-flv会话后流量汇报事件广播
    * @param url_info 播放url相关信息
    * @param total_bytes 耗费上下行总流量，单位字节数
    * @param total_seconds 本次tcp会话时长，单位秒
    * @param is_player 客户端是否为播放器
    */
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void on_mk_flow_report(ref IntPtr mk_media_info, UInt64 total_bytes, UInt64 total_seconds,int is_player,ref IntPtr mk_sock_info);

    //查找MediaSource的回调函数
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void on_mk_media_source_find_cb(IntPtr user_data, ref IntPtr mk_media_source);

    
    public class MediaServer    {        [StructLayout(LayoutKind.Sequential)]        public struct Config        {            /// int            public int thread_num;            /// int            public int log_level;            //文件日志保存路径,路径可以不存在(内部可以创建文件夹)，设置为NULL关闭日志输出至文件
            public string log_file_path;
            //文件日志保存天数,设置为0关闭日志文件
            public int log_file_days;


            /// int            public int ini_is_path;            /// char*            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPStr)]            public string ini;            /// int            public int ssl_is_path;            /// char*            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPStr)]            public string ssl;            /// char*            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPStr)]            public string ssl_pwd;        }        //public const string dllPath = @"..\..\..\release\windows\Debug\Debug\api.dll";        public const string dllPath = "mk_api.dll";        [DllImport(dllPath, EntryPoint = "mk_events_listen", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]        public static extern void mk_events_listen(ref Events events);        /// Return Type: void        ///cfg: config*        [DllImport(dllPath, EntryPoint = "mk_env_init", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]        public static extern void mk_env_init(ref Config cfg);        /// Return Type: uint16_t->unsigned short        ///port: uint16_t->unsigned short        ///ssl: int        [DllImport(dllPath, EntryPoint = "mk_http_server_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]        public static extern ushort mk_http_server_start(ushort port, int ssl);        [DllImport(dllPath, EntryPoint = "mk_rtsp_server_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]        public static extern ushort mk_rtsp_server_start(ushort port, int ssl);        [DllImport(dllPath, EntryPoint = "mk_rtmp_server_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]        public static extern ushort mk_rtmp_server_start(ushort port, int ssl);        [DllImport(dllPath, EntryPoint = "mk_rtp_server_start", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]        public static extern ushort mk_rtp_server_start(ushort port,string src);        [DllImport(dllPath, EntryPoint = "mk_rtp_server_stop", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]        public static extern bool mk_rtp_server_stop(string src);        [DllImport(dllPath, EntryPoint = "mk_stop_all_server", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]        public static extern void mk_stop_all_server();        [DllImport(dllPath, EntryPoint = "mk_media_source_get_schema", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]        public static extern IntPtr mk_media_source_get_schema(ref IntPtr PtrMediaSource);
        //MediaSource::getVhost()
        [DllImport(dllPath, EntryPoint = "mk_media_source_get_vhost", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mk_media_source_get_vhost(ref IntPtr PtrMediaSource);
        //MediaSource::getApp()
        [DllImport(dllPath, EntryPoint = "mk_media_source_get_app", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mk_media_source_get_app(ref IntPtr PtrMediaSource);
        //MediaSource::getId()
        [DllImport(dllPath, EntryPoint = "mk_media_source_get_stream", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mk_media_source_get_stream(ref IntPtr PtrMediaSource);
        //MediaSource::totalReaderCount()
        [DllImport(dllPath, EntryPoint = "mk_media_source_get_total_reader_count", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int mk_media_source_get_total_reader_count(ref IntPtr PtrMediaSource);



        [DllImport(dllPath, EntryPoint = "mk_media_info_get_schema", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]        public static extern IntPtr mk_media_info_get_schema(ref IntPtr mk_media_info);
        //MediaSource::getVhost()
        [DllImport(dllPath, EntryPoint = "mk_media_info_get_vhost", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mk_media_info_get_vhost(ref IntPtr mk_media_info);
        //MediaSource::getApp()
        [DllImport(dllPath, EntryPoint = "mk_media_info_get_app", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mk_media_info_get_app(ref IntPtr mk_media_info);
        //MediaSource::getId()
        [DllImport(dllPath, EntryPoint = "mk_media_info_get_stream", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mk_media_info_get_stream(ref IntPtr mk_media_info);

        [DllImport(dllPath, EntryPoint = "mk_media_source_get_tracks", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int mk_media_source_get_tracks(ref IntPtr mk_media_info,IntPtr trackinfos);


        //SockInfo::get_peer_ip()
        [DllImport(dllPath, EntryPoint = "mk_sock_info_peer_ip", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mk_sock_info_peer_ip(ref IntPtr mk_sock_info,ref StringBuilder buf);
        //SockInfo::get_local_ip()
        [DllImport(dllPath, EntryPoint = "mk_sock_info_peer_ip", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mk_sock_info_local_ip(ref IntPtr mk_sock_info, StringBuilder buf);
        //SockInfo::get_peer_port()
        [DllImport(dllPath, EntryPoint = "mk_sock_info_peer_ip", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int mk_sock_info_peer_port(ref IntPtr mk_sock_info);
        //SockInfo::get_local_port()
        [DllImport(dllPath, EntryPoint = "mk_sock_info_peer_ip", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern int mk_sock_info_local_port(ref IntPtr mk_sock_info);

        /**
        * 创建一个代理播放器
        * @param vhost 虚拟主机名，一般为__defaultVhost__
        * @param app 应用名
        * @param stream 流名
        * @param rtp_type rtsp播放方式:RTP_TCP = 0, RTP_UDP = 1, RTP_MULTICAST = 2
        * @param hls_enabled 是否生成hls
        * @param mp4_enabled 是否生成mp4
        * @return 对象指针
        */
        [DllImport(dllPath, EntryPoint = "mk_proxy_player_create", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mk_proxy_player_create(string vhost, string app, string stream, int hls_enabled, int mp4_enabled);

        /**
         * 销毁代理播放器
         * @param ctx 对象指针
         */
        [DllImport(dllPath, EntryPoint = "mk_proxy_player_release", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void mk_proxy_player_release(IntPtr mk_proxy_player);

        /**
         * 设置代理播放器配置选项
         * @param ctx 代理播放器指针
         * @param key 配置项键,支持 net_adapter/rtp_type/rtsp_user/rtsp_pwd/protocol_timeout_ms/media_timeout_ms/beat_interval_ms/max_analysis_ms
         * @param val 配置项值,如果是整形，需要转换成统一转换成string
         */
        [DllImport(dllPath, EntryPoint = "mk_proxy_player_set_option", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void mk_proxy_player_set_option(IntPtr mk_proxy_player, string key, string val);

        /**
         * 开始播放
         * @param ctx 对象指针
         * @param url 播放url,支持rtsp/rtmp
         */
        [DllImport(dllPath, EntryPoint = "mk_proxy_player_play", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void mk_proxy_player_play(IntPtr mk_proxy_player, string url);


        /**
        * 创建一个媒体源
        * @param vhost 虚拟主机名，一般为__defaultVhost__
        * @param app 应用名，推荐为live
        * @param stream 流id，例如camera
        * @param duration 时长(单位秒)，直播则为0
        * @param hls_enabled 是否生成hls
        * @param mp4_enabled 是否生成mp4
        * @return 对象指针
        */
        [DllImport(dllPath, EntryPoint = "mk_media_create", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr mk_media_create(string vhost, string app, string stream, float duration,
                                             int rtsp_enabled, int rtmp_enabled, int hls_enabled, int mp4_enabled);

        /**
        * 销毁媒体源
        * @param ctx 对象指针
        */
        [DllImport(dllPath, EntryPoint = "mk_media_release", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void mk_media_release(IntPtr ctx);

        /**
        * 添加h264视频轨道
        * @param ctx 对象指针
        * @param width 视频宽度
        * @param height 视频高度
        * @param fps 视频fps
        */
        [DllImport(dllPath, EntryPoint = "mk_media_init_h264", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void mk_media_init_h264(IntPtr ctx, int width, int height, int fps);
        /**
        * 初始化h264/h265/aac完毕后调用此函数，
        * 在单track(只有音频或视频)时，因为ZLMediaKit不知道后续是否还要添加track，所以会多等待3秒钟
        * 如果产生的流是单Track类型，请调用此函数以便加快流生成速度，当然不调用该函数，影响也不大(会多等待3秒)
        * @param ctx 对象指针
        */
        [DllImport(dllPath, EntryPoint = "mk_media_init_complete", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void mk_media_init_complete(IntPtr ctx);
        /**
        * 添加视频轨道
        * @param ctx 对象指针
        * @param track_id  0:CodecH264/1:CodecH265
        * @param width 视频宽度
        * @param height 视频高度
        * @param fps 视频fps
        */
        [DllImport(dllPath, EntryPoint = "mk_media_init_video", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void mk_media_init_video(IntPtr ctx, int track_id, int width, int height, int fps);
        /**
        * 输入单帧H264视频，帧起始字节00 00 01,00 00 00 01均可
        * @param ctx 对象指针
        * @param data 单帧H264数据
        * @param len 单帧H264数据字节数
        * @param dts 解码时间戳，单位毫秒
        * @param pts 播放时间戳，单位毫秒
        */
        [DllImport(dllPath, EntryPoint = "mk_media_input_h264", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void mk_media_input_h264(IntPtr ctx, IntPtr data, int len, int dts, int pts);
        [DllImport(dllPath, EntryPoint = "mk_media_input_ps", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void mk_media_input_ps(IntPtr ctx, byte[] data, int len);



        //MediaSource::find()
        [DllImport(dllPath, EntryPoint = "mk_media_source_find", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void mk_media_source_find(string schema,
                                                  string vhost,
                                                  string app,
                                                  string stream,
                                                  IntPtr user_data,
                                                  on_mk_media_source_find_cb cb);

        [DllImport(dllPath, EntryPoint = "mk_media_source_for_each", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        public static extern void mk_media_source_for_each(IntPtr user_data, on_mk_media_source_find_cb cb);
    }}