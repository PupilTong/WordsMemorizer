using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Text;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace WordMemorier {
    public sealed partial class ContentDialog1 : ContentDialog {
        public event EventHandler ReadInputFileOk;
        private Windows.Storage.StorageFile file;//导入的文件
        public ContentDialog1() {
            this.InitializeComponent();
        }
        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {

            Task readFile = new Task(async (encoderSelectedIndex) => {

                DataProcesser dataproc = new DataProcesser();
                //打开要导入的文件
                List<string[]> data = await dataproc.ReadAsync(file, (DataProcesser.EncoderType)encoderSelectedIndex);
                //打开数据文件
                StorageFolder folder = ApplicationData.Current.LocalFolder;
                //如果存在就打开，不存在就创建
                file = await folder.CreateFileAsync("words.txt", CreationCollisionOption.OpenIfExists);
                List<string[]> orgData = await dataproc.ReadAsync(file, DataProcesser.EncoderType.UTF_8);
                //对比有没有一样的单词
                List<string> tempWordIndex = new List<string>();
                //tempWordIndex存储所有的英语单词，用于查找
                for (int i = 0; i < orgData.Count; i++) {//填充tempWordIndex
                    tempWordIndex.Add(orgData[i][0]);
                }
                foreach (string[] wordInformationForInput in data) {
                    //遍历要导入的数据的每一行
                    int index = tempWordIndex.IndexOf(wordInformationForInput[0]);
                    //检测英语单词是否相同

                    //下面检查汉语意思是否有相同
                    if (index != -1) { //若在原数据的index处存在相同的英语单词
                        List<string> orgWordInformationList = new List<string>(orgData[index]);
                        //首先在orgData中取出该单词的原始数组
                        foreach (string hansMeaning in wordInformationForInput) {
                            //遍历每一个汉语意思
                            if (orgWordInformationList.IndexOf(hansMeaning) == -1) {
                                //如果没有找到相同的
                                orgWordInformationList.Insert(1,hansMeaning);
                                //添加到汉语意思
                                orgWordInformationList[orgWordInformationList.Count - 1] = "0";
                                //重置剩余天数
                            }
                        }
                        orgData[index] = orgWordInformationList.ToArray();
                        //存回orgData;
                    }
                    else {
                        //如果没找到相同的
                        //添加时间戳和剩余天数
                        string[] tempWordInformationForInput = new string[wordInformationForInput.Length + 2];
                        wordInformationForInput.CopyTo(tempWordInformationForInput, 0);
                        tempWordInformationForInput[wordInformationForInput.Length] = DateTime.Today.ToString();
                        //加入今天的日期
                        tempWordInformationForInput[wordInformationForInput.Length + 1] = "0";
                        //重置剩余天数
                        orgData.Add(tempWordInformationForInput);//添加到数据表末尾
                        tempWordIndex.Add(wordInformationForInput[0]);//添加到临时词汇表末尾

                    }
                }
                //现在开始存储数据
                await dataproc.WriteAsync(file, orgData);
                ReadInputFileOk(null, null);
            }, Encoder.SelectedIndex);
            //Encoder.SelectedIndex传入选择的编码，这破uwp不支持Dispatcher.Invoke()
            if (file!=null) {
                readFile.Start();
            }
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args) {
        }

        private async void FileSelect_Click(object sender, RoutedEventArgs e) {
            FileOpenPicker picker = new FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
            picker.FileTypeFilter.Add(".csv");
            picker.FileTypeFilter.Add(".txt");
            file = await picker.PickSingleFileAsync();
            if (file != null) {
                // Application now has read/write access to the picked file
                this.FileLocation.Text = file.Path;
            }
            else {
                this.FileLocation.Text = "Operation cancelled.";
            }
        }
    }
}
