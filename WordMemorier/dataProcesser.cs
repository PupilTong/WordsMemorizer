using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace WordMemorier {
    class DataProcesser {
        public enum EncoderType {
            UTF_8,Unicode,ANSI
        }
        /// <summary>
        /// 将数据存储到目标file对象中
        /// </summary>
        /// <param name="file">file对象</param>
        /// <param name="data">数据</param>
        public async Task WriteAsync(StorageFile file, List<string[]> data) {
            string[] fileWriteString = new string[data.Count];
            for (int i = 0; i < data.Count; i++) {
                foreach (string element in data[i]) {
                    fileWriteString[i] += element + ";";
                }
                fileWriteString[i] = fileWriteString[i].Remove(fileWriteString[i].Length - 1);
            }
            await FileIO.WriteLinesAsync(file, fileWriteString);
        }
        /// <summary>
        /// 把目标file文件的数据读出，一行为一组数据，首数据为单词，后跟多个汉语意思。自动转码为UTF-8
        /// </summary>
        /// <param name="file">file对象</param>
        /// <param name="encoderType">编码类型</param>
        /// <returns></returns>
        public async Task<List<string[]>> ReadAsync(StorageFile file, EncoderType encoderType = EncoderType.UTF_8) {
            IBuffer fileDatabuffer;
            fileDatabuffer = await FileIO.ReadBufferAsync(file); //从file对象读取数据到缓冲区
            List<string[]> csvData = new List<string[]>();

            //使用CodePagesEncodingProvider去注册扩展编码。
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding fileEncoding;
            switch (encoderType) {
                default:
                    fileEncoding = Encoding.GetEncoding("GBK");
                    break;
                case EncoderType.UTF_8:
                    fileEncoding = Encoding.UTF8;
                    break;
                case EncoderType.Unicode:
                    fileEncoding = Encoding.Unicode;
                    break;
                case EncoderType.ANSI:
                    fileEncoding = Encoding.GetEncoding("GBK");
                    break;
            }
            if (fileDatabuffer.Length != 0) {
                using (MemoryStream dataStream = new MemoryStream(Encoding.Convert(fileEncoding, Encoding.UTF8, fileDatabuffer.ToArray()))) {
                    //把读取的缓冲区对象转换为可以被dataReader读取的MemoryStream对象
                    //同时将编码转换成UTF-8
                    using (StreamReader dataReader = new StreamReader(dataStream, Encoding.UTF8)) {
                        while (!dataReader.EndOfStream) {
                            //读入一行
                            string[] oneLine = dataReader.ReadLine().Split(',');
                            //oneLine存入一行的信息，一个格一个数组元素
                            //判断是否有空行
                            bool oneLineIsNull = false;
                            for (int i = 0; i < oneLine.Length; i++) {//是否有空的格子
                                if (oneLine[i] == "") {
                                    oneLineIsNull = true;//如果有就设为真，结束循环，本行无用
                                    break;
                                }
                                else {
                                    //oneLine[i] = oneLine[i].Remove(oneLine[i].IndexOf(' '), 1);//消去空格、中文括号，中文句号，中文逗号
                                    oneLine[i] = oneLine[i].Replace('；', ';');//替换中文分号
                                }
                            }
                            //将多个汉语意思分开
                            List<string> dataSplitedList = new List<string>();
                            if (!oneLineIsNull) {
                                for (int i = 0; i < oneLine.Length; i++) {
                                    dataSplitedList.AddRange(oneLine[i].Split(';'));
                                    //Split后加到新List的末尾
                                }
                                csvData.Add(dataSplitedList.ToArray());//把新List加入List<stringp[]>保存
                            }
                        }
                    }
                }
            }
            return csvData;
        }
    }
}
