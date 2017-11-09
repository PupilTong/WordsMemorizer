using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace WordMemorier
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        List<string[]> todayTask = new List<string[]>();
        List<string> memorizedWord = new List<string>();
        int todayTaskWordPointer=0;
        int todayMemorizedCount;
        int todayTaskCount;

       

        public MainPage()
        {
            this.InitializeComponent();
        }
        /// <summary>
        /// 刷新今日任务
        /// </summary>
        /// <returns>返回一个异步Task</returns>
        private async Task RefreshAsync() {
            //填充今日任务列表
            Random randomNumber = new Random();
            DataProcesser dataproc = new DataProcesser();
            List<string[]> data =await dataproc.ReadAsync(
                await ApplicationData.Current.LocalFolder.CreateFileAsync("words.txt", CreationCollisionOption.OpenIfExists)
                );
            foreach (string[] wordInformation in data) {
                DateTime formerMemorizeDate = DateTime.Parse(wordInformation[wordInformation.Length - 2]);
                TimeSpan span = DateTime.Today - formerMemorizeDate;
                if (span.Days >= Convert.ToInt16(wordInformation[wordInformation.Length - 1])) {
                    //如果大于设定的时间间隔，添加到今日任务
                    string[] oneWord = new string[6];
                    //[单词],[汉语意思]x4,[正确选项]
                    oneWord[0] = wordInformation[0];
                    int correctSelection = randomNumber.Next(1, 5);
                    //生成正确选项的序号
                    oneWord[5] = correctSelection.ToString();
                    //将正确选项的序号存入
                    oneWord[correctSelection] = wordInformation[randomNumber.Next(1, wordInformation.Length - 2)];
                    //将正确答案存入正确选项对应的序号
                    for (int i = 1; i < 5; i++) {
                        if (i != correctSelection) {
                            //在其他选项填入随机的汉语意思
                            bool randomWordOK = false;
                            int randomWordIndex;
                            while (!randomWordOK) {
                                randomWordIndex = randomNumber.Next(0, data.Count);
                                //随机到一个单词组
                                if (data[randomWordIndex][0] != oneWord[0]) {
                                    //随机的单词不是该单词本身
                                    int randomWordElementCount = data[randomWordIndex].Length - 2;
                                    for(int j = 1; j < randomWordElementCount; j++) {
                                        int randomHansIndex = randomNumber.Next(1, randomWordElementCount);
                                        if (data[randomWordIndex][j]!=oneWord[correctSelection]) {
                                            //随机的汉语意思与正确汉语的意思不同
                                            oneWord[i] = data[randomWordIndex][j];
                                            randomWordOK = true;
                                            break;
                                        }
                                    }
                                }
                                //多次随机直到随机到不同的单词
                            }
                            
                        }
                    }
                    todayTask.Add(oneWord);
                }
            }
            //填充完毕

            //设置保存已背会单词的线程
            Task saveWords = new Task(async () => {
                await Task.Delay(20000);
                while (todayTask.Count != 0) {
                    //仍旧有今日任务，继续循环
                    await Task.Delay(20000);
                    if (memorizedWord.Count >= 10) {
                        await SaveMemorizedWords();
                    }
                }
                //否则保存一次，退出线程
                await SaveMemorizedWords();
            });
            saveWords.Start();
            

            //设置UI
            todayTaskCount = todayTask.Count;
            await Sum.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,()=> {
                this.Sum.Text = "共: " + data.Count.ToString();
                this.Today.Text = "今日: 0 / " + todayTaskCount.ToString();
            });
            GC.Collect();

        }
        /// <summary>
        /// 显示下一个单词
        /// </summary>
        /// <returns>返回一个异步Task</returns>
        private async Task NextWordAsync() {
            await Task.Delay(2000);
            if (todayTask != null && todayTask.Count != 0 ) {
                if (todayTaskWordPointer == todayTask.Count) {
                    todayTaskWordPointer = 0;//从头开始
                    await SaveUnMemorizedWords();
                    //从头开始之后，记录下本次没有记住的单词，重置这些单词的间隔天数信息

                }
                await Word.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                    SetButtonContexColor(new SolidColorBrush(Colors.Black),0);
                    Word.Text = todayTask[todayTaskWordPointer][0];
                    //显示英文单词
                    HansMeaning.Visibility = Visibility.Collapsed;
                    //隐藏汉语
                    HansMeaning.Text = todayTask[todayTaskWordPointer][
                        Convert.ToInt16(todayTask[todayTaskWordPointer][5])
                        ];
                    //设置汉语
                    Button_A.Content = todayTask[todayTaskWordPointer][1];
                    Button_B.Content = todayTask[todayTaskWordPointer][2];
                    Button_C.Content = todayTask[todayTaskWordPointer][3];
                    Button_D.Content = todayTask[todayTaskWordPointer][4];
                    //设置每个按钮的文字
                });
            }
            else {
                await SaveMemorizedWords();
                //保存下刚刚已经记住的词汇
                await MainPage1.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () => {
                    MessageDialog alert = new MessageDialog("没有什么要学的了，再去找点吧！");
                    await alert.ShowAsync();
                });
                //弹出对话框
            }
        }
        private void SelectASelection(int index) {
            if (index == Convert.ToInt16(todayTask[todayTaskWordPointer][5])) {
                //如果相等的话,选对了
                memorizedWord.Add(todayTask[todayTaskWordPointer][0]);
                //记录下选对的单词
                todayTask.RemoveAt(todayTaskWordPointer);
                //选对之后从todayTask单词表里面移除

                SolidColorBrush correctGreen = new SolidColorBrush(Colors.DarkGreen);
                SetButtonContexColor(correctGreen, index);
                HansMeaning.Foreground = correctGreen;
                HansMeaning.Visibility = Visibility.Visible;
                todayMemorizedCount++;
                this.Today.Text = "今日: " + todayMemorizedCount + " / " + todayTaskCount.ToString();
            }
            else {
                //错了
                SolidColorBrush errorRead = new SolidColorBrush(Colors.OrangeRed);
                SetButtonContexColor(errorRead, index);
                HansMeaning.Foreground = errorRead;
                HansMeaning.Visibility = Visibility.Visible;

                todayTaskWordPointer++;
            }
            NextWordAsync();
        }
        /// <summary>
        /// 设置按钮中文字的颜色
        /// </summary>
        /// <param name="textColor">绘制文字的画笔，推荐SolidColorBrush</param>
        /// <param name="index">序列号为1-4</param>
        private void SetButtonContexColor(Brush textColor, int index) {
            switch (index) {
                default:
                    for (int i = 1; i < 5; i++) {
                        SetButtonContexColor(new SolidColorBrush(Colors.Black), i);
                    }
                    break;
                case 1:
                    Button_A.Foreground = textColor;
                    break;
                case 2:
                    Button_B.Foreground = textColor;
                    break;
                case 3:
                    Button_C.Foreground = textColor;
                    break;
                case 4:
                    Button_D.Foreground = textColor;
                    break;
            }
        }
        private async void Input_ClickAsync(object sender, RoutedEventArgs e) {
            SubMainGrid.Visibility = Visibility.Collapsed;
            this.ProcessingRing.IsActive = true;
            ContentDialog1 a = new ContentDialog1();
            a.ReadInputFileOk += InputFileOkAsync;
            await a.ShowAsync();
            

        }
        private async void InputFileOkAsync(object sender, EventArgs e) {
            await this.MainPage1.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                this.SubMainGrid.Visibility = Visibility.Visible;
                this.ProcessingRing.IsActive = false;
            });
            GC.Collect();
            await RefreshAsync();
        }
        private string GetNextMemorizingDay(string former) {
            switch (former) {
                default:
                    return "0";
                case "0":
                    return "1";
                case "1":
                    return "2";
                case "2":
                    return "4";
                case "4":
                    return "7";
                case "7":
                    return "7";
            }
        }
        private async Task SaveMemorizedWords() {
            DataProcesser dataproc = new DataProcesser();
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("words.txt", CreationCollisionOption.OpenIfExists);
            List<string[]> data = await dataproc.ReadAsync(file);
            lock (memorizedWord) {
                foreach (string word in memorizedWord) {
                    for (int i = 0; i < data.Count; i++) {
                        if (data[i][0] == word) {
                            //找到对应的单词
                            data[i][data[i].Length - 2] = DateTime.Today.ToString();
                            data[i][data[i].Length - 1] = GetNextMemorizingDay(data[i][data[i].Length - 1]);
                            break;
                        }
                    }
                }
                memorizedWord.Clear();
            }
            await dataproc.WriteAsync(file, data);
            data.Clear();
            GC.Collect();

        }
        private async Task SaveUnMemorizedWords() {
            DataProcesser dataproc = new DataProcesser();
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("words.txt", CreationCollisionOption.OpenIfExists);
            List<string[]> data = await dataproc.ReadAsync(file);
            foreach (string[] wordInformation in todayTask) {
                for (int i = 0; i < data.Count; i++) {
                    if (data[i][0] == wordInformation[0]) {
                        //找到对应的单词
                        data[i][data[i].Length - 2] = DateTime.Today.ToString();
                        data[i][data[i].Length - 1] = "0";
                        break;
                    }
                }
            }
            memorizedWord.Clear();
            await dataproc.WriteAsync(file, data);
            data.Clear();
            GC.Collect();
        }
        private void MainPage1_Loading(FrameworkElement sender, object args) {
            //设置刷新任务
            Task refreshData = new Task(async () => {
                await RefreshAsync();
                await NextWordAsync();
            });


            refreshData.Start();
            //saveWords.Start();
        }

        private void Button_A_Click(object sender, RoutedEventArgs e) {
            SelectASelection(1);
        }

        private void Button_B_Click(object sender, RoutedEventArgs e) {
            SelectASelection(2);
        }

        private void Button_C_Click(object sender, RoutedEventArgs e) {
            SelectASelection(3);
        }

        private void Button_D_Click(object sender, RoutedEventArgs e) {
            SelectASelection(4);
        }

        private void MainPage1_Unloaded(object sender, RoutedEventArgs e) {
        }
    }

}
