﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VersOne.Epub;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.IO;
using System.IO.Compression;
using VersOne.Epub.Environment;
using Microsoft.Web.WebView2.Wpf;
using System.Xml;


namespace EReader
{
    public partial class Form1 : Form
    {
        private EpubBook book;
        private int currentNavigationPage = 0;
        private string currentId = null;
        Dictionary<string, List<string>> books;
        private string GetFile()
        {
            string filePath = "";
            OpenFileDialog ofd = new OpenFileDialog
            {
                InitialDirectory = @"G:\\",
                Title = "Ieškoti EPUB",
                DefaultExt = "epub",
                CheckFileExists = true,
                CheckPathExists = true, 
                Filter = "epub files (*.epub)|*.epub",
                FilterIndex = 0,

            };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                filePath = ofd.FileName;
            }
            return filePath;
        }

        private EpubBook OpenBook()
        { 
            string filePath = GetFile();
            try
            {
                book = EpubReader.ReadBook(filePath);
               
            }
            catch(AggregateException) 
            {
                MessageBox.Show("Negalima atverti knygos. Netinkama Epub versija");
         
            }
            return book;
        }

        private string ExtractionPath(EpubBook book)
        {
            string filePath = book.FilePath;
            string desPath = null;
            if (filePath != null)
            {
                int index = filePath.LastIndexOf("\\");
                int nameLength = filePath.Length - index - 5;
                string tempDirectory = filePath.Substring(index + 1, nameLength);
                string workDir = Directory.GetCurrentDirectory();
                desPath = @workDir + "\\temp\\" + tempDirectory;
            }
            return desPath;
        }
        private void ExtractBook(EpubBook book, string desPath)
        {
            string filePath = book.FilePath;
           
            if (!Directory.Exists(desPath))
            {
                Directory.CreateDirectory(desPath);
                ZipFile.ExtractToDirectory(filePath, desPath);
                    
            }
          
        }
        private List<string> GetNavigationItemPath(List<EpubNavigationItem> navigationItems, int numInItems, int numInNestedItems)
        {
            string docPath;
            List<string> navPath = new List<string>();
            EpubTextContentFile contentFile;
            if (numInNestedItems == -1)
            {
                contentFile = navigationItems[numInItems].HtmlContentFile;
            }
            else
            {
                contentFile = navigationItems[numInItems].NestedItems[numInNestedItems].HtmlContentFile;
            }
            EpubNavigationItemLink link = navigationItems[numInItems].Link;
            string idPath = link.Anchor;
            docPath = ExtractionPath(book) + "\\" + contentFile.FilePathInEpubArchive;
            navPath.Add(docPath);
            navPath.Add(contentFile.FileName);
            navPath.Add(idPath);
            for (int i = 0; i <navigationItems.Count; i++)
            {
                Console.WriteLine(navigationItems[i].HtmlContentFile.FileName);
            }
            return navPath;
        }

        private List<string> GetPagePath(int numInList)
        {
            EpubTextContentFile contentFile = book.ReadingOrder[numInList];
            List<string> navPath = new List<string>();
            string docPath = ExtractionPath(book) + "\\" + contentFile.FilePathInEpubArchive;
            navPath.Add(docPath);
            navPath.Add(contentFile.FileName);
            return navPath;
        }

        private List<EpubNavigationItem> GetPlainNavigation(List<EpubNavigationItem> bookItems)
        {
            List<EpubNavigationItem> navigationItems = new List<EpubNavigationItem>();
            foreach (EpubNavigationItem item in bookItems)
            {
                if (item.NestedItems == null)
                {
                    navigationItems.Add(item);
                }
                else
                {
                    foreach (EpubNavigationItem nestedItem in item.NestedItems)
                    {
                        navigationItems.Add(nestedItem);
                    }
                }
                

            }
            return navigationItems;
        }

        private void NavigationTree(List<EpubNavigationItem> navigationItems)
        {
            int numInList = 0;
            foreach (EpubNavigationItem item in navigationItems)
            {
                treeView1.Nodes.Add(item.Title);
                if (item.NestedItems != null)
                {
                    foreach (EpubNavigationItem nestedItem in item.NestedItems)
                    {
                        treeView1.Nodes[numInList].Nodes.Add(nestedItem.Title);
                    }
                }
                numInList++;
            }
        }

        private void SetCurrentNavigationPage(EpubBook book, string fileName)
        {
            
            for (int i = 0; i < book.ReadingOrder.Count; i++)
            {
                if (book.ReadingOrder[i].FileName == fileName)
                {
                    currentNavigationPage = i;
                    break;
                }
            }
        }

        private void WriteToLibrary()
        {
            if (book != null)
            {
                string libraryPath = "Library\\Library.xml";
                int itemNum;
                bool isInLib = false;
                string nodeValue;
                XmlDocument doc = new XmlDocument();
                doc.Load(libraryPath);
                XmlNodeList pathNodeList = doc.GetElementsByTagName("book");
                foreach (XmlNode node in pathNodeList)
                {
                    XmlNode path = node.SelectSingleNode("filepath");
                    if (path.InnerText == book.FilePath)
                    {
                        node.SelectSingleNode("htmldoc").InnerText = currentNavigationPage.ToString();
                        node.SelectSingleNode("pageid").InnerText = currentId;
                        isInLib = true;
                        break;

                    }
                }

                if (isInLib == false)
                {
                    XmlNodeList idNodeList = doc.GetElementsByTagName("bookid");
                    string coverPath;
                    if (idNodeList.Count != 0)
                    {
                        int numOfBooks = idNodeList.Count;
                        nodeValue = idNodeList.Item(numOfBooks - 1).InnerText;
                        itemNum = Convert.ToInt32(nodeValue) + 1;
                    }
                    else
                    {
                        itemNum = 1;
                    }

                    if (book.CoverImage != null)
                    {
                        coverPath = "Library\\Covers\\cover" + itemNum.ToString() + ".jpeg";
                    }

                    else
                    {
                        coverPath = "null";
                    }

                    XmlElement root = doc.DocumentElement;
                    XmlElement subroot = doc.CreateElement("book");
                    XmlElement bookid = doc.CreateElement("bookid");
                    XmlElement bookPath = doc.CreateElement("filepath");
                    XmlElement title = doc.CreateElement("title");
                    XmlElement author = doc.CreateElement("author");
                    XmlElement cover = doc.CreateElement("cover");
                    XmlElement docNum = doc.CreateElement("htmldoc");
                    XmlElement pageId = doc.CreateElement("pageid");
                    bookid.InnerText = itemNum.ToString();
                    bookPath.InnerText = book.FilePath;
                    title.InnerText = book.Title;
                    author.InnerText = book.Author;
                    cover.InnerText = coverPath;
                    docNum.InnerText = currentNavigationPage.ToString();
                    if (currentId == null)
                    {
                        pageId.InnerText = "null";
                    }
                    else
                    {
                        pageId.InnerText = currentId;
                    }
                    subroot.AppendChild(bookid);
                    subroot.AppendChild(bookPath);
                    subroot.AppendChild(title);
                    subroot.AppendChild(author);
                    subroot.AppendChild(cover);
                    subroot.AppendChild(docNum);
                    subroot.AppendChild(pageId);
                    root.AppendChild(subroot);
                    doc.AppendChild(root);
                    CoverLibrary(coverPath);
                }

                doc.Save(libraryPath);
            }
            
        }

        private void CoverLibrary(string coverPath)
        {
            if (book.CoverImage != null)
            {
                File.WriteAllBytes(coverPath, book.CoverImage);
            }
        }

        private Dictionary<string, List<string>> ReadFromLibrary()
        {
            Dictionary<string, List<string>> bookList = new Dictionary<string, List<string>>();
            string libraryPath = "Library\\Library.xml";
            XmlDocument doc = new XmlDocument();
            doc.Load(libraryPath);
            XmlNodeList pathNodeList = doc.GetElementsByTagName("book");
            foreach (XmlNode bookNode in pathNodeList)
            {
                string idNode = bookNode.SelectSingleNode("bookid").InnerText;
                string fileNode = bookNode.SelectSingleNode("filepath").InnerText;
                string titleNode = bookNode.SelectSingleNode("title").InnerText;
                string authorNode = bookNode.SelectSingleNode("author").InnerText;
                string coverNode = bookNode.SelectSingleNode("cover").InnerText;
                string docNode = bookNode.SelectSingleNode("htmldoc").InnerText;
                string pageidNode = bookNode.SelectSingleNode("pageid").InnerText;

                List<string> bookInfo = new List<string> { idNode, fileNode, titleNode, authorNode, coverNode, docNode, pageidNode };
               
                bookList.Add(idNode, bookInfo);
            }
            return bookList;
        }

        private void LibraryClickEvent(object sender, EventArgs e)
        {
            PictureBox pictureBox = sender as PictureBox;
            string keyName = pictureBox.Name;
            List<string> libraryInfo = books[keyName];
            string docPath = libraryInfo[1];
            book = EpubReader.ReadBook(docPath);
            if (book != null)
            {  
                CloseLibrary();
                string extPath = ExtractionPath(book);
                ExtractBook(book, extPath);
                int htmlDoc = Convert.ToInt32(libraryInfo[5]);
                List<string> pageInfo = GetPagePath(htmlDoc);
                string startPage = pageInfo[0];
                currentNavigationPage = htmlDoc;
                currentId = libraryInfo[6];
                
                webView21.CoreWebView2.Navigate(startPage);
                button2.Visible = true;
                button3.Visible = true;
                button2.Enabled = false;
                webView21.Visible = true;
                
                NavigationTree(book.Navigation);
                toolStripButton2.Enabled = true;
                toolStripButton3.Enabled = true;
                toolStripButton1.Enabled = false;
                treeView1.Enabled = true;
                treeView1.Visible = true;


            }

        }

        private void LoadLibrary()
        {
            toolStripButton2.Enabled = false;
            toolStripButton3.Enabled = false;
            toolStripButton1.Enabled = true;
            treeView1.Enabled = false;
            treeView1.Visible = false;
            
            books = ReadFromLibrary();
            PictureBox[] cover = new PictureBox[books.Count];
            for (int i= 0; i < books.Count; i++)
            {
              
                string bookKey = (i + 1).ToString();
                List<string> bookInfo = books[bookKey];
                string coverPath = "Library\\Covers\\cover"+ bookKey + ".jpeg";
                string tipString = bookInfo[3] + " - " + bookInfo[2];  
                cover[i] = new PictureBox();
                cover[i].Name = bookKey;
                cover[i].Size = new Size(140, 180);
                cover[i].SizeMode = PictureBoxSizeMode.StretchImage;
                cover[i].Image = Image.FromFile(coverPath);
                cover[i].Parent = flowLayoutPanel1;
                cover[i].Margin = new Padding(0, 0, 25, 25);
                ToolTip tip = new ToolTip();
                tip.SetToolTip(cover[i], tipString);
                cover[i].Click += new EventHandler(LibraryClickEvent);
            }
        }

        private void CloseBook()
        {  
            WriteToLibrary();
            book = null;
            books = null;
            currentNavigationPage = 0;
            currentId = null;
        }

        private void CloseLibrary()
        {
            flowLayoutPanel1.Controls.Clear();
            flowLayoutPanel1.Visible = false;
        }

        
        
        public Form1()
        {
            InitializeComponent();
            this.Resize += new System.EventHandler(this.Form_Resize);
        }

        private void Form_Resize(object sender, EventArgs e)
        { 
            
            webView21.Size = this.ClientSize - new Size(webView21.Location);
            webView21.Height -= 40;
            flowLayoutPanel1.Size = this.ClientSize - new Size(flowLayoutPanel1.Location);
            button2.Top = this.ClientSize.Height - button2.Height - 10;
            button3.Top = this.ClientSize.Height - button2.Height - 10;
            button3.Left = this.ClientSize.Width - button3.Width - 20;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string workDir = Directory.GetCurrentDirectory();
            string des_path = @workDir + "\\temp";
            if (!Directory.Exists(des_path))
            {
                Directory.CreateDirectory(des_path);
            }
            InitBrowser();
            button2.Visible = false;
            button3.Visible = false;
            webView21.Visible = false;
            LoadLibrary();
        }

        private async Task Initizated()
        {
            await webView21.EnsureCoreWebView2Async(null);        
        }

        public async void InitBrowser()
        {
            await Initizated();
            
            

        }

       
       

        private void button3_Click(object sender, EventArgs e)
        {
            string page = "";
            treeView1.Enabled = false;
            List<string> pageInfo = new List<string>();
            currentNavigationPage++;
            if (currentNavigationPage <= book.ReadingOrder.Count-1)
            {
                 pageInfo = GetPagePath(currentNavigationPage);
                 page = pageInfo[0];
                SetCurrentNavigationPage(book, pageInfo[1]);
                webView21.CoreWebView2.Navigate(page);
           
               
            }
            if (currentNavigationPage == book.ReadingOrder.Count-1)
            {
                button3.Enabled = false;
            }
            button2.Enabled=true;
            treeView1.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            currentNavigationPage--;
            string page = "";
            if (currentNavigationPage >= 0)
            {
                List<string> pageInfo = new List<string>();
                pageInfo = GetPagePath(currentNavigationPage);  
                page = pageInfo[0];
                SetCurrentNavigationPage(book, pageInfo[1]);
                webView21.CoreWebView2.Navigate(page);
                if (button3.Enabled == false)
                {
                    button3.Enabled = true;
                }
                
            }
            if (currentNavigationPage == 0)
            {
                button2.Enabled = false;
            }
        }

        
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            int parentSelection, childSelection;
            childSelection = -1;
            currentId = null;
            if (e.Node.Parent == null)
            {
                parentSelection = e.Node.Index;
            }
            else
            {
                parentSelection = e.Node.Parent.Index;
                childSelection = e.Node.Index;
            }
            List<String> pageInfo = GetNavigationItemPath(book.Navigation, parentSelection, childSelection);
            webView21.CoreWebView2.Navigate(pageInfo[0]);
            if (pageInfo[2] != null)
            {
                currentId = pageInfo[2];
            }
            
            SetCurrentNavigationPage(book, pageInfo[1]);
            
            if(currentNavigationPage <= book.ReadingOrder.Count)
            {
                button3.Enabled = true;
            }
            
        }

        private void webView21_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (currentId != null)
            {
                string scriptBody = "document.getElementById(\""+currentId+"\").scrollIntoView(true);";
                webView21.CoreWebView2.ExecuteScriptAsync(scriptBody);
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            EpubBook book = OpenBook();
            if (book != null)
            {
                string extPath = ExtractionPath(book);
                ExtractBook(book, extPath);
                List<string> pageInfo = new List<string>();
                pageInfo = GetPagePath(0);
                string coverPage = pageInfo[0];
                webView21.CoreWebView2.Navigate(coverPage);
                button2.Visible = true;
                button3.Visible = true;
                button2.Enabled = false;
                NavigationTree(book.Navigation);
                toolStripButton1.Enabled = false;
                toolStripButton2.Enabled = true;
                toolStripButton3.Enabled = true;
                webView21.Visible = true;
                treeView1.Enabled = true;
                treeView1.Visible = true;
                flowLayoutPanel1.Visible = false;
                CloseLibrary();
              

            }
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            CloseBook();
            LoadLibrary();
            webView21.Visible = false;
            flowLayoutPanel1.Visible = true;
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            string bookDescribe;
            if (book.Description != null)
            {
                bookDescribe = book.Description;    
            }
            else
            {
                bookDescribe = "Autorius: " + book.Author + "\nPavadinimas: " + book.Title;
            }
            MessageBox.Show(bookDescribe, "Apie knygą");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            WriteToLibrary();
            string workDir = Directory.GetCurrentDirectory();
            string des_path = @workDir + "\\temp";
            if (Directory.Exists(des_path))
            {
                Directory.Delete(des_path, true);
            }
        }
    }
}
