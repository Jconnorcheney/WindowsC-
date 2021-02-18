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
using System.Windows.Navigation;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using Mp3Player.UserControls;

namespace Mp3Player
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// https://www.wpf-tutorial.com/audio-video/how-to-creating-a-complete-audio-video-player/
    /// A large portion of the MP3 Logic is from above site.
    /// Including Execute/CanExecute Functions, timer, and slider logic.

    public partial class MainWindow : Window
    {

        //boolian flags to assist button functionality
        private bool mediaPlayerActive = false;
        private bool sliderInteractedWith = false;
        TagLib.File file = null;
        const string faultyYearStr = "Please enter a valid year.";
        string regex = "[^0-9]+";
        Match match;

        public MainWindow()
        {
            InitializeComponent();
            //timer for keeping track of playtime.
            DispatcherTimer timer = new DispatcherTimer();
            //1 tick per second
            timer.Interval = TimeSpan.FromSeconds(1);
            //
            timer.Tick += timer_Tick;
            timer.Start();
        }


        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {   //get the file location
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = "Media files (*.mp3;*.mpg;*.mpeg)|*.mp3;*.mpg;*.mpeg|All files (*.*)|*.*";
                //if we have it, use the location to get the data we need.
                if (openFileDialog.ShowDialog() == true)
                {
                    mePlayer.Source = new Uri(openFileDialog.FileName);
                    string mp3Path = openFileDialog.FileName;
                    //file hold all tag info
                    file = TagLib.File.Create(mp3Path);
                    //me data is the one being displayed in the Editor window
                    meSong.Text = file.Tag.Title;
                    meAlbum.Text = file.Tag.Album;
                    meArtist.Text = file.Tag.FirstAlbumArtist;
                    meReleaseYear.Text = Convert.ToString(file.Tag.Year);
                    //tb data is the display on main screen
                    tbAlbum.Text = file.Tag.Album;
                    tbArtist.Text = file.Tag.FirstAlbumArtist;
                    tbSong.Text = file.Tag.Title;
                    tbYear.Text = meReleaseYear.Text;
                }

            }
            catch(Exception ex)
            {
                MessageBox.Show("ERROR: " + ex);
            }
        }

        private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void Play_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mePlayer.Play();
            mediaPlayerActive = true;
        }

        private void Play_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //lets the play happen if the player isnt null and we have a file ready.
            e.CanExecute = (mePlayer != null) && (mePlayer.Source != null);
        }

        private void Pause_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mePlayer.Pause();
        }

        private void Pause_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {   
            e.CanExecute = mediaPlayerActive;
        }

        private void Stop_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = mediaPlayerActive;
        }

        private void Stop_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            mePlayer.Stop();
            mediaPlayerActive = false;
        }

        private void timer_Tick(object sender, EventArgs e)
        {   //if we have a value, the value has a duration and the user isnt using the slider
            if ((mePlayer.Source != null) && (mePlayer.NaturalDuration.HasTimeSpan) && (!sliderInteractedWith))
            {   //min slider number is 0
                sliProgress.Minimum = 0;
                //max = total time in seconds
                sliProgress.Maximum = mePlayer.NaturalDuration.TimeSpan.TotalSeconds;
                //the progress is equal to current position in song
                sliProgress.Value = mePlayer.Position.TotalSeconds;
            }
        }

        private void sliderDragStarted(object sender, DragStartedEventArgs e)
        {
            sliderInteractedWith = true;
        }

        private void sliderDragCompleted(object sender, DragCompletedEventArgs e)
        {
            sliderInteractedWith = false;
            //adjust the time to the slider posistion
            mePlayer.Position = TimeSpan.FromSeconds(sliProgress.Value);
        }

        private void sliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {   //set the time equal to the progress in the mp3 based on slider location.
            labelProgStatus.Text = TimeSpan.FromSeconds(sliProgress.Value).ToString(@"hh\:mm\:ss");
        }
        // opens/closes the edit tag window
        private void setVisiblity()
        {
            //if the player is running, turn the music off and switch the bool flag
            if(mediaPlayerActive == true)
            {
                mePlayer.Stop();
                mediaPlayerActive = false;
            }
            //if its not hidden or there is no file selected, don't show it
            if(tagEditor.Visibility == Visibility.Visible || file == null)
            {
                tagEditor.Visibility = Visibility.Hidden;
            }
            else
            {
                tagEditor.Visibility = Visibility.Visible;
            }

        }
        //save the newly updated tag data.
        private void saveMetaData()
        {
            //making sure year input is valid
            match = Regex.Match(meReleaseYear.Text, regex);
            if (match.Success)
            {
                //make textbox red and display error in the box
                meReleaseYear.Background=Brushes.Red;
                meReleaseYear.Text = faultyYearStr;
                //end the function
                return;
            }
            else
            {
                meReleaseYear.Background = Brushes.White;
            }
            //update the tags with user input data.
            file.Tag.Title = meSong.Text;
            file.Tag.Album = meAlbum.Text;
            try
            {
                file.Tag.Year = Convert.ToUInt32(meReleaseYear.Text);
            }catch(Exception e)
            {
                throw e;
            }
            file.Tag.AlbumArtists = new[] { meArtist.Text };
            try
            {
                file.Save();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //button clicked for edit
        private void editMp3_Click(object sender, RoutedEventArgs e)
        {
            setVisiblity();
        }
        //editor submit button, updates data, saves and sets visiblity.
        private void dataChangebtn_Click(object sender, RoutedEventArgs e)
        {
            saveMetaData();
            tbAlbum.Text = file.Tag.Album;
            tbArtist.Text = file.Tag.FirstAlbumArtist;
            tbSong.Text = file.Tag.Title;
            tbYear.Text = meReleaseYear.Text;
            if(meReleaseYear.Text != faultyYearStr)
            {
                setVisiblity();
            }
            
        }
        //file, close menu option
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
