using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RealSenseSample.Models
{
    /// <summary>
    /// RealSenseセンサーモデル
    /// </summary>
    public class RealSenseSensorModel : INotifyPropertyChanged
    {
        /// <summary>
        /// カラーストリーム固定設定値
        /// </summary>
        public const int COLOR_WIDTH = 640;
        public const int COLOR_HEIGHT = 480;
        public const int COLOR_FPS = 30;

        //認識人数
        public const int DETECTION_MAXFACES = 1;
        public const int LANDMARK_MAXFACES = 1;
        
        /// <summary>
        /// 排他ロック用オブジェクト
        /// </summary>
        private static object lockObject = new object();

        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        private static RealSenseSensorModel instance = null;
        public static RealSenseSensorModel Instance
        {
            get
            {
                lock (lockObject)
                {
                    if (instance == null)
                    {
                        instance = new RealSenseSensorModel();
                    }
                }
                return instance;
            }
        }

        /// <summary>
        /// センスマネージャ
        /// </summary>
        private PXCMSenseManager senseManager;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        private RealSenseSensorModel()
        {
            this.Initialize();
        }

        /// <summary>
        /// デストラクタ
        /// </summary>
        ~RealSenseSensorModel()
        {
            this.Uninitialize();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        private void Initialize()
        {
            // SenseManagerを生成する
            this.senseManager = PXCMSenseManager.CreateInstance();
            if (this.senseManager == null)
            {
                throw new Exception("SenseManagerの生成に失敗しました");
            }

            // カラーストリームを有効にする
            var status = this.senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, COLOR_WIDTH, COLOR_HEIGHT, COLOR_FPS);
            if (status < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                throw new Exception("カラーストリームの有効化に失敗しました");
            }

            // 顔認識系初期化
            this.InitializeFace();

            // パイプラインを初期化する
            status = this.senseManager.Init();
            if (status < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                throw new Exception("パイプラインの初期化に失敗しました");
            }

            // デバイス設定
            PXCMCapture.Device device = this.senseManager.QueryCaptureManager().QueryDevice();
            if (device == null)
            {
                throw new Exception("deviceの取得に失敗しました");
            }
            // ミラー表示にする
            device.SetMirrorMode(PXCMCapture.Device.MirrorMode.MIRROR_MODE_HORIZONTAL);

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        /// <summary>
        /// 顔認識系初期化
        /// </summary>
        private void InitializeFace()
        {
            //// 実装してください ////
        }

        /// <summary>
        /// 破棄処理
        /// </summary>
        private void Uninitialize()
        {
            if (this.senseManager != null)
            {
                this.senseManager.Dispose();
                this.senseManager = null;
            }
        }

        /// <summary>
        /// WPF描画更新時イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            this.UpdateFrame();
        }

        /// <summary>
        /// フレーム更新
        /// </summary>
        private void UpdateFrame()
        {
            // フレームを取得する
            var status = this.senseManager.AcquireFrame(false);
            if (status < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                return;
            }

            try
            {
                // フレームデータを取得する
                PXCMCapture.Sample sample = this.senseManager.QuerySample();
                this.UpdateColorImage(sample);

                //顔のデータを更新する
                this.UpdateFaceFrame(sample);
            }
            finally
            {
                this.senseManager.ReleaseFrame();
            }
        }

        /// <summary>
        /// カラーイメージを更新する
        /// </summary>
        /// <param name="sample">フレームデータ</param>
        private void UpdateColorImage(PXCMCapture.Sample sample)
        {
            PXCMImage colorFrame = sample.color;
            // データを取得する
            PXCMImage.ImageData data;
            pxcmStatus status = colorFrame.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24, out data);
            if (status < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                return;
            }

            try
            {
                // Bitmapに変換する
                var buffer = data.ToByteArray(0, COLOR_WIDTH * COLOR_HEIGHT * 3);
                this.ColorImageSource = BitmapSource.Create(COLOR_WIDTH, COLOR_HEIGHT, 96, 96, PixelFormats.Bgr24, null, buffer, COLOR_WIDTH * 3);
            }
            finally
            {
                colorFrame.ReleaseAccess(data);
            }
        }

        /// <summary>
        /// Faceデータを更新する
        /// </summary>
        /// <param name="sample">フレームデータ</param>
        private void UpdateFaceFrame(PXCMCapture.Sample sample)
        {
            //// 実装してください ////
        }

        /// <summary>
        /// カラーイメージソース
        /// </summary>
        private BitmapSource colorImageSource = null;
        public BitmapSource ColorImageSource
        {
            get
            {
                return this.colorImageSource;
            }
            set
            {
                this.colorImageSource = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Face情報
        /// </summary>
        private PXCMFaceData faceData = null;
        public PXCMFaceData FaceData
        {
            get
            {
                return this.faceData;
            }
            set
            {
                this.faceData = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// プロパティ変更時イベント
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
