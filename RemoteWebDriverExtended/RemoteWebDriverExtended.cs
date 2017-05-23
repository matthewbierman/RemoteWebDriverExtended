using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Serialization;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;

namespace RemoteWebDriverExtended
{
   
        public class RemoteWebDriverExtended : RemoteWebDriver
        {

            private static class DriverInfos
            {
                public static DriverInfo Chrome = new DriverInfo(
                    sessionValuesFileName: "OpenQA.Selenium.ChromeDriver.SessionIdFile.txt",
                    executableFileName: "chromedriver.exe.",
                    processName: "chromedriver",
                    processArguments: "--verbose --url-base=/ --port=9515",
                    sessionURL: "http://localhost:9515",
                    browserName: "chrome"
                    );

                public static DriverInfo FireFox = new DriverInfo(
                    sessionValuesFileName: "OpenQA.Selenium.FireFoxDriver.SessionFile.txt",
                    executableFileName: "geckodriver.exe",
                    processName: "geckodriver",
                    processArguments: "--log=trace --port=4444 --host=127.0.0.1",
                    sessionURL: "http://127.0.0.1:4444",
                    browserName: "firefox"
                    );

                //public static DriverInfo InternetExplorer = new DriverInfo(
                //    sessionIdFileName: "OpenQA.Selenium.InternetExplorer.SessionFile.txt",
                //    executableFileName: "IEDriverServer64.exe",
                //    processName: "IEDriverServer64",
                //    processArguments: "/port=5555 /host=127.0.0.1 /log-level=TRACE",
                //    sessionURL: "http://127.0.0.1:5555");

                public static DriverInfo InternetExplorer = new DriverInfo(
                    sessionValuesFileName: "OpenQA.Selenium.InternetExplorer.SessionFile.txt",
                    executableFileName: "IEDriverServer.exe",
                    processName: "IEDriverServer",
                    processArguments: "/port=5555 /host=127.0.0.1 /log-level=TRACE",
                    sessionURL: "http://127.0.0.1:5555",
                    browserName: "internet explorer"
                    );

            }

            private class DriverInfo
            {
                public DriverInfo(string sessionValuesFileName, string executableFileName, string processName, string processArguments, string sessionURL, string browserName)
                {
                    SessionValuesFileName = sessionValuesFileName;
                    ExecutableFileName = executableFileName;
                    ProcessName = processName;
                    SessionURI = new Uri(sessionURL);
                    ProcessArguments = processArguments;
                    BroswerName = browserName;
                }

                public string SessionValuesFileName { get; private set; }

                public string ExecutableFileName { get; private set; }

                public string ProcessName { get; private set; }

                public Uri SessionURI { get; private set; }

                public string ProcessArguments { get; private set; }

                public string BroswerName { get; private set; }
            }

            public RemoteWebDriverExtended(Uri remoteAddress, DesiredCapabilities desiredCapabilities) : base(remoteAddress, desiredCapabilities)
            {

            }

            public RemoteWebDriverExtended(ICommandExecutor commandExecutor, DesiredCapabilities desiredCapabilities) : base(commandExecutor, desiredCapabilities)
            {

            }

            protected override Response Execute(string driverCommandToExecute, Dictionary<string, object> parameters)
            {
                Response response = null;

                if (driverCommandToExecute == DriverCommand.NewSession)
                {
                    response = GetNewSession(parameters);
                }
                else
                {
                    response = base.Execute(driverCommandToExecute, parameters);
                }

                return response;
            }

            public class SessionData
            {
                public string SessionId { get; set; }

                public DataValuePair[] Value { get; set; }

                public class DataValuePair
                {
                    public string Key { get; set; }

                    public string Value { get; set; }
                }
            }

            private Response GetNewSession(Dictionary<string, object> parameters)
            {

                string browserName = (string)((Dictionary<string, object>)parameters["desiredCapabilities"])["browserName"];

                DriverInfo driverInfo = null;

                if (browserName == DriverInfos.Chrome.BroswerName)
                {
                    driverInfo = DriverInfos.Chrome;
                }
                else if (browserName == DriverInfos.InternetExplorer.BroswerName)
                {
                    driverInfo = DriverInfos.InternetExplorer;
                }
                else if (browserName == DriverInfos.FireFox.BroswerName)
                {
                    driverInfo = DriverInfos.FireFox;
                }

                string sessionFilePath = Path.Combine(Path.GetTempPath(), driverInfo.SessionValuesFileName);

                bool newSession = false;

                var processes = Process.GetProcessesByName(driverInfo.ProcessName);

                if (processes.Length == 0)
                {
                    newSession = true;

                    var path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

                    path = path.Substring(6) + "\\" + driverInfo.ExecutableFileName;

                    Process.Start(path, driverInfo.ProcessArguments);
                }

                Response response;

                if (newSession)
                {
                    response = base.Execute(DriverCommand.NewSession, parameters);

                    var sessionData = new SessionData
                    {
                        SessionId = response.SessionId,
                        Value = (response.Value as Dictionary<string, object>).Select(kv => new SessionData.DataValuePair { Key = kv.Key, Value = kv.Value.ToString() }).ToArray(),
                    };

                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(SessionData));

                    using (TextWriter fileWriter = new StreamWriter(sessionFilePath))
                    {
                        xmlSerializer.Serialize(fileWriter, sessionData);
                        fileWriter.Close();
                    }
                }
                else
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(SessionData));

                    string sessionId;
                    Dictionary<string, object> value;

                    using (TextReader fileReader = new StreamReader(sessionFilePath))
                    {
                        var sessionData = xmlSerializer.Deserialize(fileReader) as SessionData;
                        fileReader.Close();

                        sessionId = sessionData.SessionId;
                        value = sessionData.Value.ToDictionary(kv => kv.Key, kv => kv.Value as object);
                    }

                    response = new Response() { SessionId = sessionId, Status = WebDriverResult.Success };

                    if (browserName == DriverInfos.FireFox.BroswerName)
                    {

                    }
                }

                Debug.WriteLine(this.CommandExecutor.CommandInfoRepository.GetCommandInfo(""));

                return response;
            }

            public class KillProcessesResult
            {
                public string Name { get; set; }
                public int Found { get; set; } = 0;
                public int Killed { get; set; } = 0;
                public bool Success { get; set; } = false;
            }

            public static KillProcessesResult[] KillAllRunningWebDrivers()
            {
                List<KillProcessesResult> result = new List<KillProcessesResult>();

                result.Add(KillProcess(DriverInfos.FireFox.ProcessName));
                result.Add(KillProcess(DriverInfos.Chrome.ProcessName));
                result.Add(KillProcess(DriverInfos.InternetExplorer.ProcessName));

                return result.ToArray();
            }

            private static KillProcessesResult KillProcess(string processName)
            {
                KillProcessesResult result = new KillProcessesResult { Name = processName };

                var processes = Process.GetProcessesByName(processName);

                result.Found = processes.Count();

                if (result.Found > 0)
                {
                    bool wasAbleToKillProcceses = true;
                    foreach (var process in processes)
                    {
                        process.Kill();
                        process.Refresh();


                        if (process.HasExited)
                        {
                            result.Killed++;
                        }
                        else
                        {
                            wasAbleToKillProcceses = false;
                        }
                    }

                    result.Success = wasAbleToKillProcceses;
                }
                else
                {
                    result.Success = true;
                }

                return result;
            }

            public static IWebDriver GetChromeDriver()
            {
                IWebDriver driver = null;

                driver = new RemoteWebDriverExtended(DriverInfos.Chrome.SessionURI, DesiredCapabilities.Chrome());

                return driver;

            }

            public static IWebDriver GetFireFoxDriver()
            {
                IWebDriver driver = null;

                driver = new RemoteWebDriverExtended(new HttpW3CWireProtocolCommandExecutor(DriverInfos.FireFox.SessionURI, RemoteWebDriver.DefaultCommandTimeout), DesiredCapabilities.Firefox());

                return driver;

            }

            public static class InternetExplorerCapabilities
            {
                public const string IgnoreProtectedModeSettingsCapability = "ignoreProtectedModeSettings";
                public const string IgnoreZoomSettingCapability = "ignoreZoomSetting";
                public const string InitialBrowserUrlCapability = "initialBrowserUrl";
                public const string EnableNativeEventsCapability = "nativeEvents";
                public const string EnablePersistentHoverCapability = "enablePersistentHover";
                public const string ElementScrollBehaviorCapability = "elementScrollBehavior";
                public const string UnexpectedAlertBehaviorCapability = "unexpectedAlertBehaviour";
                public const string RequireWindowFocusCapability = "requireWindowFocus";
                public const string BrowserAttachTimeoutCapability = "browserAttachTimeout";
            }

            public static IWebDriver GetInternetExplorerDriver()
            {
                IWebDriver driver = null;

                DesiredCapabilities capabilities = DesiredCapabilities.InternetExplorer();

                capabilities.SetCapability(InternetExplorerCapabilities.EnableNativeEventsCapability, false);  //to fix slow input bug
                capabilities.SetCapability(InternetExplorerCapabilities.IgnoreProtectedModeSettingsCapability, true);

                driver = new RemoteWebDriverExtended(DriverInfos.InternetExplorer.SessionURI, capabilities);

                return driver;
            }
        }

        internal class HttpW3CWireProtocolCommandExecutor : ICommandExecutor
        {
            private const string JsonMimeType = "application/json";
            private const string ContentTypeHeader = JsonMimeType + ";charset=utf-8";
            private const string RequestAcceptHeader = JsonMimeType + ", image/png";
            private Uri remoteServerUri;
            private TimeSpan serverResponseTimeout;
            private bool enableKeepAlive;
            private CommandInfoRepository commandInfoRepository = new W3CWireProtocolCommandInfoRepository();

            /// <summary>
            /// Initializes a new instance of the <see cref="HttpCommandExecutor"/> class
            /// </summary>
            /// <param name="addressOfRemoteServer">Address of the WebDriver Server</param>
            /// <param name="timeout">The timeout within which the server must respond.</param>
            public HttpW3CWireProtocolCommandExecutor(Uri addressOfRemoteServer, TimeSpan timeout)
                : this(addressOfRemoteServer, timeout, true)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="HttpCommandExecutor"/> class
            /// </summary>
            /// <param name="addressOfRemoteServer">Address of the WebDriver Server</param>
            /// <param name="timeout">The timeout within which the server must respond.</param>
            /// <param name="enableKeepAlive"><see langword="true"/> if the KeepAlive header should be sent
            /// with HTTP requests; otherwise, <see langword="false"/>.</param>
            public HttpW3CWireProtocolCommandExecutor(Uri addressOfRemoteServer, TimeSpan timeout, bool enableKeepAlive)
            {
                if (addressOfRemoteServer == null)
                {
                    throw new ArgumentNullException("addressOfRemoteServer", "You must specify a remote address to connect to");
                }

                if (!addressOfRemoteServer.AbsoluteUri.EndsWith("/", StringComparison.OrdinalIgnoreCase))
                {
                    addressOfRemoteServer = new Uri(addressOfRemoteServer.ToString() + "/");
                }

                this.remoteServerUri = addressOfRemoteServer;
                this.serverResponseTimeout = timeout;
                this.enableKeepAlive = enableKeepAlive;

                ServicePointManager.Expect100Continue = false;

                // In the .NET Framework, HttpWebRequest responses with an error code are limited
                // to 64k by default. Since the remote server error responses include a screenshot,
                // they can frequently exceed this size. This only applies to the .NET Framework;
                // Mono does not implement the property.
                if (Type.GetType("Mono.Runtime", false, true) == null)
                {
                    HttpWebRequest.DefaultMaximumErrorResponseLength = -1;
                }
            }

            /// <summary>
            /// Gets the repository of objects containin information about commands.
            /// </summary>
            public CommandInfoRepository CommandInfoRepository
            {
                get { return this.commandInfoRepository; }
            }

            /// <summary>
            /// Executes a command
            /// </summary>
            /// <param name="commandToExecute">The command you wish to execute</param>
            /// <returns>A response from the browser</returns>
            public virtual Response Execute(Command commandToExecute)
            {
                if (commandToExecute == null)
                {
                    throw new ArgumentNullException("commandToExecute", "commandToExecute cannot be null");
                }

                CommandInfo info = this.commandInfoRepository.GetCommandInfo(commandToExecute.Name);
                HttpWebRequest request = info.CreateWebRequest(this.remoteServerUri, commandToExecute);
                request.Timeout = (int)this.serverResponseTimeout.TotalMilliseconds;
                request.Accept = RequestAcceptHeader;
                request.KeepAlive = this.enableKeepAlive;
                request.ServicePoint.ConnectionLimit = 2000;
                if (request.Method == CommandInfo.PostCommand)
                {
                    string payload = commandToExecute.ParametersAsJsonString;
                    byte[] data = Encoding.UTF8.GetBytes(payload);
                    request.ContentType = ContentTypeHeader;
                    Stream requestStream = request.GetRequestStream();
                    requestStream.Write(data, 0, data.Length);
                    requestStream.Close();
                }

                Response toReturn = this.CreateResponse(request);

                return toReturn;
            }

            private static string GetTextOfWebResponse(HttpWebResponse webResponse)
            {
                // StreamReader.Close also closes the underlying stream.
                Stream responseStream = webResponse.GetResponseStream();
                StreamReader responseStreamReader = new StreamReader(responseStream, Encoding.UTF8);
                string responseString = responseStreamReader.ReadToEnd();
                responseStreamReader.Close();

                // The response string from the Java remote server has trailing null
                // characters. This is due to the fix for issue 288.
                if (responseString.IndexOf('\0') >= 0)
                {
                    responseString = responseString.Substring(0, responseString.IndexOf('\0'));
                }

                return responseString;
            }

            private Response CreateResponse(WebRequest request)
            {
                Response commandResponse = new Response();
                HttpWebResponse webResponse = null;
                try
                {
                    webResponse = request.GetResponse() as HttpWebResponse;
                }
                catch (WebException ex)
                {
                    webResponse = ex.Response as HttpWebResponse;
                    if (ex.Status == WebExceptionStatus.Timeout)
                    {
                        string timeoutMessage = "The HTTP request to the remote WebDriver server for URL {0} timed out after {1} seconds.";
                        throw new WebDriverException(string.Format(CultureInfo.InvariantCulture, timeoutMessage, request.RequestUri.AbsoluteUri, this.serverResponseTimeout.TotalSeconds), ex);
                    }
                    else if (ex.Response == null)
                    {
                        string nullResponseMessage = "A exception with a null response was thrown sending an HTTP request to the remote WebDriver server for URL {0}. The status of the exception was {1}, and the message was: {2}";
                        throw new WebDriverException(string.Format(CultureInfo.InvariantCulture, nullResponseMessage, request.RequestUri.AbsoluteUri, ex.Status, ex.Message), ex);
                    }
                }

                if (webResponse == null)
                {
                    throw new WebDriverException("No response from server for url " + request.RequestUri.AbsoluteUri);
                }
                else
                {
                    string responseString = GetTextOfWebResponse(webResponse);
                    if (webResponse.ContentType != null && webResponse.ContentType.StartsWith(JsonMimeType, StringComparison.OrdinalIgnoreCase))
                    {
                        commandResponse = Response.FromJson(responseString);
                    }
                    else
                    {
                        commandResponse.Value = responseString;
                    }

                    if (this.commandInfoRepository.SpecificationLevel < 1 && (webResponse.StatusCode < HttpStatusCode.OK || webResponse.StatusCode >= HttpStatusCode.BadRequest))
                    {
                        // 4xx represents an unknown command or a bad request.
                        if (webResponse.StatusCode >= HttpStatusCode.BadRequest && webResponse.StatusCode < HttpStatusCode.InternalServerError)
                        {
                            commandResponse.Status = WebDriverResult.UnhandledError;
                        }
                        else if (webResponse.StatusCode >= HttpStatusCode.InternalServerError)
                        {
                            // 5xx represents an internal server error. The response status should already be set, but
                            // if not, set it to a general error code. The exception is a 501 (NotImplemented) response,
                            // which indicates that the command hasn't been implemented on the server.
                            if (webResponse.StatusCode == HttpStatusCode.NotImplemented)
                            {
                                commandResponse.Status = WebDriverResult.UnknownCommand;
                            }
                            else
                            {
                                if (commandResponse.Status == WebDriverResult.Success)
                                {
                                    commandResponse.Status = WebDriverResult.UnhandledError;
                                }
                            }
                        }
                        else
                        {
                            commandResponse.Status = WebDriverResult.UnhandledError;
                        }
                    }

                    if (commandResponse.Value is string)
                    {
                        // First, collapse all \r\n pairs to \n, then replace all \n with
                        // System.Environment.NewLine. This ensures the consistency of
                        // the values.
                        commandResponse.Value = ((string)commandResponse.Value).Replace("\r\n", "\n").Replace("\n", System.Environment.NewLine);
                    }

                    webResponse.Close();
                }

                return commandResponse;
            }
        }
    }



