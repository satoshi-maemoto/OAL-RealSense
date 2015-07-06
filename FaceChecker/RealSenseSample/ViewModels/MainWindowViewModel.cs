using RealSenseSample.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RealSenseSample.ViewModels
{
    /// <summary>
    /// メインウィンドウ　ViewModel
    /// </summary>
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// 顔を囲う矩形
        /// </summary>
        private Rectangle[] faceRects;

        /// <summary>
        /// 顔のランドマークポイント
        /// </summary>
        private Ellipse[,] facePoints;

        /// <summary>
        /// ガイドラインタイプ
        /// </summary>
        private enum GuidelineType
        {
            ForeheadTop,
            Eyeblow,
            NoseBottom,
            ChinBottom,
        }

        /// <summary>
        /// ガイドライン
        /// </summary>
        private IDictionary<GuidelineType, Line>[] guidelines;

        /// <summary>
        /// 判定結果表示テキスト
        /// </summary>
        private TextBlock[] resultText;

        /// <summary>
        /// センサーモデル
        /// </summary>
        protected RealSenseSensorModel SensorModel { get; private set; }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindowViewModel()
        {
            this.Initialize();
        }

        /// <summary>
        /// デストラクタ
        /// </summary>
        ~MainWindowViewModel()
        {
            this.Uninitialize();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        private void Initialize()
        {
            this.SensorModel = RealSenseSensorModel.Instance;
            this.SensorModel.PropertyChanged += this.Model_PropertyChanged;

            this.Canvas = new Canvas();

            //顔を囲う矩形の初期化
            this.faceRects = new Rectangle[RealSenseSensorModel.DETECTION_MAXFACES];
            for (int i = 0; i < RealSenseSensorModel.DETECTION_MAXFACES; i++)
            {
                this.faceRects[i] = new Rectangle();
                TranslateTransform transform = new TranslateTransform(RealSenseSensorModel.COLOR_WIDTH, RealSenseSensorModel.COLOR_HEIGHT);
                this.faceRects[i].Width = 0;
                this.faceRects[i].Height = 0;
                this.faceRects[i].Stroke = Brushes.Blue;
                this.faceRects[i].StrokeThickness = 3;
                this.faceRects[i].RenderTransform = transform;
                this.Canvas.Children.Add(this.faceRects[i]);
            }
            //顔のランドマークポイントの初期化
            this.facePoints = new Ellipse[RealSenseSensorModel.LANDMARK_MAXFACES, 78];
            for (int i = 0; i < RealSenseSensorModel.LANDMARK_MAXFACES; i++)
            {
                for (int j = 0; j < 78; j++)
                {
                    this.facePoints[i, j] = new Ellipse();
                    this.facePoints[i, j].Width = 1;
                    this.facePoints[i, j].Height = 1;
                    this.facePoints[i, j].Fill = new SolidColorBrush(Colors.Red);
                    this.Canvas.Children.Add(this.facePoints[i, j]);
                }
            }
            //ガイドラインの初期化
            this.guidelines = new Dictionary<GuidelineType, Line>[RealSenseSensorModel.LANDMARK_MAXFACES];
            for (int i = 0; i < RealSenseSensorModel.LANDMARK_MAXFACES; i++)
            {
                this.guidelines[i] = new Dictionary<GuidelineType, Line>();
                foreach (GuidelineType guideline in Enum.GetValues(typeof(GuidelineType)))
                {
                    var line = new Line();
                    line.Stroke = Brushes.White;
                    line.X1 = 0;
                    line.Y1 = 0;
                    line.X2 = 0;
                    line.Y2 = 0;
                    this.guidelines[i].Add(guideline, line);
                    this.Canvas.Children.Add(line);
                }
            }
            //判定結果表示テキストの初期化
            this.resultText = new TextBlock[RealSenseSensorModel.LANDMARK_MAXFACES];
            for (int i = 0; i < RealSenseSensorModel.LANDMARK_MAXFACES; i++)
            {
                this.resultText[i] = new TextBlock();
                this.resultText[i].Width = 200;
                this.resultText[i].Height = 32;
                this.resultText[i].Foreground = Brushes.Magenta;
                this.resultText[i].FontSize = 20;
                this.Canvas.Children.Add(this.resultText[i]);
            }
        }

        /// <summary>
        /// 破棄処理
        /// </summary>
        private void Uninitialize()
        {
            if (this.SensorModel != null)
            {
                this.SensorModel.PropertyChanged -= this.Model_PropertyChanged;
                this.SensorModel = null;
            }
        }

        /// <summary>
        /// カラーイメージソース
        /// </summary>
        public BitmapSource ColorImageSource
        {
            get
            {
                return this.SensorModel.ColorImageSource;
            }
        }

        /// <summary>
        /// Faceデータの描画
        /// </summary>
        private void DrawFaceDataBy2D()
        {
            //検出した顔の数を取得する
            int numberOfFaces = this.SensorModel.FaceData.QueryNumberOfDetectedFaces();

            //顔のランドマーク（特徴点）のデータの入れ物を用意
            PXCMFaceData.LandmarksData[] landmarkData = new PXCMFaceData.LandmarksData[RealSenseSensorModel.LANDMARK_MAXFACES];

            //それぞれの顔ごとに情報取得および描画処理を行う
            for (int i = 0; i < numberOfFaces; i++)
            {
                //顔の情報を取得する
                PXCMFaceData.Face face = this.SensorModel.FaceData.QueryFaceByIndex(i);
                var detection = face.QueryDetection();
                if (detection != null)
                {
                    PXCMRectI32 faceRect;
                    detection.QueryBoundingRect(out faceRect);

                    //顔の位置に合わせて長方形を変更
                    var transform = new TranslateTransform(faceRect.x, faceRect.y);
                    this.faceRects[i].Width = faceRect.w;
                    this.faceRects[i].Height = faceRect.h;
                    this.faceRects[i].RenderTransform = transform;

                    //フェイスデータからランドマーク（特徴点群）についての情報を得る
                    landmarkData[i] = face.QueryLandmarks();

                    if (landmarkData[i] != null)
                    {
                        //何個の特徴点が認識できたかを調べる
                        var numPoints = landmarkData[i].QueryNumPoints();
                        //認識できた特徴点の数だけ、特徴点を格納するインスタンスを生成する
                        var landmarkPoints = new PXCMFaceData.LandmarkPoint[numPoints];
                        //ランドマークデータから、特徴点の位置を取得、表示
                        if (landmarkData[i].QueryPoints(out landmarkPoints))
                        {
                            for (int j = 0; j < numPoints; j++)
                            {
                                this.facePoints[i, j].RenderTransform = new TranslateTransform(landmarkPoints[j].image.x, landmarkPoints[j].image.y);
                            }

                            //ガイドラインの描画
                            var eyeblowY =
                                (
                                landmarkPoints[landmarkData[i].QueryPointIndex(PXCMFaceData.LandmarkType.LANDMARK_EYEBROW_LEFT_CENTER)].image.y +
                                landmarkPoints[landmarkData[i].QueryPointIndex(PXCMFaceData.LandmarkType.LANDMARK_EYEBROW_RIGHT_CENTER)].image.y
                                ) / 2;
                            var foreheadTopY = eyeblowY - ((eyeblowY - faceRect.y) * 2);   //額と眉の距離は顔矩形の上端と眉のY位置の距離の2倍と仮定（雰囲気）
                            var noseBottom = landmarkPoints[landmarkData[i].QueryPointIndex(PXCMFaceData.LandmarkType.LANDMARK_NOSE_BOTTOM)];
                            var chin = landmarkPoints[landmarkData[i].QueryPointIndex(PXCMFaceData.LandmarkType.LANDMARK_CHIN)];
                            foreach (GuidelineType guideline in Enum.GetValues(typeof(GuidelineType)))
                            {
                                switch (guideline)
                                {
                                    case GuidelineType.ForeheadTop:
                                        this.guidelines[i][guideline].RenderTransform = new TranslateTransform(faceRect.x, foreheadTopY);
                                        break;
                                    case GuidelineType.Eyeblow:
                                        this.guidelines[i][guideline].RenderTransform = new TranslateTransform(faceRect.x, eyeblowY);
                                        break;
                                    case GuidelineType.NoseBottom:
                                        this.guidelines[i][guideline].RenderTransform = new TranslateTransform(faceRect.x, noseBottom.image.y);
                                        break;
                                    case GuidelineType.ChinBottom:
                                        this.guidelines[i][guideline].RenderTransform = new TranslateTransform(faceRect.x, chin.image.y);
                                        break;
                                }
                                this.guidelines[i][guideline].X2 = faceRect.w;
                            }

                            //判定結果の描画
                            this.resultText[i].RenderTransform = new TranslateTransform(faceRect.x, faceRect.y + faceRect.h + 16);
                            float a = eyeblowY - foreheadTopY;
                            float b = noseBottom.image.y - eyeblowY;
                            float c = chin.image.y - noseBottom.image.y;
                            this.resultText[i].Text = string.Format("{0:#.0} : {1:#.0} : {2:#.0}", 1, b / a, c / a);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// キャンバス
        /// </summary>
        public Canvas Canvas { get; private set; }

        /// <summary>
        /// Modelプロパティ変更時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "FaceData":
                    this.DrawFaceDataBy2D();
                    break;
                default:
                    //Modelの変更通知を中継してViewへ送信する
                    this.OnPropertyChanged(e.PropertyName);
                    break;
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
