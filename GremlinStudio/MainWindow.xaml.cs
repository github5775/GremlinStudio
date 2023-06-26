
using Gremlin.Net.Driver;
using Gremlin.Net.Driver.Exceptions;
using Gremlin.Net.Structure.IO.GraphSON;
using GremlinUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Graphyte;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.Options;
using System.Threading;
using MaterialDesignThemes.Wpf;
using Wpf.Notification;
using System.Net.WebSockets;
using System.Net;

namespace GremlinStudio
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region hk
        private static GremlinServer _server = null;
        private static GremlinClient _ctx = null;
        private static readonly Stopwatch _sw = new Stopwatch();
        private static string _newQueryTabDefaultQuery = "g.V().range(0,3)";
        private readonly List<Exception> _exceptions = new List<Exception>();
        //internal readonly TabablzControl InitialTabablzControl;
        private List<TabItem> _queryTabItems;
        private TabItem _tabAdd;
        private string _vertexIn = ".inE().limit(10).as('QueryEdge').outV().as('QueryVertex').select('QueryEdge', 'QueryVertex')";
        private string _vertexOut = ".outE().limit(11).as('QueryEdge').inV().as('QueryVertex').select('QueryEdge', 'QueryVertex')";
        private List<string> _queryExecutionResults = null;

        private ResultSet<dynamic> _results = null;
        private IOptions<AppSettings> _options;

        private Vertex lastVertexTouched = null;
        private VertexType lastVertexTypeTouched;
        private string _lastClickedOn = "";

        //private static bool _isDoubleClick = false;

        private static Timer _timer = null;
        #endregion

        #region ctor
        public MainWindow(IOptions<AppSettings> options)
        {
            _options = options;

            if (DateTime.UtcNow > DateTime.Parse("12/31/2023"))
            {
                return;
            }

            try
            {
                InitializeComponent();

                tabInit();

                ConnectToServer();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
        }
        #endregion


        #region query tab helpers
        private void tabInit()
        {
            Debug.WriteLine($"InitQueryTabs()");
            // initialize tabItem array
            _queryTabItems = new List<TabItem>();

            // add a tabItem with + in header 
            _tabAdd = new TabItem();
            _tabAdd.Header = "+";
            // tabAdd.MouseLeftButtonUp += new MouseButtonEventHandler(tabAdd_MouseLeftButtonUp);

            _queryTabItems.Add(_tabAdd);

            // add first tab
            this.tabAdd();

            // bind tab control
            tabQueries.DataContext = _queryTabItems;

            tabQueries.SelectedIndex = 0;

            //(tabQueries.SelectedContent as TextBox).Text = _newQueryTabDefaultQuery;

        }
        private TabItem tabAdd()
        {
            int count = _queryTabItems.Count;
            Debug.WriteLine($"tabAdd(): {count}");

            // create new tab item
            TabItem tab = new TabItem();

            tab.Header = string.Format("query {0}", count);
            tab.Name = string.Format("tab{0}", count);
            tab.HeaderTemplate = tabQueries.FindResource("TabHeader") as DataTemplate;

            //tab.MouseDoubleClick += new MouseButtonEventHandler(tab_MouseDoubleClick);

            // add controls to tab item, this case I added just a textbox
            TextBox txt = new TextBox();
            txt.Name = "txt";

            tab.Content = txt;

            // insert tab item right before the last (+) tab item
            _queryTabItems.Insert(count - 1, tab);

            return tab;
        }

        private void tab_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine($"QueryTab_MouseLeftButtonUp()");
            if (sender is Button)
            {
                Debug.WriteLine($"{(sender as Button).Content}");
            }
            // clear tab control binding
            tabQueries.DataContext = null;

            TabItem tab = this.tabAdd();

            // bind tab control
            tabQueries.DataContext = _queryTabItems;

            // select newly added tab item
            tabQueries.SelectedItem = tab;

            tabQueries.SelectedIndex = tabQueries.Items.Count - 1;

            //tabQueries[tabQueries.SelectedIndex] as TextBox).Text = _resetQuery;
        }

        private void tab_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine($"QueryTab_MouseDoubleClick()");
            if (sender is Button)
            {
                Debug.WriteLine($"{(sender as Button).Content}");
            }
            TabItem tab = sender as TabItem;

            TabProperty dlg = new TabProperty();

            // get existing header text
            dlg.txtTitle.Text = tab.Header.ToString();

            if (dlg.ShowDialog() == true)
            {
                // change header text
                tab.Header = dlg.txtTitle.Text.Trim();
            }
        }

        private void tabDelete_Click(object sender, RoutedEventArgs e)
        {
            string tabName = (sender as Button).CommandParameter.ToString();

            var item = tabQueries.Items.Cast<TabItem>().Where(i => i.Name.Equals(tabName)).SingleOrDefault();

            TabItem tab = item as TabItem;

            if (tab != null)
            {
                if (_queryTabItems.Count < 3)
                {
                    MessageBox.Show("Cannot remove last tab.");
                }
                else if (MessageBox.Show(string.Format("Are you sure you want to remove the tab '{0}'?", tab.Header.ToString()),
                    "Remove Tab", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    // get selected tab
                    TabItem selectedTab = tabQueries.SelectedItem as TabItem;

                    // clear tab control binding
                    tabQueries.DataContext = null;

                    _queryTabItems.Remove(tab);

                    // bind tab control
                    tabQueries.DataContext = _queryTabItems;

                    // select previously selected tab. if that is removed then select first tab
                    if (selectedTab == null || selectedTab.Equals(tab))
                    {
                        selectedTab = _queryTabItems[0];
                    }
                    tabQueries.SelectedItem = selectedTab;
                    (tabQueries.SelectedContent as TextBox).Text = _newQueryTabDefaultQuery;
                }
            }
        }
        private void tab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabItem tab = tabQueries.SelectedItem as TabItem;
            if (tab == null) return;

            if (tab.Equals(_tabAdd))
            {
                // clear tab control binding
                tabQueries.DataContext = null;

                TabItem newTab = this.tabAdd();

                // bind tab control
                tabQueries.DataContext = _queryTabItems;
                (newTab.Content as TextBox).Text = _newQueryTabDefaultQuery;
                // select newly added tab item
                tabQueries.SelectedItem = newTab;

                // your code here...
                Debug.WriteLine("'");
                //(tabQueries.SelectedContent as TextBox).Text = _resetQuery;
            }
            else
            {

            }
        }
        #endregion

        #region db helpers
        private void ConnectToServer()
        {         
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                string endpoint = config.AppSettings.Settings["GremlinEndpoint"].Value;
                string key = config.AppSettings.Settings["AuthKey"].Value;
                string db = config.AppSettings.Settings["CosmosDb"].Value;
                string collection = config.AppSettings.Settings["Collection"].Value;

                if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(collection))
                {
                    _ = MessageBox.Show("Please complete your connection settings...");
                    return;
                }

                _server = null;
                _server = new GremlinServer(endpoint.Replace("https://", "").Replace("wss://", "").Replace(":443/", ""), 443, enableSsl: true,

                                                                               username: "/dbs/" + db + "/colls/" + collection,
                                                                               password: key);
            }
            catch (Exception ex)
            {
                if (_server == null)
                {
                    MessageBox.Show("Please check your connection settings...");
                    return;
                }

                MessageBox.Show(ex.Message);
                return;
            }

        }
        private void ConnectToClient()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            string endpoint = config.AppSettings.Settings["GremlinEndpoint"].Value;
            string key = config.AppSettings.Settings["AuthKey"].Value;
            string db = config.AppSettings.Settings["CosmosDb"].Value;
            string collection = config.AppSettings.Settings["Collection"].Value;

            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(db) || string.IsNullOrWhiteSpace(collection))
            {
                _ = MessageBox.Show("Please complete your connection settings...");
                return;
            }

            string containerLink = $"/dbs/{db}/colls/{collection}";

            _server = new GremlinServer(endpoint, 443, enableSsl: true,
                                                               username: containerLink,
                                                               password: key);
            ConnectionPoolSettings connectionPoolSettings = new ConnectionPoolSettings()
            {
                MaxInProcessPerConnection = 10,
                PoolSize = 30,
                ReconnectionAttempts = 3,
                ReconnectionBaseDelay = TimeSpan.FromMilliseconds(500)
            };

            var webSocketConfiguration =
                new Action<ClientWebSocketOptions>(options =>
                {
                    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
                });

            _ctx = new GremlinClient(_server, new GraphSON2Reader(),
                new GraphSON2Writer(),
                "application/vnd.gremlin-v2.0+json",
                connectionPoolSettings,
                webSocketConfiguration);
        }
        //public async Task<List<T>> ExecuteQueryAsync<T>(string query)
        //{
        //    return ConvertQueryResults<T>(await ExecuteQueryAsync(query));
        //}
        public async Task<List<string>> ExecuteQueryAsync(string query, bool updateStats = true)
        {
            Debug.WriteLine($"-------------------------------------------------------");
            Debug.WriteLine($"Q==>{query}");
            if (string.IsNullOrWhiteSpace(query))
            {
                return new List<string>();
            }
            int limit = 20;

            List<string> vs = new List<string>();
            _exceptions.Clear();

            if (_ctx == null)
            {
                Debug.WriteLine($"------------------------------------------------------- NEW CLIENT");
                _ctx = new GremlinClient(_server, new GraphSON2Reader(), new GraphSON2Writer(), "application/vnd.gremlin-v2.0+json");

            }

            if (_ctx == null)
            {
                Debug.WriteLine($"------------------------------------------------------- NULL");
            }

            if (_ctx != null)
            {
                try
                {
                    int pos = query.LastIndexOf(".limit");

                    if (pos == -1 || pos < query.Length - 14)
                    {
                        pos = query.LastIndexOf(".executionProfile()");

                        if (pos < 0)
                        {
                            query += $".limit({limit})";
                        }
                    }
                    _sw.Reset();
                    _sw.Start();
                    _results = await _ctx.SubmitAsync<dynamic>(query);
                    _sw.Stop();
                    Double charge = Convert.ToDouble(_results.StatusAttributes["x-ms-total-request-charge"]);
                    Debug.WriteLine($"RU: {charge:N2}, {_results.Count}, {_sw.ElapsedMilliseconds} ms");
                    if (updateStats)
                    {
                        textStatusResults.Text = $"Count: {_results.Count}, {_sw.ElapsedMilliseconds:N0} ms, {charge:N2} RU\r\n";
                    }

                    //textResults.Text = "[";
                    int counter = 0;

                    foreach (dynamic result in _results.Take(limit))
                    {
                        vs.Add(JsonConvert.SerializeObject(result));
                    }

                    return vs;
                }
                catch (NoConnectionAvailableException ex)
                {
                    _exceptions.Add(new NoConnectionAvailableException(query + "\r\n" + ex.Message + "\r\n" + ex.StackTrace));
                    _ctx.Dispose();
                    _ctx = null;
                    textResults.Text = "Connection Failed:  Please check connection settings.";
                    return null;
                }
                catch (ResponseException ex)
                {
                    _exceptions.Add(new ResponseException(ex.StatusCode, ex.StatusAttributes, query + "\r\n" + ex.Message + "\r\n" + ex.StackTrace + "\r\n" + ex.StatusCode.ToString()));
                    _ctx.Dispose();
                    _ctx = null;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"------------------------------------------------------- FAILURE");
                    //_exceptions.Add(new Exception(_user.UserName + "\r\n" + ex.Message + "\r\n" + query));
                    _exceptions.Add(new Exception(query + "\r\n" + ex.Message + "\r\n" + ex.StackTrace));

                    _ctx.Dispose();
                    _ctx = null;

                    if (ex.Message == "DocumentClientException")
                    {
                        textResults.Text = "Connection Failed:  Please check connection settings.";
                        return null;
                    }
                }
            }
            else
            {
                //
            }

            textResults.Text = "";
            foreach (var item in _exceptions)
            {
                textResults.Text += "\r\n" + item.Message;
                Console.WriteLine(item.Message);
                if (item.InnerException != null)
                {
                    Console.WriteLine(item.InnerException.Message);
                    textResults.Text += "\r\n" + item.InnerException.Message;
                }

                ResponseException exception = item as ResponseException;

                if (exception.StatusAttributes.TryGetValue("x-ms-status-code", out object statusCode))
                {
                    if (Convert.ToInt32(statusCode) == 404)
                    {
                        textResults.Text = "Connection Failed:  Please check connection settings.";
                        return null;
                    }
                }

            }
            return null;
        }

        private void LoadGraph(List<string> vs)
        {
            if (vs.Count > 0)
            {
                graphite.Children.Clear();

                foreach (var item in vs)
                {
                    var vertexJson = JValue.Parse(item);

                    var vertex = new Vertex()
                    {
                        ID = vertexJson.SelectToken("id").ToString(),
                        Type = VertexType.Standard
                    };

                    string vertexName = GetVertexProperty(item, "Name");

                    if (!string.IsNullOrWhiteSpace(vertexName))
                    {
                        vertex.Title = vertexName;
                    }

                    InfoBag bag = new InfoBag();
                    bag.label = vertexJson.SelectToken("label").ToString();
                    string vertexPart = GetVertexProperty(item, "PartitionId");

                    if (!string.IsNullOrWhiteSpace(vertexPart))
                    {
                        bag.part = vertexPart;
                    }
                    vertex.Info = bag;
                    graphite.AddVertex(vertex);
                }

                //tabResults.SelectedIndex = 2;
            }
        }

        private string GetVertexProperty(string vertexJson, string propertyName)
        {
            //Vertex vertex = JsonConvert.DeserializeObject<Vertex>(vertexJson);

            var vertex = JValue.Parse(vertexJson);

            if (vertex != null)
            {
                var xx = vertex.SelectToken("properties");
                if (xx != null)
                {
                    var yy = xx.SelectToken(propertyName);

                    if (yy != null)
                    {
                        return yy.Last.Last.Last.ToString();
                    }
                }
            }

            return "";
        }
        //public async Task<List<string>> ExecutionQueryAsync(string query)
        //{
        //    query += ".executionProfile()";

        //    Debug.WriteLine($"-------------------------------------------------------");
        //    Debug.WriteLine($"Q==>{query}");

        //    List<string> vs = new List<string>();
        //    _exceptions.Clear();

        //    if (_ctx == null)
        //    {
        //        Debug.WriteLine($"------------------------------------------------------- NEW CLIENT");
        //        _ctx = new GremlinClient(_server, new GraphSON2Reader(), new GraphSON2Writer(), "application/vnd.gremlin-v2.0+json");
        //    }

        //    if (_ctx != null)
        //    {
        //        try
        //        {
        //            _sw.Reset();
        //            _sw.Start();
        //            _results = await _ctx.SubmitAsync<dynamic>(query);
        //            _sw.Stop();
        //            int charge = Convert.ToInt32(_results.StatusAttributes["x-ms-total-request-charge"]);
        //            Debug.WriteLine($"RU: {charge}");

        //            textStatusResults.Text = "";
        //            textResults.Text = "[";
        //            int counter = 0;

        //            foreach (dynamic result in _results.Take(200))
        //            {
        //                //string output = JsonConvert.SerializeObject(result);
        //                vs.Add(JsonConvert.SerializeObject(result));
        //                //Console.WriteLine(String.Format("\tResult:\n\t{0}", output));
        //                counter++;
        //                if (counter > 1)
        //                {
        //                    textResults.Text += ",\r\n";
        //                }
        //                textResults.Text += JsonConvert.SerializeObject(result, Formatting.None);
        //            }
        //            textResults.Text += "]";
        //            string json = JsonConvert.SerializeObject(textResults.Text);
        //            json = textResults.Text;
        //            Console.WriteLine(json); // single line JSON string

        //            string jsonFormatted = JValue.Parse(json).ToString(Formatting.Indented);
        //            textResults.Text = jsonFormatted;
        //            JsonViewer.Load(json);
        //            JsonViewer.ExpandAll();
        //            return vs;
        //        }
        //        catch (NoConnectionAvailableException ex)
        //        {
        //            _exceptions.Add(new NoConnectionAvailableException(query + "\r\n" + ex.Message + "\r\n" + ex.StackTrace));
        //            _ctx.Dispose();
        //            _ctx = null;
        //        }
        //        catch (ResponseException ex)
        //        {
        //            _exceptions.Add(new ResponseException(ex.StatusCode, ex.StatusAttributes, query + "\r\n" + ex.Message + "\r\n" + ex.StackTrace + "\r\n" + ex.StatusCode.ToString()));
        //            _ctx.Dispose();
        //            _ctx = null;
        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine($"------------------------------------------------------- FAILURE");
        //            //_exceptions.Add(new Exception(_user.UserName + "\r\n" + ex.Message + "\r\n" + query));
        //            _exceptions.Add(new Exception(query + "\r\n" + ex.Message + "\r\n" + ex.StackTrace));

        //            _ctx.Dispose();
        //            _ctx = null;
        //        }
        //    }
        //    else
        //    {
        //        //
        //    }

        //    textResults.Text = "";
        //    foreach (var item in _exceptions)
        //    {
        //        textResults.Text += "\r\n" + item.Message;
        //        Console.WriteLine(item.Message);
        //        if (item.InnerException != null)
        //        {
        //            Console.WriteLine(item.InnerException.Message);
        //            textResults.Text += "\r\n" + item.InnerException.Message;
        //        }
        //    }

        //    return null;
        //}
        //public List<T> ConvertQueryResults<T>(IEnumerable<string> resultList)
        //{
        //    Stopwatch sw = new Stopwatch();
        //    sw.Start();
        //    //List<T> list = resultList.Select(result => (T)JsonConvert.DeserializeObject<T>(result.Replace("properties", "Properties"))).ToList();
        //    //sw.Stop();
        //    //Debug.WriteLine($"result count: {list.Count} / {sw.ElapsedMilliseconds}");
        //    //sw.Reset();
        //    //sw.Start();
        //    List<T> list = (List<T>)JsonConvert.DeserializeObject<IEnumerable<T>>("[" + string.Join(",", resultList) + "]");
        //    sw.Stop();

        //    Debug.WriteLine($"result count/ms: {list.Count} / {sw.ElapsedMilliseconds}");
        //    //return (List<T>)JsonConvert.DeserializeObject<IEnumerable<T>>("[" + string.Join(",", resultList) + "]");
        //    return list;
        //}
        #endregion

        private static void _timer_Tick(object state)
        {
            Debug.Write("Tick! ");
            Debug.WriteLine(
                Thread.CurrentThread.
                ManagedThreadId.ToString());

            Thread.Sleep(500);
            // (tabQueries.SelectedContent as TextBox).Text = _newQueryTabDefaultQuery;

            _timer.Dispose();
        }
        private async void button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string buttonText = button.Content.ToString();

            string queryText = "";

            TextBox textBox = (tabQueries.SelectedContent as TextBox);

            switch (buttonText)
            {
                case "Execute":
                    queryText = (tabQueries.SelectedContent as TextBox).Text.ToString().Trim();
                    if (string.IsNullOrWhiteSpace(queryText))
                    {
                        return;
                    }

                    if (_ctx == null)
                    {
                        ConnectToClient();

                        if (_ctx == null)
                        {
                            MessageBox.Show("Please check your connection settings...");
                            return;
                        }
                    }

                    UpdateQueries(queryText);
                    _queryExecutionResults = await ExecuteQueryAsync(queryText);
                    if (_queryExecutionResults != null && _queryExecutionResults.Count > 0)
                    {
                        JToken xx = JToken.Parse(_queryExecutionResults[0]);

                        DisplayQueryResults(_queryExecutionResults, false);
                    }
                    break;
                case "View Plan":
                    queryText = (tabQueries.SelectedContent as TextBox).Text.ToString().Trim();
                    if (string.IsNullOrWhiteSpace(queryText))
                    {
                        return;
                    }
                    queryText = (tabQueries.SelectedContent as TextBox).Text.ToString().Trim() + ".limit(1).executionProfile()";
                    UpdateQueries(queryText);

                    _queryExecutionResults = await ExecuteQueryAsync(queryText);
                    if (_queryExecutionResults != null && _queryExecutionResults.Count > 0)
                    {
                        DisplayQueryResults(_queryExecutionResults, true);
                    }
                    break;

                case "g.V()":
                    textBox.Text = "g.V()";
                    break;
                case "g.V(['part','id'])":
                    textBox.Text = "g.V(['part','id'])";
                    break;

                case "addV()":
                    Dictionary<string, object> props = new Dictionary<string, object>();
                    props.Add("part", "id");
                    props.Add("propname", "propvalue");
                    textBox.Text = GremlinHelper.AddVertexQuery("label", props);
                    break;
                case "addE()":
                    textBox.Text = "g.addE()";
                    break;

                case "has prop":
                    textBox.Text += ".has('prop','value')";
                    SelectWord(textBox, "prop");
                    break;
                case "set prop":
                    textBox.Text += ".property('prop','value')";
                    break;

                case "Starts With":
                    textBox.Text += ".has('prop',TextP.startingWith('value'))";
                    break;
                case "! Starts With":
                    textBox.Text += ".has('prop',TextP.notStartingWith('value'))";
                    break;

                case "Ends With":
                    textBox.Text += ".has('prop',TextP.endingWith('value'))";
                    break;
                case "! Ends With":
                    textBox.Text += ".has('prop',TextP.notEndingWith('value'))";
                    break;

                case "Contains":
                    textBox.Text += ".has('prop',TextP.containing('value'))";
                    break;
                case "! Contains":
                    textBox.Text += ".has('prop',TextP.notContaining('value'))";
                    break;

                case "repeat":
                    textBox.Text += ".repeat(out().simplePath())";
                    break;
                //case "out('label')":
                //textBox.Text += ".out('label')";
                //break;

                case "until":
                    textBox.Text += ".until(has('label','labelName')).range(0,3)";
                    break;
                //case "times(2)":
                //    textBox.Text += ".times(2)";
                //    break;

                case "order ASC":
                    textBox.Text += ".order().by('Created_On',incr)";
                    break;
                case "order DESC":
                    textBox.Text += ".order().by('Created_On',decr)";
                    break;
                case "sample traversal":
                    textBox.Text = "g.V().has('label','user').has('First_Name',TextP.startingWith('Mic')).outE().otherV()";
                    break;
                case "shortest path":
                    textBox.Text = "g.V().has('code','AUS').repeat(out().simplePath()).until(has('code','AGR')).path().by('code').limit(10)";
                    break;
                case "list w/project":
                    textBox.Text = "g.V().has('label','user').range(0,3).project('id','First_Name','Last_Name').by(id()).by('First_Name').by('Last_Name')";
                    break;
                case "query w/count and paging":
                    textBox.Text = "g.V().range(0,10).fold().as('list','count').select('list','count').by(range(local,0,3)).by(count(local))";
                    break;

                default:
                    textBox.Text += "." + buttonText;
                    break;
            }
        }

        private void SelectWord(TextBox textBox, string word)
        {
            int position = textBox.Text.IndexOf(word, StringComparison.CurrentCultureIgnoreCase);

            if (position < 0)
            {
                return;
            }

            textBox.Select(position, word.Length);
        }

        private void DisplayQueryResults(List<string> results, bool onlyDisplayJson)
        {
            bool populateGraph = false;
            TabItem tabGraph = tabResults.Items[1] as TabItem;
            tabGraph.Visibility = Visibility.Hidden;

            //textClick.Text = "";
            //spProps.Children.RemoveRange(1, 100);
            spResults.Children.Clear();
            textResults.Text = "[";
            int counter = 0;

            Hyperlink hyperlink1 = null;

            foreach (var item in results)
            {
                counter++;

                if (!onlyDisplayJson)
                {
                    JToken mainJsonToken = JToken.Parse(item);

                    if (counter == 1 && mainJsonToken.SelectToken("type") != null && (string)((JValue)mainJsonToken["type"]).Value.ToString().ToLower() == "vertex")
                    {
                        populateGraph = true;
                    }

                    string partitionId = "";
                    if (populateGraph)
                    {
                        partitionId = GetPartitionId(mainJsonToken);

                        if (mainJsonToken.SelectToken("id") != null)
                        {
                            string vertexId = $"{(string)((JValue)mainJsonToken["id"]).Value}";
                            TextBlock textBlock = new TextBlock()
                            {
                                FontSize = 12,
                                //Foreground = new SolidColorBrush(Colors.Green),
                                //Background = new SolidColorBrush(Colors.AliceBlue),
                                TextTrimming = TextTrimming.CharacterEllipsis
                            };

                            textBlock.Inlines.Clear();
                            Hyperlink hyperlink = new Hyperlink() { NavigateUri = new Uri(uriString: "http://www.google.com") }; // + vertexId) };
                            Run run1 = new Run(vertexId);
                            hyperlink.Inlines.Add(run1);
                            if (!string.IsNullOrWhiteSpace(partitionId))
                            {
                                hyperlink.Tag = mainJsonToken.Last.First.SelectToken(partitionId).Last.Last.Last.ToString();
                            }

                            hyperlink.Click += Hyperlink_Click;

                            textBlock.Inlines.Add(hyperlink);
                            Border border = new Border()
                            {
                                BorderBrush = new SolidColorBrush(Colors.LightGray),
                                Padding = new Thickness(5),
                                Margin = new Thickness(5),
                                BorderThickness = new Thickness(0, 0, 0, 1)
                            };

                            border.Child = textBlock;

                            spResults.Children.Add(border);

                            if (counter == 1)
                            {
                                hyperlink1 = hyperlink;
                            }
                        }
                    } //populate graph
                } //only json

                if (counter > 1)
                {
                    textResults.Text += ",\r\n";
                }

                textResults.Text += item;// JsonConvert.SerializeObject(result, Formatting.None);
            } //each result item
            resultTabJson.Header = $"Json ({counter})";
            textResults.Text += "]";
            string json = JsonConvert.SerializeObject(textResults.Text);
            json = textResults.Text;
            Console.WriteLine(json); // single line JSON string

            string jsonFormatted = JValue.Parse(json).ToString(Formatting.Indented);
            textResults.Text = jsonFormatted;
            textResults.ScrollToHome();

            if (!onlyDisplayJson)
            {
                //JsonViewer.Load(json);
                //JsonViewer.ExpandAll();

                if (populateGraph)
                {
                    //LoadGraph(results.Take(1).ToList());
                    //LoadGraphVertexClick(results.Take(1).ToList());

                    tabGraph.Visibility = Visibility.Visible;

                    hyperlink1.DoClick();
                }
                else
                {
                    resultTabJson.IsSelected = true;
                }
            }
            else
            {
                tabResults.SelectedIndex = 0;
            }
        }

        private string GetPartitionId(JToken xx)
        {
            string partitionId = "";
            JToken token = xx.SelectToken("properties");

            if (token == null)
            {
                return "";
            }
            var children = token.Children().ToList();
            foreach (var item in children)
            {
                partitionId = ((Newtonsoft.Json.Linq.JProperty)item).Name;
                string value = ((Newtonsoft.Json.Linq.JContainer)item).First.First.First.Last.ToString();
                if (value.EndsWith($"|{partitionId}", StringComparison.InvariantCultureIgnoreCase))
                {
                    break;
                }
            }

            return partitionId;
        }

        private string GetNameValue(JToken xx)
        {
            string nameField = "";
            string value = "";
            string typeName = (string)((JValue)xx["type"]).Value.ToString();

            if (xx.SelectToken("properties") == null)
            {
                return "";
            }
            var children = xx.SelectToken("properties").Children().ToList();
            foreach (var item in children)
            {
                nameField = ((Newtonsoft.Json.Linq.JProperty)item).Name;

                if (nameField.Equals("Name", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (typeName == "edge")
                    {
                        value = ((Newtonsoft.Json.Linq.JContainer)item).Last.ToString();
                    }
                    else
                    {
                        value = ((Newtonsoft.Json.Linq.JContainer)item).First.First.Last.Last.ToString();
                    }

                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                foreach (var item in children)
                {
                    nameField = ((Newtonsoft.Json.Linq.JProperty)item).Name;

                    if (nameField.Contains("Name"))
                    {
                        if (typeName == "edge")
                        {
                            value = ((Newtonsoft.Json.Linq.JContainer)item).Last.ToString();
                        }
                        else
                        {
                            value = ((Newtonsoft.Json.Linq.JContainer)item).First.First.Last.Last.ToString();
                        }
                        break;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                foreach (var item in children)
                {
                    nameField = ((Newtonsoft.Json.Linq.JProperty)item).Name;

                    if (nameField.Contains("name"))
                    {
                        if (typeName == "edge")
                        {
                            value = ((Newtonsoft.Json.Linq.JContainer)item).Last.ToString();
                        }
                        else
                        {
                            value = ((Newtonsoft.Json.Linq.JContainer)item).First.First.Last.Last.ToString();
                        }
                        break;
                    }
                }
            }

            return value;
        }

        private async void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            //retrieve info from the hyperlink
            Hyperlink link = sender as Hyperlink;
            string id = (link.Inlines.FirstInline as Run).Text;
            string part = link.Tag.ToString();

            //reset graph
            //graphite.Children.Clear();
            //graphite.Reset();
            //graphite.UpdateLayout();
            graphite.NewDiagram(false);

            //load this vertex
            graphite.AddVertex(new Vertex() { ID = id, Info = new InfoBag() { part = part }, Type = VertexType.Standard, Title = id });
            Vertex vertex = graphite.Vertices.Where(v => v.ID == id).FirstOrDefault();

            //look for related vertices
            string query = $"g.V(['{part}','{id}'])" + _vertexOut;

            var list = await ExecuteQueryAsync(query);

            if (list != null && list.Count > 0)
            {
                LoadGraphVertexClick(list);
            }

            if (vertex != null)
            {
                lastVertexTouched = null;
                lastVertexTypeTouched = VertexType.Standard;
                graphite_VertexClick(null, new VertexEventArgs(vertex));
            }
        }

        private void UpdateQueries(string query)
        {
            //if (listQueries.Items.Count == 0 || !listQueries.Items.Contains(query))
            //{
            //    listQueries.Items.Add(query);
            //}

            //if (listQueries.Items.Count > 50)
            //{
            //    listQueries.Items.RemoveAt(0);
            //}
        }
        private void ListQueries_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //(tabQueries.SelectedContent as TextBox).Text = (string)listQueries.SelectedItem;
        }

        private void diaConnection_DialogOpened(object sender, MaterialDesignThemes.Wpf.DialogOpenedEventArgs eventArgs)
        {
            Debug.WriteLine("open");

            string endpoint = ConfigurationManager.AppSettings["GremlinEndpoint"];
            string key = ConfigurationManager.AppSettings["AuthKey"];
            string db = ConfigurationManager.AppSettings["CosmosDb"];
            string collection = ConfigurationManager.AppSettings["Collection"];

            textHost.Text = endpoint;
            textKey.Text = key;
            textDb.Text = db;
            textCollection.Text = collection;
        }

        private void diaConnection_DialogClosing(object sender, MaterialDesignThemes.Wpf.DialogClosingEventArgs eventArgs)
        {

            Debug.WriteLine("closes");

            Debug.WriteLine(eventArgs.Parameter);

            if ((string)eventArgs.Parameter == "Cancel")
            {
                return;
            }

            //save to app config
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings.Remove("GremlinEndpoint");
            config.AppSettings.Settings.Add("GremlinEndpoint", textHost.Text);
            config.AppSettings.Settings.Remove("AuthKey");
            config.AppSettings.Settings.Add("AuthKey", textKey.Text);
            config.AppSettings.Settings.Remove("CosmosDb");
            config.AppSettings.Settings.Add("CosmosDb", textDb.Text);
            config.AppSettings.Settings.Remove("Collection");
            config.AppSettings.Settings.Add("Collection", textCollection.Text);
            //ConfigurationManager.AppSettings["Collection"] = textCollection.Text;
            config.Save();
            ConfigurationManager.RefreshSection("appSettings");

            //ConnectToServer();

            ConnectToClient();
        }

        private async void graphite_VertexClick(object sender, VertexEventArgs e)
        {
            //if (_isDoubleClick)
            //{
            //    e.Handled = true;
            //    Debug.WriteLine($"Click not allowed: {((InfoBag)(e.Vertex.Info)).label}:  {e.Vertex.ID}/{((InfoBag)e.Vertex.Info).part} Name: {e.Vertex.Title}");
            //    return;
            //}
            if (_lastClickedOn == e.Vertex.ID)
            {
                return;
            }
            _lastClickedOn = e.Vertex.ID;

            Debug.WriteLine($"Clicked on a  {((InfoBag)(e.Vertex.Info)).label}:  {e.Vertex.ID}/{((InfoBag)e.Vertex.Info).part} Name: {e.Vertex.Title}");
            //textClick.Text += $"\r\n{e.Vertex.Title}";

            bool isBubble = e.Vertex.Type == VertexType.Bubble;

            #region reset last vertex touched
            if (lastVertexTouched == null)
            {
                lastVertexTouched = e.Vertex;
                lastVertexTypeTouched = e.Vertex.Type;
            }
            else
            {
                if (lastVertexTouched.ID != e.Vertex.ID)
                {
                    lastVertexTouched.Type = lastVertexTypeTouched;
                    graphite.SetVertexType(lastVertexTouched.ID, lastVertexTypeTouched);
                    lastVertexTypeTouched = e.Vertex.Type;
                }
            }
            lastVertexTouched = e.Vertex;
            #endregion

            e.Vertex.Type = VertexType.Selected;
            graphite.SetVertexType(e.Vertex.ID, VertexType.Selected);

            string id = e.Vertex.ID;
            string part = ((InfoBag)e.Vertex.Info).part;
            string query = $"g.V(['{part}','{id}'])";
            List<string> list = await ExecuteQueryAsync(query);
            JToken vToken = JToken.Parse(list[0]);
            //var vToken = xx["QueryVertex"];
            string partitionId = GetPartitionId(vToken);
            string vertexId = vToken.SelectToken("id").ToString();

            if (vertexId == id)
            {
                #region add properties and partition
                List<VertexProperty> aprops = new List<VertexProperty>();
                aprops.Add(new VertexProperty() { Name = "Id", Type = "String", Value = id });

                string label = vToken.SelectToken("label").ToString();

                aprops.Add(new VertexProperty() { Name = "Label", Type = "String", Value = label });

                var children = vToken.SelectToken("properties").Children().ToList();
                foreach (var child in children)
                {
                    string name = ((Newtonsoft.Json.Linq.JProperty)child).Name;
                    string value = ((Newtonsoft.Json.Linq.JProperty)child).Value.Last.Last.Last.ToString();

                    aprops.Add(new VertexProperty() { Name = name, Type = $"{((Newtonsoft.Json.Linq.JProperty)child).Value.Last.Last.Last.Type}", Value = value });
                }
                dataProps.ItemsSource = aprops;

                if (aprops.Count > 0)
                {
                    dataProps.SelectedIndex = 0;
                    dataProps.ScrollIntoView(dataProps.SelectedItem);
                    //expProps.IsExpanded = true;
                }
                #endregion

                foreach (VertexProperty item in dataProps.Items)
                {
                    if (item.Name.Equals(partitionId, StringComparison.InvariantCultureIgnoreCase))
                    {
                        dataProps.SelectedItems.Clear();
                        dataProps.SelectedItem = item;
                        break;
                    }
                }

                query = $"g.V(['{part}','{id}']).bothE().limit(20)";
                list = await ExecuteQueryAsync(query, false);

                spEdges.Children.Clear();

                foreach (var item in list)
                {
                    EdgeProcess(id, part, item);
                }

                //if (list.Count > 0)
                //{
                //    expEdges.IsExpanded = true;
                //}
                //else
                //{
                //    expEdges.IsExpanded = false;
                //}
                //foreach (var item in e.Vertex.ChildrenVertices())
                //{
                //    StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center, Height = 30 };
                //    TextBlock textBlock = new TextBlock()
                //    {
                //        Text = $"{item.Title} ({item.ID})"
                //    };
                //    Arrow arrow = new Arrow() { X1 = 0, X2 = 50, HeadHeight = 3, HeadWidth = 3, Stroke = new SolidColorBrush(Colors.DarkGreen), StrokeThickness = 2.0, Margin = new Thickness(4.0,10.0,4.0,0) };
                //    sp.Children.Add(arrow);
                //    sp.Children.Add(textBlock);
                //    sp.ToolTip = $"{item.Title} ({item.ID})";
                //    spEdges.Children.Add(sp);
                //}
                //foreach (var item in e.Vertex.ParentVertices())
                //{
                //    StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center, Height = 30 };
                //    TextBlock textBlock = new TextBlock()
                //    {
                //        Text = $"{item.Title} ({item.ID})"
                //    };
                //    Arrow arrow = new Arrow() { X1 = 50, X2 = 0, HeadHeight = 3, HeadWidth = 3, Stroke = new SolidColorBrush(Colors.DarkGreen), StrokeThickness = 2.0, Margin = new Thickness(4.0, 10.0, 4.0, 0) };
                //    sp.Children.Add(arrow);
                //    sp.Children.Add(textBlock);
                //    sp.ToolTip = $"{item.Title} ({item.ID})";
                //    spEdges.Children.Add(sp);
                //}
            }

            ////
            if (!isBubble)
            {
                return;
            }
            ////
            ///
            lastVertexTypeTouched = VertexType.Standard;

            query = $"g.V(['{part}','{id}'])" + _vertexOut;
            list = await ExecuteQueryAsync(query);
            query = $"g.V(['{part}','{id}'])" + _vertexIn;
            List<string> list2 = await ExecuteQueryAsync(query, false);

            if (list == null && list2 != null)
            {
                list = list2;
            }
            else if (list != null && list2 != null)
            {
                list = list.Concat(list2).ToList();
            }

            if (list != null && list.Count > 0)
            {
                LoadGraphVertexClick(list);
            }
        }

        private void EdgeProcess(string id, string part, string item)
        {
            JToken eToken = JToken.Parse(item);

            //main item sp
            StackPanel sp = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 40
            };
            //sp.MouseLeftButtonUp += edgeClick;

            StackPanel spItem = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Height = 40,
                Width = 150,
                Cursor = Cursors.Hand,
                Background = new SolidColorBrush(Colors.MintCream)
            };
            spItem.MouseLeftButtonUp += edgeClick;

            if (eToken.SelectToken("inV").ToString().ToLower() == id.ToLower())
            {
                Debug.WriteLine($"in('{eToken.SelectToken("label")}') from {eToken.SelectToken("outVLabel")}: {eToken.SelectToken("outV")}");

                Arrow arrow = new Arrow()
                {
                    X1 = 50,
                    X2 = 0,
                    HeadHeight = 3,
                    HeadWidth = 3,
                    Stroke = new SolidColorBrush(Colors.DarkGreen),
                    StrokeThickness = 2.0,
                    Margin = new Thickness(4.0, 10.0, 4.0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                spItem.Children.Add(arrow);
                TextBlock textBlock = new TextBlock()
                {
                    Text = $"in('{eToken.SelectToken("label")}')",
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                spItem.Children.Add(textBlock);
                sp.Children.Add(spItem);

                TextBlock textBlockMain = new TextBlock()
                {
                    Text = $" from {eToken.SelectToken("outVLabel")}: {eToken.SelectToken("outV")}"
                };
                sp.Children.Add(textBlockMain);
                sp.ToolTip = $"g.V(['{part}','{id}']).in('{eToken.SelectToken("label")}') from {eToken.SelectToken("outVLabel")}: {eToken.SelectToken("outV")}";
            }
            else
            {
                Debug.WriteLine($"out('{eToken.SelectToken("label")}') to {eToken.SelectToken("inVLabel")}: {eToken.SelectToken("inV")}");
                Arrow arrow = new Arrow()
                {
                    X1 = 0,
                    X2 = 50,
                    HeadHeight = 3,
                    HeadWidth = 3,
                    Stroke = new SolidColorBrush(Colors.DarkGreen),
                    StrokeThickness = 2.0,
                    Margin = new Thickness(4.0, 10.0, 4.0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                spItem.Children.Add(arrow);
                TextBlock textBlock = new TextBlock()
                {
                    Text = $"out('{eToken.SelectToken("label")}')",
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                spItem.Children.Add(textBlock);
                sp.Children.Add(spItem);

                TextBlock textBlockMain = new TextBlock()
                {
                    Text = $" to {eToken.SelectToken("inVLabel")}: {eToken.SelectToken("inV")}"
                };
                sp.Children.Add(textBlockMain);
                sp.ToolTip = $"g.V(['{part}','{id}']).out('{eToken.SelectToken("label")}') to {eToken.SelectToken("inVLabel")}: {eToken.SelectToken("inV")}";
            }

            spEdges.Children.Add(sp);
        }

        private void edgeClick(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is TextBlock)
            {
                Debug.WriteLine((e.OriginalSource as TextBlock).Text);
                Debug.WriteLine(((e.OriginalSource as TextBlock).Parent as StackPanel).Children[1]);
                string clipboardText = (((e.OriginalSource as TextBlock).Parent as StackPanel).Parent as StackPanel).ToolTip.ToString();
                int position = clipboardText.IndexOf("') to ");
                if (position > -1)
                {
                    clipboardText = clipboardText.Substring(0, position + 2);
                }
                else
                {
                    position = clipboardText.IndexOf("') from ");
                    if (position > -1)
                    {
                        clipboardText = clipboardText.Substring(0, position + 2);
                    }
                }
                if (position > -1)
                {
                    Clipboard.SetText(clipboardText);

                    var notificationManager = new NotificationManager();

                    notificationManager.Show(
                        new NotificationContent { Title = "Copied to Clipboard", Message = clipboardText },
                        areaName: "WindowArea");
                    //notificationManager.Show(new NotificationContent
                    //{
                    //    Title = "Copied to Clipboard",
                    //    Message = clipboardText,
                    //    Type = NotificationType.Information
                    //});
                }
            }
            else
            {
                //arrow
            }

        }

        private void LoadGraphVertexClick(List<string> list)
        {
            if (list.Count > 0)
            {
                var xxx = JToken.Parse(list[0]);
                var yyy = xxx["QueryVertex"];
                //graphite.Children.Clear();
                string partitionId = GetPartitionId(yyy);

                foreach (var item in list)
                {
                    JToken xx = JToken.Parse(item);
                    var vToken = xx["QueryVertex"];

                    string vertexId = vToken.SelectToken("id").ToString();
                    string vertexPart = vToken.Last.First.SelectToken(partitionId).First.Last.Last.ToString();

                    if (graphite.HasVertex(vertexId))
                    {
                        continue;
                    }

                    var vertex = new Vertex()
                    {
                        ID = vToken.SelectToken("id").ToString(),
                        Type = VertexType.Bubble
                    };

                    InfoBag bag = new InfoBag();
                    bag.label = vToken.SelectToken("label").ToString();

                    if (!string.IsNullOrWhiteSpace(vertexPart))
                    {
                        bag.part = vertexPart;
                    }
                    string vertexName = GetNameValue(vToken);

                    if (!string.IsNullOrWhiteSpace(vertexName))
                    {
                        vertex.Title = vertexName;
                    }
                    else
                    {
                        vertex.Title = vertex.ID;
                    }

                    vertex.Info = bag;
                    graphite.AddVertex(vertex);
                }
                Dictionary<string, string> edges = new Dictionary<string, string>();

                foreach (var item in list)
                {
                    JToken xx = JToken.Parse(item);
                    var vEdge = xx["QueryEdge"];

                    string inVId = vEdge.SelectToken("inV").ToString();
                    string outVId = vEdge.SelectToken("outV").ToString();

                    if (!edges.ContainsKey(inVId + "_" + outVId) && !edges.ContainsKey(outVId + "_" + inVId))
                    {
                        edges.Add(inVId + "_" + outVId, "");
                        edges.Add(outVId + "_" + inVId, "");
                    }
                    else
                    {
                        Debug.WriteLine($"WHAT?????? EDGE ADD ==> FROM: {inVId} to {outVId}");
                        continue;
                    }

                    string name = GetNameValue(vEdge);
                    name = "";
                    Vertex v1 = graphite.Vertices.Where(v => v.ID == inVId).FirstOrDefault();
                    Vertex v2 = graphite.Vertices.Where(v => v.ID == outVId).FirstOrDefault();
                    if (v1 != null && v2 != null && v1 != v2)
                    {
                        graphite.AddEdge(v2, v1, name);
                        Debug.WriteLine($"EDGE ADD ==> FROM: {v2.Title} TO: {v1.Title} LABEL: {name}");
                        //break;
                    }
                    else
                    {
                        Debug.WriteLine($"WHAT?????? EDGE ADD ==> FROM: {v2.Title} TO: {v1.Title} LABEL: {name}");
                    }


                    //if (graphite.h(vertexId))
                    //{
                    //    continue;
                    //}
                    //Vertex from = graphite.Vertices.Where(v => v.ID == item.QueryEdge.inV).FirstOrDefault();
                    //Vertex to = graphite.Vertices.Where(v => v.ID == item.QueryEdge.outV).FirstOrDefault();

                    //graphite.AddEdge(from, to, item.QueryEdge.label);

                    //InfoBag bag = new InfoBag();
                    //bag.label = item.QueryVertex.label;
                    //string vertexPart = item.QueryVertex.properties.PartitionId;

                    //if (!string.IsNullOrWhiteSpace(vertexPart))
                    //{
                    //    bag.part = vertexPart;
                    //}
                }
                //tabResults.SelectedIndex = 2;
            }
        }

        private async void graphite_VertexDoubleClick(object sender, VertexEventArgs e)
        {
            Debug.WriteLine($"DClicked on a  {((InfoBag)(e.Vertex.Info)).label}:  {e.Vertex.ID}/{((InfoBag)e.Vertex.Info).part} Name: {e.Vertex.Title}");
            string id = e.Vertex.ID;
            string part = ((InfoBag)e.Vertex.Info).part;
            string oldquery = _newQueryTabDefaultQuery;

            _newQueryTabDefaultQuery = $"g.V(['{part}','{id}'])";

            tab_MouseLeftButtonUp(null, null);

            ////////TabItem tab = this.tabAdd();

            ////////// bind tab control
            ////////tabQueries.DataContext = _queryTabItems;

            ////////// select newly added tab item
            ////////tabQueries.SelectedItem = tab;

            ////////tabQueries.SelectedIndex = tabQueries.Items.Count - 1;

            //look for related vertices
            string query = $"g.V(['{part}','{id}'])" + _vertexOut;

            var list = await ExecuteQueryAsync(query, false);

            if (list != null && list.Count > 0)
            {
                LoadGraphVertexClick(list);
            }

            //if (vertex != null)
            //{
            //    lastVertexTouched = null;
            //    lastVertexTypeTouched = VertexType.Standard;
            //    graphite_VertexClick(null, new VertexEventArgs(vertex));
            //}
            _newQueryTabDefaultQuery = oldquery;
            int count = _queryTabItems.Count;
            Debug.WriteLine($"dc:tabitems{count}/{tabQueries.Items.Count}");

            //tabQueries.Items.RemoveAt(count - 2);
            //_queryTabItems.RemoveAt(count - 2);

            var item = tabQueries.Items.Cast<TabItem>().Where(i => i.Name.Equals($"tab{count - 2}")).SingleOrDefault();

            TabItem tab = item as TabItem;

            if (tab != null)
            {
                if (_queryTabItems.Count < 3)
                {
                    MessageBox.Show("Cannot remove last tab.");
                }
                else
                {
                    // get selected tab
                    TabItem selectedTab = tabQueries.SelectedItem as TabItem;

                    //TabItem selectedTab = tab;

                    // clear tab control binding
                    tabQueries.DataContext = null;

                    _queryTabItems.Remove(tab);

                    // bind tab control
                    tabQueries.DataContext = _queryTabItems;

                    selectedTab.Name = string.Format("tab{0}", _queryTabItems.Count - 1);
                    selectedTab.Header = string.Format("query {0}", _queryTabItems.Count - 1);

                    // select previously selected tab. if that is removed then select first tab
                    if (selectedTab == null || selectedTab.Equals(tab))
                    {
                        selectedTab = _queryTabItems[0];
                    }
                    tabQueries.SelectedItem = selectedTab;
                    //(tabQueries.SelectedContent as TextBox).Text = _newQueryTabDefaultQuery;
                }
            }
        }

        private void buttonAbout_Click(object sender, RoutedEventArgs e)
        {
            About dlg = new About();

            // Configure the dialog box
            dlg.Owner = this;
            //dlg.DocumentMargin = this.documentTextBox.Margin;

            // Open the dialog box modally 
            dlg.ShowDialog();
        }

        private void GridSplitter_MouseMove(object sender, MouseEventArgs e)
        {
            Debug.WriteLine($"GridSplitter_MouseMove: {e.OriginalSource.ToString()}");
        }
    }

    public class PopupViewModel
    {
        public string Name { get; } = "John Doe";
    }

    public enum ConnectionEditorDisplayMode
    {
        Dialog,
        MultiEdit
    }
    public class VertexProperty
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }
    public struct InfoBag
    {
        public string part;
        public string label;
    }
    public class InfoBlob : DependencyObject
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Foto { get; set; }
        public int Born { get; set; }
        public int Died { get; set; }
    }

    #region WPF converters
    public class NameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InfoBlob)
            {
                var blob = value as InfoBlob;
                return string.Format("{0} {1}", blob.FirstName, blob.LastName);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class PictureConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InfoBlob)
            {

                var blob = value as InfoBlob;
                if (string.IsNullOrEmpty(blob.Foto)) blob.Foto = "unknown.gif";
                var imageSourceConverter = new ImageSourceConverter();
                return
                    imageSourceConverter.ConvertFromString(@"pack://application:,,,/WpfApplication1;Component/images/" +
                                                           blob.Foto) as ImageSource;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BornDiedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is InfoBlob)
            {
                var blob = value as InfoBlob;
                string born = "°" + blob.Born;
                string died = blob.Died == 0 ? "" : " - †" + blob.Died;
                return born + died;
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    #endregion
    public class ConnectionEditorViewModel : INotifyPropertyChanged
    {
        private string _label;
        private string _host;
        private string _authorisationKey;
        private string _databaseId;
        private string _collectionId;
        private ConnectionEditorDisplayMode _displayMode;

        public ConnectionEditorViewModel(Action<ConnectionEditorViewModel> saveHandler, Action cancelHandler)
            : this(null, saveHandler, cancelHandler)

        { }

        public ConnectionEditorViewModel(ExplicitConnection explicitConnection, Action<ConnectionEditorViewModel> saveHandler, Action cancelHandler)
        {
            if (saveHandler == null) throw new ArgumentNullException(nameof(saveHandler));
            if (cancelHandler == null) throw new ArgumentNullException(nameof(cancelHandler));

            //SaveCommand = new System.Windows.Input.ICommand(_ => saveHandler(this));
            //CancelCommand = new Command(_ => cancelHandler());
            //ExploreToSettingsFileCommand = new Command(_ => ExploreToSettingsFile());

            if (explicitConnection == null) return;

            Id = explicitConnection.Id;
            _label = explicitConnection.Label;
            _host = explicitConnection.Host;
            _authorisationKey = explicitConnection.AuthorisationKey;
            _databaseId = explicitConnection.DatabaseId;
            _collectionId = explicitConnection.CollectionId;
        }

        public ICommand SaveCommand { get; }

        public ICommand CancelCommand { get; }

        public ICommand ExploreToSettingsFileCommand { get; }

        public Guid? Id { get; }

        public string SettingsConfigurationFilePath => Persistance.ConfigurationFilePath;

        public string Label { get; set; }

        public string Host
        {
            get { return _host; }
            set { _host = value; }
        }

        public string AuthorisationKey
        {
            get { return _authorisationKey; }
            set { _authorisationKey = value; }
        }

        public string DatabaseId
        {
            get { return _databaseId; }
            set { _databaseId = value; }
        }

        public string CollectionId
        {
            get { return _collectionId; }
            set { _collectionId = value; }
        }

        public ConnectionEditorDisplayMode DisplayMode
        {
            get { return _displayMode; }
            set { _displayMode = value; }
        }

        private void ExploreToSettingsFile()
        {
            System.Diagnostics.Process.Start("Explorer", $"/select,\"{SettingsConfigurationFilePath}\"");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private Action<PropertyChangedEventArgs> RaisePropertyChanged()
        {
            return args => PropertyChanged?.Invoke(this, args);
        }
    }
    public class Persistance
    {
        private const string ConfigurationFileName = "gremlinstudio-settings-auto.cosmos";
        public static readonly string ConfigurationFilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ConfigurationFileName);

        public static readonly string QueryFileFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "gremlinstudio-docs");

        public bool TryLoadRaw(out string rawData)
        {
            if (File.Exists(ConfigurationFilePath))
            {
                try
                {
                    rawData = File.ReadAllText(ConfigurationFilePath);
                    return true;
                }
                catch (Exception)
                {
                }
            }
            rawData = null;
            return false;
        }

        public bool TrySaveRaw(string rawData)
        {
            try
            {
                File.WriteAllText(ConfigurationFilePath, rawData);
                return true;
            }
            catch (Exception)
            { }

            return false;
        }
    }
    public class ExplicitConnection //: Connection
    {
        public ExplicitConnection(Guid id, string label, string host, string authorisationKey, string databaseId, string collectionId)
        {
            Id = id;
            Label = label;
            Host = host;
            AuthorisationKey = authorisationKey;
            DatabaseId = databaseId;
            CollectionId = collectionId;
        }
        private string _host;
        private string _authorisationKey;
        private string _databaseId;
        private string _collectionId;
        public Guid Id { get; }

        public string Label
        {
            get;
            set;
        }
        public string Host
        {
            get { return _host; }
            set { _host = value; }
        }

        public string AuthorisationKey
        {
            get { return _authorisationKey; }
            set { _authorisationKey = value; }
        }

        public string DatabaseId
        {
            get { return _databaseId; }
            set { _databaseId = value; }
        }

        public string CollectionId
        {
            get { return _collectionId; }
            set { _collectionId = value; }
        }
        protected bool Equals(ExplicitConnection other)
        {
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExplicitConnection)obj);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}
namespace GremlinStudio.Graph
{
    public class QueryEdge
    {
        public string id { get; set; }
        public string label { get; set; }
        public string type { get; set; }
        public string inVLabel { get; set; }
        public string outVLabel { get; set; }
        public string inV { get; set; }
        public string outV { get; set; }
        public Properties properties { get; set; }
    }

    public class QueryVertex
    {
        public string id { get; set; }
        public string label { get; set; }
        public string type { get; set; }
        public Properties properties { get; set; }
    }
    public class BranchOut
    {
        public QueryEdge QueryEdge { get; set; }
        public QueryVertex QueryVertex { get; set; }
    }
    public class Properties
    {
        public string Name { get; set; }
        public string Search { get; set; }
        public string LocId { get; set; }
        public string Address { get; set; }
        public string Lat_Lon { get; set; }
        public int? Shadow { get; set; }
        public string Account { get; set; }
        public string Type { get; set; }
        public int? Size { get; set; }
        public string PartitionId { get; set; }
    }

}
