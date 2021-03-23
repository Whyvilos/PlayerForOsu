using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace WpfTutorialSamples.Audio_and_Video
{
    public partial class AudioVideoPlayerCompleteSample : Window
    {
        private bool mediaPlayerIsPlaying = false;
        private bool userIsDraggingSlider = false;
        private MediaPlayer mePlayer;
        public string PathOsu = "";   //путь к осу 
        private Random rnd; //рандомайзер
        private int randomSong;  //рандомная бит-мапа
        private NorthOBD.ReaderOSU.ReaderOsuDB OsuDB;  //хранит osu!.db
        private NorthOBD.ReaderCollection.ReaderCollectionDB CollectionDB; // хранит collection.db

        public AudioVideoPlayerCompleteSample()
        {

            InitializeComponent();
            //Создаем рандомайзер и медиопеременную
            rnd = new Random();
            mePlayer = new MediaPlayer();
            //Настройка звука
            pbVolume.Value = 0.5;
            mePlayer.Volume = 0.5;


            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        //Далее функции плеера
        private void timer_Tick(object sender, EventArgs e)
        {
            if ((mePlayer.Source != null) && (mePlayer.NaturalDuration.HasTimeSpan) && (!userIsDraggingSlider))
            {
                sliProgress.Minimum = 0;
                sliProgress.Maximum = mePlayer.NaturalDuration.TimeSpan.TotalSeconds;
                sliProgress.Value = mePlayer.Position.TotalSeconds;
            }
        }

        private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }


        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            DialogResult result = folderBrowser.ShowDialog();
            if (!string.IsNullOrWhiteSpace(folderBrowser.SelectedPath))
            {
                //получаем путь осу
                PathOsu = folderBrowser.SelectedPath;
                //загружаем информацию о бит-мапах
                if (RenderBM())
                    if (RenderCollection())
                    { randomSong = rnd.Next(0, (int)OsuDB.NumberOfBM); rndButton.IsEnabled = true; PlaySet(); }

            }
        }

        private void Play_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = (mePlayer != null) && (mePlayer.Source != null);
        }

        private void Play_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mePlayer.Play();
            mediaPlayerIsPlaying = true;
        }

        private void Pause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = mediaPlayerIsPlaying;
        }

        private void Pause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mePlayer.Pause();
        }

        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = mediaPlayerIsPlaying;
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mePlayer.Stop();
            mediaPlayerIsPlaying = false;
        }

        private void sliProgress_DragStarted(object sender, DragStartedEventArgs e)
        {
            userIsDraggingSlider = true;
        }

        private void sliProgress_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            userIsDraggingSlider = false;
            mePlayer.Position = TimeSpan.FromSeconds(sliProgress.Value);
        }

        private void sliProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            lblProgressStatus.Text = TimeSpan.FromSeconds(sliProgress.Value).ToString(@"hh\:mm\:ss");
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            mePlayer.Volume += (e.Delta > 0) ? 0.005 : -0.005;
            pbVolume.Value += (e.Delta > 0) ? 0.005 : -0.005;
        }



        //выбираем рандомную бит-мапу
        private void RandomSong(object sender, RoutedEventArgs e)
        {
            randomSong = rnd.Next(0, (int)OsuDB.NumberOfBM);
            mePlayer.Stop();
            PlaySet();

        }

        //читаем osu!.db
        public bool RenderBM()
        {

            try
            {
                BinaryReader readerDb = new BinaryReader(File.Open(PathOsu + "\\osu!.db", FileMode.Open));
                OsuDB = new NorthOBD.ReaderOSU.ReaderOsuDB(ref readerDb);

                readerDb.Close();
                return true;
            }
            catch
            {
                {
                    System.Windows.MessageBox.Show("Возможно это не корневая папка osu");
                }
                return false;
            }

        }

        //читаем collection.db
        public bool RenderCollection()
        {


            try
            {
                BinaryReader readerDb = new BinaryReader(File.Open(PathOsu + "\\collection.db", FileMode.Open));
                CollectionDB = new NorthOBD.ReaderCollection.ReaderCollectionDB(ref readerDb);
                return true;
            }
            catch
            {
                {
                    System.Windows.MessageBox.Show("Возможно это не корневая папка osu");
                }
                return false;
            }
        }

        //ищем и запускаем mp3
        public void PlaySet()
        {
            try
            {

                //получаем название .osu файла
                string nameOsuFile = OsuDB.Beatmaps[randomSong].ArtistName + " - " + OsuDB.Beatmaps[randomSong].SongName + " (" + OsuDB.Beatmaps[randomSong].CreaterName + ") [" + OsuDB.Beatmaps[randomSong].Difficulty + "].osu";
                //читаем .osu файла
                string allFile = System.IO.File.ReadAllText(PathOsu + "\\Songs\\" + OsuDB.Beatmaps[randomSong].SongFolder + "\\" + nameOsuFile, Encoding.Default).Replace("\n", " ");
                //в файле .osu ищем название mp3
                string nameJpg = ((Regex.Match(allFile, @"(?i)[\w\s\(\)\!\.\[\]\'\^\-\~\+\\\/\&]+\.mp3(?i)").Value));
                //загружаем mp3
                mePlayer.Open(new Uri(PathOsu + "\\Songs\\" + OsuDB.Beatmaps[randomSong].SongFolder + "\\" + nameJpg.Remove(0, 1), UriKind.Absolute));
            }

            catch
            {
                {
                    System.Windows.MessageBox.Show("Ошибка чтения");
                }

            }


        }

    }
}


