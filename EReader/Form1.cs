using System;
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

namespace EReader
{
    public partial class Form1 : Form
    {
        private EpubBook book;
        private int currentNavigationPage = 0;
        private string currentId = null;
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

        
        public Form1()
        {
            InitializeComponent();
            this.Resize += new System.EventHandler(this.Form_Resize);
        }

        private void Form_Resize(object sender, EventArgs e)
        {
            webView21.Size = this.ClientSize - new Size(webView21.Location);
            webView21.Height -= 40;
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
        }

        private async Task Initizated()
        {
            await webView21.EnsureCoreWebView2Async(null);        
        }

        public async void InitBrowser()
        {
            await Initizated();
            
            

        }

       
        private void button1_Click(object sender, EventArgs e)
        {
            EpubBook book = OpenBook();
            if (book != null)
            {
                string extPath = ExtractionPath(book);
                ExtractBook(book, extPath);
                List<string> pageInfo = new List<string>();
                pageInfo = GetPagePath(0);
                string coverPage = pageInfo[0];
                EpubTextContentFile contentFile = book.ReadingOrder[currentNavigationPage];
                webView21.CoreWebView2.Navigate(coverPage);
                button2.Visible = true;
                button3.Visible = true;
                button2.Enabled = false;
                SetCurrentNavigationPage(book, pageInfo[1]);
                NavigationTree(book.Navigation);
            }
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string page = "";
            List<string> pageInfo = new List<string>();
            currentNavigationPage++;
            if (currentNavigationPage <= book.ReadingOrder.Count-1)
            {
                 pageInfo = GetPagePath(currentNavigationPage);
                 page = pageInfo[0];
                SetCurrentNavigationPage(book, pageInfo[1]);
                webView21.CoreWebView2.Navigate(page);
                label1.Text = book.ReadingOrder.Count.ToString();
               
            }
            if (currentNavigationPage == book.ReadingOrder.Count-1)
            {
                button3.Enabled = false;
                treeView1.Enabled= false;
            }
            button2.Enabled=true;
            
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
            label1.Text = "text";
        }

        private void webView21_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (currentId != null)
            {
                string scriptBody = "document.getElementById(\""+currentId+"\").scrollIntoView(true);";
                webView21.CoreWebView2.ExecuteScriptAsync(scriptBody);
            }
        }
    }
}
