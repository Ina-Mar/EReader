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
        EpubBook book;
        int currentNavigationPage = 0;
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

        private string GetPagePath(int numInList)
        {
            EpubTextContentFile contentFile = book.ReadingOrder[numInList];
            string docPath = ExtractionPath(book) + "\\" + contentFile.FilePathInEpubArchive;
            return docPath;
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

        public async Task AddScript(string idRef)
        {
            await webView21.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync("document.getElementById("+ idRef + ").scrollIntoView({behavior: 'smooth'});");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            EpubBook book = OpenBook();
            if (book != null)
            {
                string extPath = ExtractionPath(book);
                ExtractBook(book, extPath);
                EpubTextContentFile contentFile = book.ReadingOrder[currentNavigationPage];
                string coverPage = GetPagePath(0);
                webView21.CoreWebView2.Navigate(coverPage);
                button2.Visible = true;
                button3.Visible = true;
                button2.Enabled = false;
                NavigationTree(book.Navigation);
            }
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string page = "";
            currentNavigationPage++;
            if (currentNavigationPage <= book.ReadingOrder.Count-1)
            {
                 page = GetPagePath(currentNavigationPage);

                webView21.CoreWebView2.Navigate(page);
                //currentNavigationPage++;
            }
            if (currentNavigationPage == book.ReadingOrder.Count-1)
            {
                button3.Enabled = false;
            }
            button2.Enabled=true;
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            currentNavigationPage--;
            string page = "";
            if (currentNavigationPage >= 0)
            {
                page = GetPagePath(currentNavigationPage);

                webView21.CoreWebView2.Navigate(page);
                
            }
            if (currentNavigationPage == 0)
            {
                button2.Enabled = false;
            }
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {

            
        }

        private async void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            int parentSelection, childSelection;
            childSelection = -1;
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
            label1.Text = pageInfo[2];
           
            for (int i = 0; i < book.ReadingOrder.Count(); i++)
            {
                if (book.ReadingOrder[i].FileName == pageInfo[1])
                {
                    await AddScript(pageInfo[2]);
                    currentNavigationPage = i;
                    break;
                }
            }
        }
    }
}
